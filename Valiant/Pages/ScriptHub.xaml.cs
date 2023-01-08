using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using Valiant.Classes;
using Valiant.Properties;
using Valiant.UserControls;

namespace Valiant.Pages;

/// <summary>
/// Interaction logic for ScriptHub.xaml
/// </summary>
public partial class ScriptHub : Page
{
    public static readonly DependencyProperty CurrentPageProperty = DependencyProperty.Register(
        nameof(CurrentPage), typeof(ScriptPage), typeof(ScriptHub), new PropertyMetadata(default(ScriptPage)));

    public ScriptPage CurrentPage
    {
        get => (ScriptPage)GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    public static readonly DependencyProperty CurrentIndexProperty = DependencyProperty.Register(
        nameof(CurrentIndex), typeof(int), typeof(ScriptHub), new PropertyMetadata(default(int)));

    public int CurrentIndex
    {
        get => (int)GetValue(CurrentIndexProperty);
        set => SetValue(CurrentIndexProperty, value);
    }

    public static readonly DependencyProperty IsLoadedProperty = DependencyProperty.Register(
        nameof(IsLoaded), typeof(bool), typeof(ScriptHub), new PropertyMetadata(default(bool)));

    public new bool IsLoaded
    {
        get => (bool)GetValue(IsLoadedProperty);
        set => SetValue(IsLoadedProperty, value);
    }

    // Singleton
    public static ScriptHub Instance { get; private set; }

    public ScriptHub()
    {
        InitializeComponent();
        DataContext = this;
        Instance = this;

        ScriptHubLoad();
    }

    public async void ScriptHubLoad(int page = 1, int take = 25, string query = "")
    {
        IsLoaded = false;

        try
        {
            var url = new UriBuilder("https://scriptblox.com/api/script/fetch");

            var para = HttpUtility.ParseQueryString(url.Query);
            para["page"] = page.ToString();
            para["max"] = take.ToString();

            if (!string.IsNullOrWhiteSpace(query))
                para["q"] = query;

            url.Query = para.ToString();

            var str = await App.HttpClient.GetStringAsync(url.Uri);
            var json = ScriptPage.Parse(str);

            CurrentPage = json;
            CurrentIndex = page;

            ScriptList.ItemsSource = CurrentPage.Scripts;
        }

        catch
        {
            ScriptList.ItemsSource = null;
            MainWindow.Instance.ScriptHubRadioButton.IsEnabled = false;
            MessageBox.Show("There seems to be a problem with our cloud provider. Script hub is temporary disabled.");
        }

        IsLoaded = true;
    }

    private void FilterBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        ScriptHubLoad(page: 1, take: 20, query: FilterBox.Text);
        e.Handled = true;
    }

    private void PrevButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (CurrentIndex > 1)
            ScriptHubLoad(page: CurrentIndex - 1, take: 20, query: FilterBox.Text);
    }
    private void balls_OnClick(object sender, RoutedEventArgs e)
    {
      
        ScriptHubLoad(page: 1, take: 20, query: FilterBox.Text);
        e.Handled = true;
        return;
    }

    private void NextButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (CurrentIndex < CurrentPage.TotalPages)
            ScriptHubLoad(page: CurrentIndex + 1, take: 20, query: FilterBox.Text);
    }

    private async void ExecuteButton_OnClick(object sender, RoutedEventArgs e)
    {
        var item = (Script)ScriptList.SelectedItem;
        if (item != null)
            MainWindow.Instance.Api.Execute(await item.GetContent());
    }

    private async void CopyButton_OnClick(object sender, RoutedEventArgs e)
    {
        var item = (Script)ScriptList.SelectedItem;
        if (item != null)
            Clipboard.SetText(await item.GetContent());
    }

    private async void OpenButton_OnClick(object sender, RoutedEventArgs e)
    {
        var item = (Script)ScriptList.SelectedItem;
        if (item == null) return;

        var tab = new TabInfo
        {
            Name = item.Title,
            Content = await item.GetContent()
        };
        
        Code.Instance.Editor.Items.Add(tab);
        Code.Instance.Editor.TabList.SelectedItem = tab;

        MainWindow.Instance.CodeRadioButton.IsChecked = true;
    }

    private void ScriptList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

    }
}