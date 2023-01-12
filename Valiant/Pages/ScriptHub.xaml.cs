using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
using Newtonsoft.Json.Linq;
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

    public ObservableCollection<string> Favorites { get; set; }

    // Singleton
    public static ScriptHub Instance { get; private set; }

    public ScriptHub()
    {
        InitializeComponent();
        Favorites = new ObservableCollection<string>(Properties.Settings.Default.Favorites);
        Favorites.CollectionChanged += (_, _) =>
        {
            Properties.Settings.Default.Favorites = Favorites.ToList();

            if (ScriptTypeComboBox.SelectedIndex == 1)
                ScriptHubLoad(CurrentIndex, query: FilterBox.Text);
        };

        DataContext = this;
        Instance = this;

        ScriptHubLoad();
    }

    public async void ScriptHubLoad(int page = 1, int take = 9, string query = "")
    {
        IsLoaded = false;

        try
        {
            if (ScriptTypeComboBox.SelectedIndex == 0)
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
            }

            // Favorite
            // https://scriptblox.com/api/script/<id>
            else
            {
                page -= 1;
                page = page < 0 ? 0 : page;

                var scriptPage = new ScriptPage
                {
                    TotalPages = Favorites.Count / take,
                    Scripts = new List<Script>()
                };

                // i hate math
                // ive been up for fucking hours to make this so dont
                // expect these code to be good
                var start = page * take;
                var end = page * take + take;
                Debug.WriteLine($"before start: {start} end: {end}");
                end = end >= Favorites.Count ? Favorites.Count : end;
                Debug.WriteLine($"after start: {start} end: {end}");
                for (var i = start; i < end; i++)
                {
                    var fav = Favorites[i];
                    var str = await App.HttpClient.GetStringAsync($"https://scriptblox.com/api/script/{fav}");
                    var json = JToken
                        .Parse(str)!
                        .Value<JToken>("script")!
                        .ToObject<Script>();

                    if (json.Title.ToLower().Contains(FilterBox.Text.ToLower()) ||
                        json.Slug.ToLower().Split('-').Any(o => FilterBox.Text.ToLower().Contains(o)) ||
                        string.IsNullOrWhiteSpace(FilterBox.Text))
                        scriptPage.Scripts.Add(json);
                }

                CurrentPage = scriptPage;
                CurrentIndex = page;
            }
        }

        catch
        {
            ScriptControl.ItemsSource = null;
            MainWindow.Instance.ScriptHubRadioButton.IsEnabled = false;
            MessageBox.Show("There seems to be a problem with our cloud provider. Script hub is temporary disabled.");
        }

        finally
        {
            ScriptControl.ItemsSource = CurrentPage.Scripts;
        }

        IsLoaded = true;
    }

    private void FilterBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        ScriptHubLoad(page: 1, query: FilterBox.Text);
        e.Handled = true;
    }

    private void PrevButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (CurrentIndex > 1)
            ScriptHubLoad(page: CurrentIndex - 1, query: FilterBox.Text);
    }
    // private void balls_OnClick(object sender, RoutedEventArgs e)
    // {
    //   
    //     ScriptHubLoad(page: 1, take: 20, query: FilterBox.Text);
    //     e.Handled = true;
    //     return;
    // }

    private void NextButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (CurrentIndex < CurrentPage.TotalPages)
            ScriptHubLoad(page: CurrentIndex + 1, query: FilterBox.Text);
    }

    private async void ExecuteButton_OnClick(object sender, RoutedEventArgs e)
    {
        var item = (Script)((Button)sender).DataContext;
        if (item != null)
            MainWindow.Instance.Api.Execute(await item.GetContent());
    }

    // private async void CopyButton_OnClick(object sender, RoutedEventArgs e)
    // {
    //     var item = (Script)ScriptList.SelectedItem;
    //     if (item != null)
    //         Clipboard.SetText(await item.GetContent());
    // }

    private async void OpenButton_OnClick(object sender, RoutedEventArgs e)
    {
        var item = (Script)((Button)sender).DataContext;
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

    // private void ScriptList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    // {
    //
    // }
    private void ScriptTypeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FilterBox != null)
            ScriptHubLoad(page: 1, query: FilterBox.Text);
    }

    private void FavButton_OnClick(object sender, RoutedEventArgs e)
    {
        var item = (Script)((Button)sender).DataContext;
        if (Favorites.Contains(item.Id))
        {
            Favorites.Remove(item.Id);
            MessageBox.Show("removed script");
        }
        else
        {
            Favorites.Add(item.Id);
            MessageBox.Show("added script");
        }
    }
}

public class StarConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
        !ScriptHub.Instance.Favorites.Contains((string)value);

    public object ConvertBack(object value, Type targetType, object parameter,
        System.Globalization.CultureInfo culture) =>
        null;
}