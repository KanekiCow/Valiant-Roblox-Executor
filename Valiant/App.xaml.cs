using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Valiant.Classes;
using Valiant.Properties;

namespace Valiant;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static readonly string BinDirectory = Path.GetFullPath("bin");
    public static readonly string ModuleDirectory = Path.GetFullPath(".");
    public static readonly string ThemeDirectory = Path.GetFullPath("theme");

    // HttpClient should only be initialized once.
    public static readonly HttpClient HttpClient = new();

    public App()
    {
        ExceptionHandler.Start();

        Startup += App_Startup;
        Settings.Default.PropertyChanged += Settings_PropertyChanged;

        Directory.CreateDirectory(BinDirectory);
        Directory.CreateDirectory(ModuleDirectory);
        Directory.CreateDirectory(ThemeDirectory);
    }

    private static bool _isXamlDangerous;
    private static bool _isDangerousXamlIgnored;
    public static Exception TryAddExternalXaml(string path)
    {
        try
        {
            if (_isXamlDangerous)
                return null;

            var content = File.ReadAllText(path);

            // too lazy to actually validate lmao
            if (content.ToLower().Contains("cdata") && !_isDangerousXamlIgnored)
            {
                var option = MessageBox.Show(
                    "Xaml style file contains potentially dangerous code! Do you want to execute?",
                    "Warning!",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (option == MessageBoxResult.Yes)
                    _isDangerousXamlIgnored = true;
                else
                {
                    _isXamlDangerous = true;
                    return null;
                }
            }

            Current.Resources.MergedDictionaries.Add(
                new ResourceDictionary { Source = new Uri(path, UriKind.Absolute) });
        } catch (Exception ex) { return ex; }

        return null;
    } 

    private static void App_Startup(object sender, StartupEventArgs e) =>
        Valiant.MainWindow.Instance.Show();

    private static void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        Settings.Default.Save();

        // dummy injector to check compatibility
        if (e.PropertyName is not ("Api" or "Injector")) return;
        var injector = Injector.CreateNewInstance(Settings.Default.Injector, "");

        if (!injector.SupportedApi.HasFlag(Settings.Default.Api))
            MessageBox.Show(
                "The API isn't compatible with the injector!",
                "Warning!",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
    }
}