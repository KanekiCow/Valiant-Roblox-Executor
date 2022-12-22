using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using Valiant.Properties;
using Valiant.UserControls;
using Path = System.IO.Path;

namespace Valiant.Pages;

/// <summary>
/// Interaction logic for Code.xaml
/// </summary>
public partial class Code : Page
{
    public static Code Instance { get; private set; }

    public Code()
    {
        InitializeComponent();
        InitializeTree("./scripts");

        Instance = this;
    }

    // tree fucking view
    private static bool IsSamePath(string path, params string[] paths) =>
        paths.All(e => Path.GetFullPath(path) == Path.GetFullPath(e));

    private static FileSystemWatcher NewWatcher(string dir, bool isFolder = false) => new ()
    {
        NotifyFilter = isFolder ?
            NotifyFilters.DirectoryName :
            NotifyFilters.FileName,

        Path = dir,
        EnableRaisingEvents = true,
        IncludeSubdirectories = true,
    };

    private void InitializeTree(string dir)
    {
        Directory.CreateDirectory(dir);

        var fw = NewWatcher(dir);
        var dw = NewWatcher(dir, true);

        AddFolder(Tree, dir, fw, dw);
    }

    private void AddFile(ItemsControl parent, string path, FileSystemWatcher watcher)
    {
        var item = new TreeViewItem
        {
            Header = Path.GetFileName(path)
        };

        item.RequestBringIntoView += (sender, args) => args.Handled = true;

        item.MouseDoubleClick += (_, _) =>
            Editor.Items.Add(new TabInfo { Name = Path.GetFileName(path), Content = File.ReadAllText(path!) });

        watcher.Renamed += (_, e) => Dispatcher.Invoke(() =>
        {
            if (!IsSamePath(e.OldFullPath, path)) return;
            path = e.FullPath;
            item.Header = Path.GetFileName(path);
        });

        watcher.Deleted += (_, e) => Dispatcher.Invoke(() =>
        {
            if (IsSamePath(e.FullPath, path))
                parent.Items.Remove(item);
        });

        parent.Items.Add(item);
    }

    private void AddFolder(ItemsControl parent, string path, FileSystemWatcher fw, FileSystemWatcher dw)
    {
        var item = new TreeViewItem
        {
            Header = Path.GetFileName(path)
        };

        item.RequestBringIntoView += (sender, args) => args.Handled = true;

        dw.Created += (_, e) => Dispatcher.Invoke(() =>
        {
            if (IsSamePath(Path.GetDirectoryName(e.FullPath), path))
                AddFolder(item, e.FullPath, fw, dw);
        });

        dw.Renamed += (_, e) => Dispatcher.Invoke(() =>
        {
            if (!IsSamePath(e.OldFullPath, path)) return;
            path = e.FullPath;
            item.Header = Path.GetFileName(path);
        });

        dw.Deleted += (_, e) => Dispatcher.Invoke(() =>
        {
            if (IsSamePath(e.FullPath, path))
                parent.Items.Remove(item);
        });

        fw.Created += (_, e) => Dispatcher.Invoke(() => {
            if (IsSamePath(Path.GetDirectoryName(e.FullPath), path))
                AddFile(item, e.FullPath, fw);
        });

        foreach (var dir in Directory.GetDirectories(path))
            AddFolder(item, dir, fw, dw);

        foreach (var file in Directory.GetFiles(path))
            AddFile(item, file, fw);

        parent.Items.Add(item);
    }

    private void ExecuteButton_Click(object sender, RoutedEventArgs e) =>
        MainWindow.Instance.Api.Execute(Editor.GetText());

    private void InjectButton_Click(object sender, RoutedEventArgs e)
    {
        if (MainWindow.Instance.Api.IsInjected())
        {
            MessageBox.Show("API is already injected!", "bruh", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        MainWindow.Instance.Api.Inject();
    }

    private void ClearButton_OnClick(object sender, RoutedEventArgs e) =>
        Editor.SetText("");

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            InitialDirectory = Path.GetFullPath("scripts"),
            RestoreDirectory = true,
            FilterIndex = 0,
            Filter = "LUA scripts (*.lua)|*.lua|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
            File.WriteAllText(dialog.FileName, Editor.GetText());
    }

    private void LoadButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            InitialDirectory = Path.GetFullPath("scripts"),
            RestoreDirectory = true,
            FilterIndex = 0,
            Filter = "LUA scripts (*.lua)|*.lua|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
            Editor.Items.Add(new TabInfo
            {
                Name = Path.GetFileName(dialog.FileName),
                Content = File.ReadAllText(dialog.FileName)
            });
    }
}