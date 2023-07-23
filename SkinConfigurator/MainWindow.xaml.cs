using Microsoft.Win32;
using SMShared;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace SkinConfigurator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public SkinPackModel Model
        {
            get => (SkinPackModel)DataContext;
            set => DataContext = value;
        }

        public MainWindow()
        {
            InitializeComponent();
            Model = new SkinPackModel();
        }

        private void CreatePackButton_Click(object sender, RoutedEventArgs e)
        {
            Model = new SkinPackModel();
        }

        private void ImportPackButton_Click(object sender, RoutedEventArgs e)
        {
            using var folderDialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                Description = "Select root folder of skin",
                UseDescriptionForTitle = true,
            };

            var result = folderDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                if (!Directory.Exists(folderDialog.SelectedPath))
                {
                    MessageBox.Show(this, "Selected path is not a valid directory", "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Model = PackImporter.ImportFromFolder(folderDialog.SelectedPath);
            }
        }

        private void AddSkinButton_Click(object sender, RoutedEventArgs e)
        {
            using var folderDialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                Description = "Select folder with texture files",
                UseDescriptionForTitle = true,
            };

            var result = folderDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                if (!Directory.Exists(folderDialog.SelectedPath))
                {
                    MessageBox.Show(this, "Selected path is not a valid directory", "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string? carId = PromptForCarType();
                Model.AddSkinConfig(folderDialog.SelectedPath, carId);
            }
        }

        private void AddManySkinButton_Click(object sender, RoutedEventArgs e)
        {
            using var folderDialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                Description = "Select folder containing skins",
                UseDescriptionForTitle = true,
            };

            var result = folderDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                if (!Directory.Exists(folderDialog.SelectedPath))
                {
                    MessageBox.Show(this, "Selected path is not a valid directory", "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string? carId = PromptForCarType();

                foreach (string subDir in Directory.GetDirectories(folderDialog.SelectedPath))
                {
                    bool hasTextures = Directory.EnumerateFiles(subDir)
                        .Any(f => Constants.IsSupportedExtension(Path.GetExtension(f)));

                    if (hasTextures)
                    {
                        Model.AddSkinConfig(subDir, carId);
                    }
                }
            }
        }

        private void RemoveSkinButton_Click(object sender, RoutedEventArgs e)
        {
            Model.RemoveSelectedSkin();
        }

        private void SelectCarTypeButton_Click(object sender, RoutedEventArgs e)
        {
            if (Model.SelectedSkinConfig is SkinConfigModel skin)
            {
                skin.BindingCarId = PromptForCarType();
            }
        }

        private string? PromptForCarType()
        {
            var carTypeSelect = new SelectCarTypeWindow(this);
            bool? result = carTypeSelect.ShowDialog();

            if (result == true)
            {
                string? type = carTypeSelect.SelectedCarType;
                if (type == Constants.CUSTOM_TYPE)
                {
                    type = null;
                }
                return type;
            }
            return null;
        }

        private void PackageButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog()
            {
                Title = "Select Destination Zip",
                FileName = Model.ModInfoModel.Id,
                AddExtension = true,
                DefaultExt = ".zip",
                Filter = "Zip Archives (.zip)|*.zip",
            };

            bool? result = dialog.ShowDialog(this);

            if (result == true && !string.IsNullOrEmpty(dialog.FileName))
            {
                try
                {
                    SkinPackager.SaveToArchive(dialog.FileName!, Model);
                    var openDest = MessageBox.Show("Skins packaged successfully. Did you want to open the destination folder?", 
                        "Success!", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (openDest == MessageBoxResult.Yes)
                    {
                        string? destFolder = Path.GetDirectoryName(dialog.FileName!);
                        Process.Start("explorer.exe", destFolder!);
                    }
                }
                catch (SkinPackageException ex)
                {
                    MessageBox.Show(ex.Message, "Error Packaging Skins", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
