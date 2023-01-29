using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using Valiant.Classes;
using Valiant.Pages;
using Valiant.UserControls;
using WpfAnimatedGif;
using Settings = Valiant.Properties.Settings;

namespace Valiant;

public class Notification
{
    public string Title { get; set; }
    public string Description { get; set; }
}

/// <summary>
/// Interaction logic for MainWindow.xaml   
/// </summary>
public partial class MainWindow : Window
{
    
    public static readonly DependencyProperty CanUiBeUsedProperty = DependencyProperty.Register(
        nameof(CanUiBeUsed), typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

    public bool CanUiBeUsed
    {
        get
        {
            if (GetValue(CanUiBeUsedProperty) == null)
                SetValue(CanUiBeUsedProperty, false);
            return (bool)GetValue(CanUiBeUsedProperty);
        }
        set => SetValue(CanUiBeUsedProperty, value);
    }

    public ExploitApi Api;

    public ObservableCollection<Notification> Notifications { get; }

    public static MainWindow Instance { get; }

    private readonly DispatcherTimer _autoAttachTimer = new() { Interval = TimeSpan.FromSeconds(1) };

    static MainWindow()
    {
        Instance = new MainWindow();
    }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        Notifications = new ObservableCollection<Notification>();

        // Initialize API
        Directory.CreateDirectory(App.ModuleDirectory);

        Api = ExploitApi.CreateNewInstance(App.ModuleDirectory, Settings.Default.Api, Settings.Default.Injector);
        Api.InitProgressChanged += Api_OnInitProgressChanged;

        // Auto attach
        _autoAttachTimer.Tick += _autoAttachTimer_OnTick;

        // Disable navigation
        NavigationCommands.BrowseBack.InputGestures.Clear();
        NavigationCommands.BrowseForward.InputGestures.Clear();
    }

    public void CreateNotification(Notification notification) =>
        Notifications.Add(notification);

    public static bool IsWebView2Installed() =>
        CoreWebView2Environment.GetAvailableBrowserVersionString() != null;

    public async Task<bool> CheckDependencies()
    {
        UpdateStatus(
            "Checking for WebView2 availability",
            0,
            Visibility.Visible,
            Visibility.Visible);

        if (IsWebView2Installed()) return true;

        UpdateStatus(
            "Downloading WebView2",
            33,
            Visibility.Visible,
            Visibility.Visible);

        var webView2 = Path.Combine(Path.GetTempPath(), "webview.exe");
        await App.HttpClient.GetFileAsync("https://go.microsoft.com/fwlink/p/?LinkId=2124703", webView2);

        UpdateStatus(
            "Installing WebView2",
            66,
            Visibility.Visible,
            Visibility.Visible);

        var proc = new Process
        {
            StartInfo =
            {
                FileName = webView2,
                Arguments = "/silent /install",
                UseShellExecute = false
            }
        };

        proc.Start();

        await proc.WaitForExitAsync();

        MessageBox.Show("WebView2 has been installed! Click \"ok\" to restart Valiant", "WebView2", MessageBoxButton.OK, MessageBoxImage.Information);

        return false;

    }

    public void Api_OnInitProgressChanged(object sender, InitProgressEventArg e)
    {
        switch (e.Progress)
        {
            case InitializeProgress.DownloadingDll:
                UpdateStatus(
                    text: "Downloading DLL",
                    progress: 0,
                    spinnerVisibility: Visibility.Visible,
                    progressVisibility: Visibility.Visible);
                break;
            case InitializeProgress.DownloadingInjector:
                UpdateStatus(
                    text: "Downloading Injector",
                    progress: 50,
                    spinnerVisibility: Visibility.Visible,
                    progressVisibility: Visibility.Visible);
                break;
            case InitializeProgress.Initialized:             
                _autoAttachTimer.IsEnabled = Settings.Default.AutoAttach;

                Settings.Default.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == "AutoAttach")
                        _autoAttachTimer.IsEnabled = Settings.Default.AutoAttach;
                };

                UpdateStatus(
                    text: "Loaded! Welcome to Valiant 2",
                    progress: 0,
                    spinnerVisibility: Visibility.Hidden,
                    progressVisibility: Visibility.Hidden);

