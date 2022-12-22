using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace Valiant.UserControls;

/// <summary>
/// Interaction logic for EditableTextBlock.xaml
/// </summary>
public partial class EditableTextBlock : UserControl
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(EditableTextBlock), new PropertyMetadata(default(string)));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register(
        nameof(IsEditing), typeof(bool), typeof(EditableTextBlock), new PropertyMetadata(false));

    public bool IsEditing
    {
        get => (bool)GetValue(IsEditingProperty);
        set => SetValue(IsEditingProperty, value);
    }

    public EditableTextBlock()
    {
        InitializeComponent();
    }

    private void TextEdit_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter && e.Key != Key.Escape) return;

        Keyboard.ClearFocus();
        e.Handled = true;
    }

    private void TextEdit_LostKeyboardFocus(object sender, RoutedEventArgs e) =>
        IsEditing = false;

    private void TextBlockControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        IsEditing = true;
        FocusManager.SetFocusedElement(TextEdit, TextEdit);
    }
}