using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
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
using System.Windows.Shapes;
using Microsoft.Win32;
using Valiant.Classes;
using Path = System.IO.Path;

namespace Valiant.Pages
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Page
    {
        public static readonly DependencyProperty IsNotReinstallingProperty = DependencyProperty.Register(
            nameof(IsNotReinstalling), typeof(bool), typeof(Settings), new PropertyMetadata(true));

        public bool IsNotReinstalling
        {
            get => (bool)GetValue(IsNotReinstallingProperty);
            set => SetValue(IsNotReinstallingProperty, value);
        }

        // Singleton
        public static Settings Instance { get; private set; }

        public Settings()
        {
            InitializeComponent();
            DataContext = this;
            Instance = this;
        }

        private void WallpaperButton_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                RestoreDirectory = true,
                FilterIndex = 0,
                Filter = 
                    "Image files (*.png;*.jpg;*.gif)|*.png;*.jpg;*.gif|" +
                    "All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
                Properties.Settings.Default.Wallpaper = dialog.FileName;
        }

        private void ClearWallpaperButton_OnClick(object sender, RoutedEventArgs e) =>
            Properties.Settings.Default.Wallpaper = null;

        private async void ReinstallButton_OnClick(object sender, RoutedEventArgs e)
        {
            IsNotReinstalling = false;

            var localAppData =
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var robloxPath = Path.Combine(localAppData, "Roblox");

            if (Directory.Exists(robloxPath))
                Directory.Delete(robloxPath, true);
            
            var tempLauncherPath = Path.Combine(Path.GetTempPath(), "RobloxPlayerLauncher.exe");
            await App.HttpClient.GetFileAsync("http://setup.roblox.com/Roblox.exe", tempLauncherPath);

            var process = new Process { StartInfo = new ProcessStartInfo(tempLauncherPath) };

            process.Start();
            await process.WaitForExitAsync();

            IsNotReinstalling = true;
        }

        private void BackgroundBlurCheckBox_OnClick(object sender, RoutedEventArgs e) =>
            MessageBox.Show("Please restart Valiant to apply the changes");

        private void KillRoblox_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (var process in Process.GetProcessesByName("RobloxPlayerBeta"))
                process.Kill();
        }

        private void FixRoblox_OnClick(object sender, RoutedEventArgs e)
        {
            var roblox = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Roblox");
            var globalBasicSettings = Path.Combine(roblox, "GlobalBasicSettings_13.xml");
            var globalSettings = Path.Combine(roblox, "GlobalSettings_13.xml");

            if (File.Exists(globalBasicSettings)) File.Delete(globalBasicSettings);
            if (File.Exists(globalSettings)) File.Delete(globalSettings);
        }

        private static bool IsUserVisible(FrameworkElement element, FrameworkElement container)
        {
            if (!element.IsVisible)
                return false;

            var bounds = element.TransformToAncestor(container).TransformBounds(new Rect(0.0, 0.0, element.ActualWidth, element.ActualHeight));
            var rect = new Rect(0.0, 0.0, container.ActualWidth, container.ActualHeight);
            return rect.Contains(bounds.TopLeft) || rect.Contains(bounds.BottomRight);
        }

        private static void ScrollToTop(UIElement element, ScrollViewer container)
        {
            var elementTransform = element.TransformToAncestor(container);
            var rectangle = elementTransform.TransformBounds(new Rect(new Point(0, 0), element.RenderSize));
            container.ScrollToVerticalOffset(rectangle.Top + container.VerticalOffset);
        }

        private void Viewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (IsUserVisible(AppearancePanel, Viewer))
                AppearanceRadioButton.IsChecked = true;
            if (IsUserVisible(ApiPanel, Viewer))
                ApiRadioButton.IsChecked = true;
        }

        private void AppearanceRadioButton_OnClick(object sender, RoutedEventArgs e) =>
            ScrollToTop(AppearanceLabel, Viewer);

        private void ApiRadioButton_OnClick(object sender, RoutedEventArgs e) =>
            ScrollToTop(ApiLabel, Viewer);
    }
}