                CreateNotification(new Notification
                {
                    Title = "Loaded!",
                    Description = "Valiant is loaded!"
                });
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (e.IsInitialized)
            CanUiBeUsed = true;
    }

    private bool _autoAttaching;
    private void _autoAttachTimer_OnTick(object sender, EventArgs e)
    {
        var robloxProcesses = Process.GetProcessesByName("RobloxPlayerBeta");
        if (robloxProcesses.Length == 0 || Api.IsInjected())
        {
            _autoAttaching = false;
            return;
        }

        if (_autoAttaching || robloxProcesses.Any(p =>
                p.MainWindowHandle == IntPtr.Zero ||
                p.MainWindowTitle != "Roblox")) return;
        
        _autoAttaching = true;
        Api.Inject();
    }

    public void UpdateStatus(
        string text = "",
        double progress = 0,
        Visibility spinnerVisibility = Visibility.Hidden,
        Visibility progressVisibility = Visibility.Hidden)
    {
        StatusText.Text = text;
        StatusSpinner.Visibility = spinnerVisibility;
        StatusProgress.Visibility = progressVisibility;
        StatusProgress.Value = progress;
    }

    private void UpdateWallpaper(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                ImageBehavior.SetAnimatedSource(WallpaperImage, null);
                return;
            }

            var bitmap = new BitmapImage(new Uri(path));
            ImageBehavior.SetAnimatedSource(WallpaperImage, bitmap);
        }
        catch
        {
            Settings.Default.Wallpaper = null;
        }
    }

    private void PageButton_OnChecked(object sender, RoutedEventArgs e)
    {
        var btn = (PageRadioButton)sender;
        PageFrame.Navigate(btn.Page);
    }

    private void TitleBorder_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

    private void MaximizeButton_OnClick(object sender, RoutedEventArgs e) => WindowState = WindowState switch
    {
        WindowState.Maximized => WindowState.Normal,
        _ => WindowState.Maximized
    };

    private void MinimizeButton_OnClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void MainWindow_OnStateChanged(object sender, EventArgs e) => MaximizeButton.Content = WindowState switch
    {
        WindowState.Maximized => '\xe923',
        _ => '\xe922'
    };

    private static string GetResourceKey(IDictionary dict, object resource) => (
        from object dictKey in dict.Keys
        where dict[dictKey] == resource
        select dictKey.ToString()).FirstOrDefault();

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        var pt = e.GetPosition(MainGrid);

        BackgroundBrush.GradientOrigin = new Point(pt.X / Width, pt.Y / Height);

        if (!Settings.Default.Inspector)
            return;

        var result = VisualTreeHelper.HitTest(MainGrid, pt);
        if (result is not { VisualHit: FrameworkElement }) return;
        
        var obj = (FrameworkElement)result.VisualHit;

        var absolutePosition = obj
            .TransformToAncestor(MainGrid)
            .Transform(new Point(0, 0));

        InspectionText.Text =
            $"Object: {obj.GetType().Name}\n" +
            $"Name: {obj.Name}\n" +
            $"Style: {GetResourceKey(Resources, obj.Style)}";

        BoundingRect.Margin = new Thickness(absolutePosition.X, absolutePosition.Y, 0, 0);
        BoundingRect.Width = obj.ActualWidth;
        BoundingRect.Height = obj.ActualHeight;
    }
    

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        // Theme
        Settings.Default.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == "Wallpaper")
                UpdateWallpaper(Settings.Default.Wallpaper);
        };

        UpdateWallpaper(Settings.Default.Wallpaper);

        foreach (var file in Directory.GetFiles(App.ThemeDirectory, "*.xaml"))
        {
            Debug.WriteLine(file);
            var ex = App.TryAddExternalXaml(file);
            if (ex != null)
                Debug.WriteLine(ex.ToString());
        }

        HomePageButton.IsChecked = true;
    }

    private void NotificationButton_OnClick(object sender, RoutedEventArgs e)
    {
        var data = (Notification)((Button)sender).DataContext;
        Notifications.Remove(data);
    }
}