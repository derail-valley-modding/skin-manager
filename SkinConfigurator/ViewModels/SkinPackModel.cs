using SMShared.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace SkinConfigurator.ViewModels
{
    public class SkinPackModel : DependencyObject, INotifyPropertyChanged
    {
        public SkinPackModel()
        {
            ModInfoModel = new();
        }

        public SkinModInfoModel ModInfoModel
        {
            get { return (SkinModInfoModel)GetValue(ModInfoModelProperty); }
            set
            {
                if (GetValue(ModInfoModelProperty) is SkinModInfoModel prevVal)
                {
                    prevVal.PropertyChanged -= HandleChildPropertyChanged;
                }
                SetValue(ModInfoModelProperty, value);
                value.PropertyChanged += HandleChildPropertyChanged;
            }
        }

        // Using a DependencyProperty as the backing store for ModInfoModel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModInfoModelProperty =
            DependencyProperty.Register("ModInfoModel", typeof(SkinModInfoModel), typeof(SkinPackModel), new PropertyMetadata(null));


        public ObservableCollection<PackComponentModel> PackComponents { get; private set; } = new();

        public ObservableCollection<ThemeConfigModel> ThemeConfigs { get; private set; } = new();

        public List<PackComponentModel> ResourceOptions => PackComponents.Where(c => c.Type == PackComponentType.Resource).ToList();

        public event EventHandler<SkinNameChangedEventArgs>? SkinNameChanged;

        public PackComponentModel CreateSkinConfig(string? carId)
        {
            var skin = new PackComponentModel()
            {
                CarId = carId
            };
            AddSkinConfig(skin);
            return skin;
        }

        public void AddSkinConfig(PackComponentModel skin)
        {
            skin.PropertyChanged += HandleChildPropertyChanged;
            skin.NameChanged += OnSkinNameChanged;

            PackComponents.Add(skin);
            RaisePropertyChanged(nameof(IsValid));
        }

        public void RemoveSkin(PackComponentModel component)
        {
            component.PropertyChanged -= HandleChildPropertyChanged;
            component.NameChanged -= OnSkinNameChanged;

            PackComponents.Remove(component);
            RaisePropertyChanged(nameof(IsValid));
        }

        public bool IsValid => ModInfoModel.IsValid && PackComponents.All(s => s.IsValid);


        public event PropertyChangedEventHandler? PropertyChanged;
        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void HandleChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IValidated.IsValid))
            {
                RaisePropertyChanged(nameof(IsValid));
            }
        }

        private void OnSkinNameChanged(object? sender, SkinNameChangedEventArgs e)
        {
            SkinNameChanged?.Invoke(sender, e);
        }

        public ThemeConfigModel CreateThemeConfig(string? themeName = null)
        {
            if (themeName is null)
            {
                PackComponentModel? toAdd = PackComponents.Where(comp => comp.Type == PackComponentType.Skin)
                .FirstOrDefault(skin => !ThemeConfigs.Any(theme => theme.ThemeName == skin.Name));
                themeName = toAdd?.Name;
            }
            
            var config = new ThemeConfigModel(this, themeName);
            ThemeConfigs.Add(config);
            return config;
        }

        public void ImportThemeConfig(string jsonPath)
        {
            string folder = Path.GetDirectoryName(jsonPath)!;

            using FileStream themeStream = File.OpenRead(jsonPath);
            var themeJson = JsonSerializer.Deserialize<ThemeConfigJson>(themeStream, SkinPackager.JsonSettings);

            if ((themeJson?.Themes is not null) && (themeJson.Themes.Length > 0))
            {
                foreach (var themeItem in themeJson.Themes)
                {
                    var themeModel = new ThemeConfigModel(this, themeItem, folder);
                    ThemeConfigs.Add(themeModel);
                }
            }
        }

        public void AddThemeConfig(ThemeConfigModel theme)
        {
            ThemeConfigs.Add(theme);
        }

        public void RemoveThemeConfig(ThemeConfigModel toRemove)
        {
            ThemeConfigs.Remove(toRemove);
        }
    }
}
