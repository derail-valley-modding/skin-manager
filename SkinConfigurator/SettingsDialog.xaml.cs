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
using System.Windows.Shapes;

namespace SkinConfigurator
{
    /// <summary>
    /// Interaction logic for SettingsDialog.xaml
    /// </summary>
    public partial class SettingsDialog : Window
    {
        public SettingsModel SettingsModel
        {
            get => (SettingsModel)DataContext;
            set => DataContext = value;
        }

        public SettingsDialog(ConfiguratorSettings currentSettings)
        {
            SettingsModel = new SettingsModel(currentSettings);
            InitializeComponent();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void SelectWorkingFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            using var folderDialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                Description = "Select default location for importing skin files",
                UseDescriptionForTitle = true,
                SelectedPath = SettingsModel.WorkingFolder,
            };

            var result = folderDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                if (!Directory.Exists(folderDialog.SelectedPath))
                {
                    MessageBox.Show(this, "Selected path is not a valid directory", "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Error);
                    SettingsModel.WorkingFolder = Environment.CurrentDirectory;
                    return;
                }

                SettingsModel.WorkingFolder = folderDialog.SelectedPath;
            }
        }

        private void SelectDVFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            using var folderDialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                Description = "Select Derail Valley install location",
                UseDescriptionForTitle = true,
                SelectedPath = SettingsModel.DerailValleyDirectory,
            };

            var result = folderDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                if (!Directory.Exists(folderDialog.SelectedPath))
                {
                    MessageBox.Show(this, "Selected path is not a valid directory", "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Error);
                    SettingsModel.DerailValleyDirectory = Environment.CurrentDirectory;
                    return;
                }

                SettingsModel.DerailValleyDirectory = folderDialog.SelectedPath;
            }
        }
    }
}
