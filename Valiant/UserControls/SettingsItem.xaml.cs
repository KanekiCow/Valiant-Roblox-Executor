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

namespace Valiant.UserControls
{
    /// <summary>
    /// Interaction logic for SettingsItem.xaml
    /// </summary>
    public partial class SettingsItem : UserControl
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(SettingsItem), new PropertyMetadata(default(string)));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
            nameof(Description), typeof(string), typeof(SettingsItem), new PropertyMetadata(default(string)));

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public static readonly DependencyProperty ElementProperty = DependencyProperty.Register(
            nameof(Element), typeof(UIElement), typeof(SettingsItem), new PropertyMetadata());

        public UIElement Element
        {
            get => (UIElement)GetValue(ElementProperty);
            set => SetValue(ElementProperty, value);
        }

        public SettingsItem()
        {
            InitializeComponent();
        }
    }
}
