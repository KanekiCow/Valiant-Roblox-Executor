using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
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

namespace Valiant.Pages;

/// <summary>
/// Interaction logic for Home.xaml
/// </summary>
public partial class Home : Page
{
    public static readonly DependencyPropertyKey IsNotValidatingProperty = DependencyProperty.RegisterReadOnly(
        nameof(IsNotValidating), typeof(bool), typeof(Home), new PropertyMetadata(true));

    public bool IsNotValidating => (bool)GetValue(IsNotValidatingProperty.DependencyProperty);

    // Singleton
    public static Home Instance { get; private set; }

    public Home()
    {
        InitializeComponent();
        DataContext = this;
        Instance = this;

        if (!string.IsNullOrEmpty(Properties.Settings.Default.Key))
            EnterKey(Properties.Settings.Default.Key);
    }

    private void KeyButton_Click(object sender, RoutedEventArgs e) =>
        EnterKey(KeyTextBox.Text);

    private async void EnterKey(string key)
    {
        SetValue(IsNotValidatingProperty, false);

        if (string.IsNullOrWhiteSpace(key))
            MessageBox.Show(
                "Enter something lmao",
                "Invalid key",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

        else
        {
            var content = await App.HttpClient.GetStringAsync("https://raw.githubusercontent.com/KanekiCat/shit/main/sex");

            if (content.Trim() == key.Trim())
            {
                Properties.Settings.Default.Key = key;
                
                var dependenciesInstalled = await MainWindow.Instance.CheckDependencies();

                if (!dependenciesInstalled)
                {
                    Process.Start(Assembly.GetExecutingAssembly().Location);
                    Application.Current.Shutdown();
                }

                await MainWindow.Instance.Api.Initialize();
                MainWindow.Instance.CodeRadioButton.IsChecked = true;

                return;
            }

            MessageBox.Show(
                $"Not a valid key, join our server for the key.\n\n{content}",
                "Invalid key",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        SetValue(IsNotValidatingProperty, true);
    }

    private async void DiscordUrl_OnClick(object sender, RoutedEventArgs e)
    {
        using var wc = new HttpClient();
        var url = await wc.GetStringAsync("https://raw.githubusercontent.com/KanekiCat/shit/main/dsicrod%20sex");
        Process.Start(url);
    }


  


}