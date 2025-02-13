using Microsoft.Win32;
using SkinConfigurator.ViewModels;
using SMShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace SkinConfigurator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindowViewModel Model
        {
            get => (MainWindowViewModel)DataContext;
            set => DataContext = value;
        }

        public static ConfiguratorSettings Settings { get; private set; } = new ConfiguratorSettings();

        public MainWindow()
        {
            string staging = Path.Combine(Environment.CurrentDirectory, "Temp", "Staging");
            Directory.CreateDirectory(staging);

            InitializeComponent();
            Model = new MainWindowViewModel();
            Settings = ConfiguratorSettings.LoadConfig();

#if DEBUG
            var texes = GetCarTex("P:\\SteamLibrary\\steamapps\\common\\Derail Valley\\Mods\\SkinManagerMod\\Exported");
            var settings = new JsonSerializerOptions()
            {
                WriteIndented = true,
            };
            string result = JsonSerializer.Serialize(texes, settings);
            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "tex.json"), result);
#endif
        }

        private static IEnumerable<string> GetNames(string folder)
        {
            string[] ext = { ".png", ".jpeg", ".jpg" };
            return Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
                .Where(s => ext.Contains(Path.GetExtension(s)))
                .Select(s => Path.GetFileName(s))
                .OrderBy(s => s);
        }

        private static Dictionary<string, string[]> GetCarTex(string baseFolder)
        {
            var result = new Dictionary<string, string[]>();
            foreach (string subDir in Directory.EnumerateDirectories(baseFolder))
            {
                result.Add(Path.GetFileName(subDir), GetNames(subDir).ToArray());
            }
            return result;
        }

        #region Project Menu

        private void CreatePackButton_Click(object sender, RoutedEventArgs e)
        {
            Model.Reset();
        }

        private void ImportPackButton_Click(object sender, RoutedEventArgs e)
        {
            using var folderDialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                Description = "Select root folder of skin",
                UseDescriptionForTitle = true,
                SelectedPath = Settings.DefaultSkinWorkFolder,
            };

            var result = folderDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                if (!Directory.Exists(folderDialog.SelectedPath))
                {
                    MessageBox.Show(this, "Selected path is not a valid directory", "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Error);
                    RecentFileSelector.RemoveFile(folderDialog.SelectedPath);
                    return;
                }

                DoFolderImport(folderDialog.SelectedPath);
            }
        }

        private void DoFolderImport(string path)
        {
            using (new WaitCursor())
            {
                var imported = PackImporter.ImportFromFolder(path);

                if (imported is not null)
                {
                    Model.SkinPack = imported;
                    MyResourceSelector.RefreshAvailableItems();

                    RecentFileSelector.InsertFile(path);
                }
            }
        }

        private void ImportZipButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog()
            {
                Title = "Select Zipped Skin Pack",
                DefaultExt = ".zip",
                Filter = "Zip Archives (.zip)|*.zip",
                InitialDirectory = Settings.DefaultSkinWorkFolder,
            };

            bool? result = dialog.ShowDialog(this);

            if (result == true)
            {
                if (!File.Exists(dialog.FileName))
                {
                    MessageBox.Show(this, "Selected path is not a valid Zip Archive", "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Error);
                    RecentFileSelector.RemoveFile(dialog.FileName);
                    return;
                }

                DoZipImport(dialog.FileName);
            }
        }

        private void DoZipImport(string path)
        {
            using (new WaitCursor())
            {
                var imported = PackImporter.ImportFromArchive(path);

                if (imported is not null)
                {
                    Model.SkinPack = imported;
                    MyResourceSelector.RefreshAvailableItems();

                    RecentFileSelector.InsertFile(path);
                }
            }
        }

        private void RecentFileSelector_MenuClick(object sender, RecentFileList.MenuClickEventArgs e)
        {
            var pathAttr = File.GetAttributes(e.Filepath);

            if (pathAttr.HasFlag(FileAttributes.Directory))
            {
                DoFolderImport(e.Filepath);
            }
            else
            {
                DoZipImport(e.Filepath);
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        #endregion

        #region Skins Menu

        private void NewSkinButton_Click(object sender, RoutedEventArgs e)
        {
            string? carId = PromptForCarType();
            Model.AddComponent(carId);
        }

        private void AddSkinButton_Click(object sender, RoutedEventArgs e)
        {
            using var folderDialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                Description = "Select folder with texture files",
                UseDescriptionForTitle = true,
                SelectedPath = Settings.DefaultSkinWorkFolder,
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
                Model.AddComponent(carId, folderDialog.SelectedPath);
            }
        }

        private void AddManySkinButton_Click(object sender, RoutedEventArgs e)
        {
            using var folderDialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                Description = "Select folder containing skins",
                UseDescriptionForTitle = true,
                SelectedPath = Settings.DefaultSkinWorkFolder,
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

                using (new WaitCursor())
                {
                    foreach (string subDir in Directory.GetDirectories(folderDialog.SelectedPath))
                    {
                        bool hasTextures = Directory.EnumerateFiles(subDir)
                            .Any(f => Constants.IsSupportedExtension(Path.GetExtension(f)));

                        if (hasTextures)
                        {
                            Model.AddComponent(carId, subDir);
                        }
                    }
                }
            }
        }

        private void UpgradeSkinButton_Click(object sender, RoutedEventArgs e)
        {
            if (Model.SelectedSkin is PackComponentModel skin)
            {
                foreach (var item in skin.Items.Where(f => f.CanUpgradeFileName))
                {
                    item.UpgradeFileName();
                }
            }
        }

        private void RemoveSkinButton_Click(object sender, RoutedEventArgs e)
        {
            Model.RemoveSelectedComponent();
        }

        #endregion

        #region Settings

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsDialog = new SettingsDialog(Settings);
            bool? result = settingsDialog.ShowDialog();

            if (result == true)
            {
                Settings = settingsDialog.SettingsModel.Data;
                ConfiguratorSettings.SaveConfig(Settings);
            }
        }

        #endregion

        #region Skin Properties

        private void SelectCarTypeButton_Click(object sender, RoutedEventArgs e)
        {
            if (Model.SelectedSkin is PackComponentModel skin)
            {
                skin.CarId = PromptForCarType();
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

        private void SelectResourcesButton_Click(object sender, RoutedEventArgs e)
        {
            if (Model.SelectedSkin == null) return;

            var resourceSelect = new SelectResourcesWindow(this, Model.SkinPack.ResourceOptions);
            bool? result = resourceSelect.ShowDialog();

            if (result == true)
            {
                Model.SelectedSkin.Resources = resourceSelect.SelectedResources;
            }
        }

        #endregion

        #region Skin File Management

        private void AddFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (Model.SelectedSkin == null) return;

            string texExtensions = string.Join(';', Constants.SupportedImageExtensions.Select(e => $"*{e}"));

            var browser = new OpenFileDialog()
            {
                Title = "Select Skin Files To Include",
                Filter = $"Texture Files|{texExtensions}|Numbering Config|numbering.xml|All Files|*.*",
                InitialDirectory = Settings.DefaultSkinWorkFolder,
                Multiselect = true,
            };

            bool? result = browser.ShowDialog();
            if (result == true)
            {
                foreach (string filePath in browser.FileNames)
                {
                    Model.SelectedSkin.AddItem(filePath);
                }
            }
        }

        private void ReplaceFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (Model.SelectedSkin is PackComponentModel currentComp && Model.SelectedSkinFile is SkinFileModel currentFile)
            {
                string texExtensions = string.Join(';', Constants.SupportedImageExtensions.Select(e => $"*{e}"));

                var browser = new OpenFileDialog()
                {
                    Title = "Select Replacement Skin File",
                    Filter = $"Texture Files|{texExtensions}|All Files|*.*",
                    InitialDirectory = Settings.DefaultSkinWorkFolder,
                    Multiselect = false,
                };

                bool? result = browser.ShowDialog();
                if (result == true)
                {
                    var newItem = Model.SelectedSkin.AddItem(browser.FileName);
                    newItem.FileName = currentFile.FileName;
                }

                currentComp.RemoveItem(currentFile);
            }
        }

        private void RemoveFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (Model.SelectedSkin is PackComponentModel currentComp && Model.SelectedSkinFile is SkinFileModel currentFile)
            {
                currentComp.RemoveItem(currentFile);
            }
        }

        private void UpgradeFileNameButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender is Button button) && (button.DataContext is SkinFileModel file))
            {
                file.UpgradeFileName();
            }
        }

        #endregion

        #region Packaging

        private void PackageButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog()
            {
                Title = "Select Destination Zip",
                FileName = Model.SkinPack.ModInfoModel.Id,
                AddExtension = true,
                DefaultExt = ".zip",
                Filter = "Zip Archives (.zip)|*.zip",
            };

            bool? result = dialog.ShowDialog(this);

            if (result == true && !string.IsNullOrWhiteSpace(dialog.FileName))
            {
                RunPackaging<ZipPackager>(dialog.FileName, Path.GetDirectoryName(dialog.FileName!)!);
            }
        }

        private void TestPackButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Select the destination folder for your skin pack - " +
                "this should be a child of the Mods folder in your DV install directory. You may need " +
                "to create a folder if this is the first time exporting a pack. For example:\n\n" +
                "\"C:\\Program Files\\Steam\\steamapps\\common\\Derail Valley\\Mods\\My Beautiful Skin\\\"\n\n" +
                "THIS WILL CLEAR THE SELECTED FOLDER!\nProceed with caution.",
                "Select destination folder for skins", MessageBoxButton.OK, MessageBoxImage.Information);

            var dialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                Description = "Select destination folder for skins",
                UseDescriptionForTitle = true,
                InitialDirectory = Settings.DerailValleyDirectory,
                ShowNewFolderButton = true,
            };

            var result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                RunPackaging<FolderPackager>(dialog.SelectedPath, dialog.SelectedPath);
            }
        }

        private void RunPackaging<T>(string destination, string viewPath) where T : SkinPackager
        {
            using (new WaitCursor())
            {
                try
                {
                    SkinPackager.Package<T>(destination, Model.SkinPack);

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        var openDest = MessageBox.Show("Skins packaged successfully. Did you want to open the destination folder?",
                            "Success!", MessageBoxButton.YesNo, MessageBoxImage.Information);

                        if (openDest == MessageBoxResult.Yes)
                        {
                            Process.Start("explorer.exe", viewPath);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Skins packaged successfully.", 
                            "Success!", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (SkinPackageException ex)
                {
                    MessageBox.Show(ex.Message, "Error Packaging Skins", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Drag and Drop

        private Point _mouseStartPoint;
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);
            _mouseStartPoint = e.GetPosition(null);
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);
            if (e.LeftButton == MouseButtonState.Pressed && (e.OriginalSource is FrameworkElement source) && !IsChildOf<ScrollBar>(source) && (source.DataContext is SkinFileModel file))
            {
                Trace.WriteLine(source.GetType());
                var newPos = e.GetPosition(null);

                if (Math.Abs(newPos.X - _mouseStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(newPos.Y - _mouseStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    var data = new DataObject(typeof(SkinFileModel), file);
                    DragDrop.DoDragDrop(source, data, DragDropEffects.Move);
                }
            }
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            if (e.Data.GetData(typeof(SkinFileModel)) is SkinFileModel file)
            {
                if (GetTargetSkin(e.OriginalSource as DependencyObject) is PackComponentModel target)
                {
                    file.Parent.RemoveItem(file);
                    target.AddItem(file);
                }
            }
            else if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
            {
                if (IsChildOf(e.OriginalSource as DependencyObject, SkinFileList) && Model.SelectedSkin is PackComponentModel target)
                {
                    foreach (string path in files)
                    {
                        target.AddItem(path);
                    }
                }
            }
        }

        private static PackComponentModel? GetTargetSkin(DependencyObject? source)
        {
            while (source != null)
            {
                if (source is FrameworkElement elem && elem.DataContext is PackComponentModel component)
                {
                    return component;
                }

                source = VisualTreeHelper.GetParent(source);
            }

            return null;
        }

        private static bool IsChildOf(DependencyObject? child, DependencyObject parent)
        {
            while (child != null)
            {
                if (child == parent) return true;

                child = VisualTreeHelper.GetParent(child);
            }
            return false;
        }

        private static bool IsChildOf<TParent>(DependencyObject? child)
        {
            while (child != null)
            {
                if (child is TParent) return true;

                child = VisualTreeHelper.GetParent(child);
            }
            return false;
        }

        #endregion

        #region Theme Config

        private void AddThemeConfigButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender == SkinContextAddThemeButton) && (Model.SelectedSkin is PackComponentModel skin))
            {
                Model.AddThemeConfig(skin.Name);
            }
            else
            {
                Model.AddThemeConfig();
            }
        }

        private void ImportThemeConfigButton_Click(object sender, RoutedEventArgs e)
        {
            var browser = new OpenFileDialog()
            {
                Title = "Select Theme Config File",
                Filter = $"JSON Files|*.json|All Files|*.*",
                InitialDirectory = Settings.DefaultSkinWorkFolder,
                Multiselect = true,
            };

            bool? result = browser.ShowDialog();
            if (result == true)
            {
                foreach (string filePath in browser.FileNames)
                {
                    Model.SkinPack.ImportThemeConfig(filePath);
                }
            }
        }

        private void RemoveThemeConfigButton_Click(object sender, RoutedEventArgs e)
        {
            Model.RemoveSelectedThemeConfig();
        }

        #endregion
    }
}
