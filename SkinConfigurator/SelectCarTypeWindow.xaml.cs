using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SkinConfigurator
{
    /// <summary>
    /// Interaction logic for SelectCarTypeWindow.xaml
    /// </summary>
    public partial class SelectCarTypeWindow : Window
    {
        public string? SelectedCarType
        {
            get { return GetValue(SelectedCarTypeProperty) as string; }
            set { SetValue(SelectedCarTypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedCarType.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedCarTypeProperty =
            DependencyProperty.Register("SelectedCarType", typeof(string), typeof(SelectCarTypeWindow), new PropertyMetadata(null));


        public SelectCarTypeWindow(Window owner)
        {
            Owner = owner;
            InitializeComponent();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
