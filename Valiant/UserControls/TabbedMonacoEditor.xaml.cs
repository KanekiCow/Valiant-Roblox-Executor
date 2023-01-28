using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Valiant.Properties;
using Button = System.Windows.Controls.Button;
using Path = System.IO.Path;
using Color = System.Drawing.Color;

namespace Valiant.UserControls;

/// <summary>
/// Interaction logic for TabbedMonacoEditor.xaml
/// </summary>
public partial class TabbedMonacoEditor : UserControl
{
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
        nameof(Items), typeof(ObservableCollection<TabInfo>), typeof(TabbedMonacoEditor), new PropertyMetadata(default(ObservableCollection<TabInfo>)));
        
    public ObservableCollection<TabInfo> Items
    {
        get => (ObservableCollection<TabInfo>)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    private static readonly DependencyPropertyKey IsLoadedProperty = DependencyProperty.RegisterReadOnly(
        nameof(IsLoaded), typeof(bool), typeof(TabbedMonacoEditor), new PropertyMetadata(false));

    public new bool IsLoaded => (bool)GetValue(IsLoadedProperty.DependencyProperty);

    private readonly DispatcherTimer _autosaveTimer = new() { Interval = new TimeSpan(500) };
    private readonly BinaryFormatter _formatter = new();

    public TabbedMonacoEditor()
    {
        InitializeComponent();

        if (!MainWindow.IsWebView2Installed())
            return;

        try
        {
            using var fs = new FileStream(Path.GetFullPath("tabs.bin"), FileMode.Open, FileAccess.ReadWrite);
            Items = (ObservableCollection<TabInfo>)_formatter.Deserialize(fs);
        } catch { Items = new ObservableCollection<TabInfo> { new() { Name = "new tab" } }; }

        TabList.ItemsSource = Items;
        DataContext = this;

        _autosaveTimer.Tick += (_, _) =>
        {
            try
            {
                using var fs = new FileStream(Path.GetFullPath("tabs.bin"), FileMode.OpenOrCreate, FileAccess.ReadWrite);
                _formatter.Serialize(fs, Items);
            } catch { /* ignore */ }
        };

        _autosaveTimer.Start();

        Editor.DefaultBackgroundColor = Color.Transparent;
        InitializeWebView();
    }

    public async void InitializeWebView()
    {
        await Editor.EnsureCoreWebView2Async();
        Editor.CoreWebView2.DOMContentLoaded += Editor_OnCoreWebView2DOMContentLoaded;
        Editor.CoreWebView2.WebMessageReceived += Editor_OnCoreWebView2WebMessageReceived;
        Editor.CoreWebView2.NavigationCompleted += Editor_OnCoreWebView2OnNavigationCompleted;

        Editor.Source = new Uri($"file:///{Directory.GetCurrentDirectory()}/Monaco/rosploco.html");
    }

    private void Editor_OnCoreWebView2OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (TabList.Items.Count > 0)
            TabList.SelectedIndex = 0;

        SetValue(IsLoadedProperty, true);
    }

    public void SetText(string text) =>
        Editor.ExecuteScriptAsync($"setText({JsonConvert.SerializeObject(text)})");

    public string GetText() =>
        TabList.SelectedIndex >= 0 ? Items[TabList.SelectedIndex].Content : "";

    private void Editor_OnCoreWebView2WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e) =>
        // Fuck you UI Thread
        Dispatcher.Invoke(() =>
        {
            if (TabList.SelectedIndex < 0) return;
            Items[TabList.SelectedIndex].Content = e.TryGetWebMessageAsString();
        });

    private int _previousSelectedIndex;
    private void TabList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Items.Count == 0)
            Items.Add(new TabInfo());

        if (Items.Count > 0 & TabList.SelectedItem == null)
            TabList.SelectedIndex = _previousSelectedIndex - 1 >= 0 ? _previousSelectedIndex - 1 : 0;

        _previousSelectedIndex = TabList.SelectedIndex;

        var item = Items[TabList.SelectedIndex];
        var id = JsonConvert.SerializeObject(item.Id);
        var content = JsonConvert.SerializeObject(item.Content);


        if (TabList.SelectedIndex >= 0)
            Editor.ExecuteScriptAsync($@"
                if (!window.monacoModels[{id}]) {{
                    window.monacoModels[{id}] = monaco.editor.createModel({content}, ""lua"")
                    window.monacoModels[{id}].onDidChangeContent(() => chrome.webview.postMessage(window.monacoModels[{id}].getValue()))
                }}
                
                editor.setModel(window.monacoModels[{id}])
            ");
    }

    private void Editor_OnCoreWebView2DOMContentLoaded(object sender, CoreWebView2DOMContentLoadedEventArgs e)
    {
        Dispatcher.Invoke(async () =>
        {
            await Editor.ExecuteScriptAsync(@"
                window.onload = () => {
                    window.monacoModels = {};
                    chrome.webview.addEventListener('message', (e) => setText(e.data));
                };
            ");
        });
    }

    private void AddButton_OnClick(object sender, RoutedEventArgs e) =>
        Items.Add(new TabInfo());

    // hacky ass stuff (thanks stackoverflow)
    public static T FindParentOfType<T>(DependencyObject child) where T : DependencyObject
    {
        var parentObject = VisualTreeHelper.GetParent(child);
        return parentObject switch
        {
            null => null,
            T parent => parent,
            _ => FindParentOfType<T>(parentObject)
        };
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var lbi = FindParentOfType<Grid>((Button)sender);
            var storyboard = ((Storyboard)FindResource("ListBoxRemoved")).Clone();

            storyboard.Completed += (_, _) => Items.Remove((TabInfo)((Button)sender).DataContext);
            storyboard.Begin(lbi);
        } catch { /* ignored */ }
    }

    private void TabScroll_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e) =>
        TabScroll.ScrollToHorizontalOffset(TabScroll.HorizontalOffset + e.Delta);

    private void TabList_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (TabList.IsMouseCaptured)
            TabList.ReleaseMouseCapture();
    }

    private void FrameworkElement_OnRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e) =>
        e.Handled = true;
}

[Serializable]
public class TabInfo : INotifyPropertyChanged
{
    private string _name = "New Tab";
    private string _content = "-- Welcome to Valiant v2";

    public readonly string Id = Guid.NewGuid().ToString("D");

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged();
        }
    }

    public string Content
    {
        get => _content;
        set
        {
            _content = value;
            OnPropertyChanged();
        }
    }

    [field:NonSerialized]
    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}