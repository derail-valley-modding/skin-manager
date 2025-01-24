using Microsoft.Win32;
using SkinConfigurator.ViewModels;
using SMShared;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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

        private void PriceInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (float.TryParse(PriceInput.Text, out float value))
            {
                
            }
        }
    }

    public class FloatStringRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string strVal = (string)value;

            if (string.IsNullOrWhiteSpace(strVal)) return ValidationResult.ValidResult;

            if (float.TryParse(strVal, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out _))
            {
                return ValidationResult.ValidResult;
            }
            return new ValidationResult(false, "Not a valid decimal number");
        }
    }

    public class FloatStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is float f)
            {
                return f.ToString("F0", CultureInfo.CurrentCulture);
            }
            return string.Empty;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string strVal = (string)value;
            if (string.IsNullOrWhiteSpace(strVal))
            {
                return null;
            }

            if (float.TryParse(strVal, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out float result))
            {
                return (float)Math.Round(Math.Abs(result));
            }
            return null;
        }
    }
}
