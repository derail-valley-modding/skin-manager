using SMShared.Json;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace SkinConfigurator.ViewModels
{
    public class ThemeConfigModel : HasImageFileModel, INotifyPropertyChanged, IValidated
    {
        public SkinPackModel ParentPack { get; private set; }

        public List<string> AvailableSkinNames
        {
            get
            {
                return ParentPack.PackComponents
                    .Where(comp => (comp.Type == PackComponentType.Skin) && !string.IsNullOrWhiteSpace(comp.Name))
                    .Select(comp => comp.Name!)
                    .Union(new[] { string.Empty })
                    .ToList();
            }
        }

        public string? ThemeName
        {
            get { return (string)GetValue(ThemeNameProperty); }
            set
            {
                SetValue(ThemeNameProperty, value);
                RaiseValidationChange();
            }
        }

        // Using a DependencyProperty as the backing store for ThemeName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ThemeNameProperty =
            DependencyProperty.Register("ThemeName", typeof(string), typeof(ThemeConfigModel), new PropertyMetadata(string.Empty));


        public Color LabelBaseColor
        {
            get { return (Color)GetValue(LabelBaseColorProperty); }
            set { SetValue(LabelBaseColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LabelBaseColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LabelBaseColorProperty =
            DependencyProperty.Register("LabelBaseColor", typeof(Color), typeof(ThemeConfigModel), new PropertyMetadata(Colors.White));


        public Color LabelAccentColorA
        {
            get { return (Color)GetValue(LabelAccentColorAProperty); }
            set { SetValue(LabelAccentColorAProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LabelAccentColorA.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LabelAccentColorAProperty =
            DependencyProperty.Register("LabelAccentColorA", typeof(Color), typeof(ThemeConfigModel), new PropertyMetadata(Colors.White));


        public Color LabelAccentColorB
        {
            get { return (Color)GetValue(LabelAccentColorBProperty); }
            set { SetValue(LabelAccentColorBProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LabelAccentColorB.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LabelAccentColorBProperty =
            DependencyProperty.Register("LabelAccentColorB", typeof(Color), typeof(ThemeConfigModel), new PropertyMetadata(Colors.White));



        public bool HideFromStores
        {
            get { return (bool)GetValue(HideFromStoresProperty); }
            set { SetValue(HideFromStoresProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HideFromStores.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HideFromStoresProperty =
            DependencyProperty.Register("HideFromStores", typeof(bool), typeof(ThemeConfigModel), new PropertyMetadata(false));


        public event PropertyChangedEventHandler? PropertyChanged;

        public ThemeConfigModel() : base()
        {
            ParentPack = null!;
        }

        public ThemeConfigModel(SkinPackModel parent, string? skinName = null)
        {
            ParentPack = parent;
            ParentPack.SkinNameChanged += OnSkinNameChanged;
            ParentPack.PackComponents.CollectionChanged += OnPackComponentsChanged;

            ThemeName = skinName ?? string.Empty;
        }

        private void OnPackComponentsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is not null)
            {
                foreach (PackComponentModel item in e.OldItems)
                {
                    item.NameChanged -= OnSkinNameChanged;
                }
            }

            if (e.NewItems is not null)
            {
                foreach (PackComponentModel item in e.NewItems)
                {
                    item.NameChanged += OnSkinNameChanged;
                }
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableSkinNames)));
        }

        private void OnSkinNameChanged(object? sender, SkinNameChangedEventArgs e)
        {
            if (ThemeName == e.OldName)
            {
                ThemeName = e.NewName;
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableSkinNames)));
        }

        public ThemeConfigModel(SkinPackModel parent, ThemeConfigItem json, string dirPath)
        {
            ParentPack = parent;
            ThemeName = json.Name;
            if (!string.IsNullOrEmpty(json.LabelTextureFile))
            {
                string texturePath = Path.Combine(dirPath, json.LabelTextureFile);
                UpdateImageFile(texturePath);
            }

            LabelBaseColor = TryParseColor(json.LabelBaseColor);
            LabelAccentColorA = TryParseColor(json.LabelAccentColorA);
            LabelAccentColorB = TryParseColor(json.LabelAccentColorB);
        }

        private static Color TryParseColor(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                var parsed = System.Drawing.ColorTranslator.FromHtml(value);
                return parsed.ToMediaColor();
            }
            return Colors.White;
        }

        private static string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        public string PackagedLabelTexturePath
        {
            get
            {
                string? safeThemeName = ThemeName?.ToLower().Replace(' ', '_');
                return $"label_{safeThemeName}{Extension}";
            }
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(ThemeName);

        public ThemeConfigItem JsonModel()
        {
            var result = new ThemeConfigItem()
            {
                Name = ThemeName,
                HideFromStores = HideFromStores,
            };

            if (HasValidImage)
            {
                result.LabelTextureFile = PackagedLabelTexturePath;
            }
            else
            {
                result.LabelBaseColor = ColorToHex(LabelBaseColor);
                result.LabelAccentColorA = ColorToHex(LabelAccentColorA);
                result.LabelAccentColorB = ColorToHex(LabelAccentColorB);
            }

            return result;
        }

        protected void RaiseValidationChange()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsValid)));
        }
    }
}
