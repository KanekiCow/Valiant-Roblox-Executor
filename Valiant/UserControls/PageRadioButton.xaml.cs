using System;
using System.Collections.Generic;
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
/// Interaction logic for PageRadioButton.xaml
/// </summary>
public partial class PageRadioButton : RadioButton
{
    public static readonly DependencyProperty PageProperty = DependencyProperty.Register(
        nameof(Page), typeof(UIElement), typeof(PageRadioButton), new PropertyMetadata(default(UIElement)));

    public UIElement Page
    {
        get => (UIElement)GetValue(PageProperty);
        set => SetValue(PageProperty, value);
    }

    public PageRadioButton()
    {
        InitializeComponent();
    }
}