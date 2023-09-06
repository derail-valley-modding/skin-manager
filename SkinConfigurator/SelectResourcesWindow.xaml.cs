using SkinConfigurator.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SkinConfigurator
{
    /// <summary>
    /// Interaction logic for SelectResourcesWindow.xaml
    /// </summary>
    public partial class SelectResourcesWindow : Window
    {
        public IList<PackComponentModel> Options { get; private set; }

        public IList<PackComponentModel> SelectedResources => ResourceList.SelectedItems.Cast<PackComponentModel>().ToList();

        public SelectResourcesWindow(Window owner, List<PackComponentModel> options)
        {
            Owner = owner;
            Options = options;
            InitializeComponent();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
