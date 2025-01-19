using Microsoft.Win32;
using SkinConfigurator.ViewModels;
using SMShared;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SkinConfigurator
{
    /// <summary>
    /// Interaction logic for ThemeConfigEditor.xaml
    /// </summary>
    public partial class ThemeConfigEditor : UserControl
    {
        private ThemeConfigModel Model
        {
            get => (ThemeConfigModel)DataContext;
            set => DataContext = Model;
        }

        public event EventHandler<RoutedEventArgs>? RemoveClicked;

        public ThemeConfigEditor()
        {
            InitializeComponent();
        }

        private void ClickBaseColor(object sender, MouseButtonEventArgs e)
        {
            if (PromptColor(Model.LabelBaseColor, out Color newColor))
            {
                Model.LabelBaseColor = newColor;
            }
        }

        private void ClickAccentA(object sender, MouseButtonEventArgs e)
        {
            if (PromptColor(Model.LabelAccentColorA, out Color newAccent))
            {
                Model.LabelAccentColorA = newAccent;
            }
        }

        private void ClickAccentB(object sender, MouseButtonEventArgs e)
        {
            if (PromptColor(Model.LabelAccentColorB, out Color newAccent))
            {
                Model.LabelAccentColorB = newAccent;
            }
        }

        private static bool PromptColor(Color current, out Color newColor)
        {
            var colorDialog = new System.Windows.Forms.ColorDialog
            {
                Color = current.ToDrawingColor()
            };

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                newColor = colorDialog.Color.ToMediaColor();
                return true;
            }
            return false;
        }

        

        private void ClickSetTexture(object sender, RoutedEventArgs e)
        {
            string texExtensions = string.Join(';', Constants.SupportedImageExtensions.Select(e => $"*{e}"));

            var browser = new OpenFileDialog()
            {
                Title = "Select Replacement Skin File",
                Filter = $"Texture Files|{texExtensions}|All Files|*.*",
                InitialDirectory = MainWindow.Settings.DefaultSkinWorkFolder,
                Multiselect = false,
            };

            bool? result = browser.ShowDialog();
            if (result == true)
            {
                Model.UpdateImageFile(browser.FileName);
            }
        }

        private void ClickRemoveTexture(object sender, RoutedEventArgs e)
        {
            Model.UpdateImageFile(null);
        }

        private void RemoveThemeConfig_Click(object sender, RoutedEventArgs e)
        {
            RemoveClicked?.Invoke(this, e);
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Model.ThemeName = (string?)ThemeNameCombo.SelectedItem;
        }
    }
}
