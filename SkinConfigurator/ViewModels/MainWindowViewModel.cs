using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace SkinConfigurator.ViewModels
{
    public class MainWindowViewModel : DependencyObject
    {
        public SkinPackModel SkinPack
        {
            get { return (SkinPackModel)GetValue(SkinPackProperty); }
            set
            {
                SetValue(SkinPackProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for PackModel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SkinPackProperty =
            DependencyProperty.Register(nameof(SkinPack), typeof(SkinPackModel), typeof(MainWindowViewModel), new PropertyMetadata(null));



        public PackComponentModel? SelectedSkin
        {
            get { return GetValue(SelectedSkinProperty) as PackComponentModel; }
            set { SetValue(SelectedSkinProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedSkinConfig.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedSkinProperty =
            DependencyProperty.Register(nameof(SelectedSkin), typeof(PackComponentModel), typeof(MainWindowViewModel), new PropertyMetadata(defaultValue: null, OnSelectedSkinChanged));



        public SkinFileModel SelectedSkinFile
        {
            get { return (SkinFileModel)GetValue(SelectedSkinFileProperty); }
            set { SetValue(SelectedSkinFileProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedSkinFile.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedSkinFileProperty =
            DependencyProperty.Register("SelectedSkinFile", typeof(SkinFileModel), typeof(MainWindowViewModel), new PropertyMetadata(defaultValue: null));


        public ThemeConfigModel? SelectedThemeConfig
        {
            get { return (ThemeConfigModel)GetValue(SelectedThemeConfigProperty); }
            set { SetValue(SelectedThemeConfigProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedThemeConfig.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedThemeConfigProperty =
            DependencyProperty.Register("SelectedThemeConfig", typeof(ThemeConfigModel), typeof(MainWindowViewModel), new PropertyMetadata(defaultValue: null));


        private static void OnSelectedSkinChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var selected = e.NewValue as PackComponentModel;
            if (selected?.CarId != null && _defaultTextureMap.TryGetValue(selected.CarId, out string[]? names))
            {
                ((MainWindowViewModel)d).DefaultTextureNames = names;
            }
            else
            {
                ((MainWindowViewModel)d).DefaultTextureNames = Enumerable.Empty<string>();
            }
        }

        public IEnumerable<string> DefaultTextureNames
        {
            get { return (IEnumerable<string>)GetValue(DefaultTextureNamesProperty); }
            set { SetValue(DefaultTextureNamesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DefaultTextureNames.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DefaultTextureNamesProperty =
            DependencyProperty.Register("DefaultTextureNames", typeof(IEnumerable<string>), typeof(MainWindowViewModel), new PropertyMetadata(Enumerable.Empty<string>()));


        private static readonly Dictionary<string, string[]> _defaultTextureMap;

        static MainWindowViewModel()
        {
            string textureJsonPath = Path.Combine(Environment.CurrentDirectory, "tex_names.json");
            using var inFile = File.OpenRead(textureJsonPath);
            _defaultTextureMap = JsonSerializer.Deserialize<Dictionary<string, string[]>>(inFile)!;
        }

        public MainWindowViewModel()
        {
            Reset();
        }

        public void Reset()
        {
            SkinPack = new SkinPackModel();
        }

        public void AddComponent(string? carId, string? sourceFolder = null)
        {
            var newSkin = SkinPack.CreateSkinConfig(carId);
            if (sourceFolder != null)
            {
                newSkin.Name = Path.GetFileName(sourceFolder).Replace('_', ' ');
                newSkin.AddItemsFromFolder(sourceFolder);
            }
            SelectedSkin = newSkin;
        }

        public void RemoveSelectedComponent()
        {
            if (SelectedSkin != null)
            {
                SkinPack.RemoveSkin(SelectedSkin);
                SelectedSkin = null;
            }
        }

        public void AddSkinFile(string sourcePath)
        {
            if (SelectedSkin != null)
            {
                SelectedSkin.AddItem(sourcePath);
            }
        }

        public void AddThemeConfig(string? themeName = null)
        {
            var newConfig = SkinPack.CreateThemeConfig(themeName);
            SelectedThemeConfig = newConfig;
        }

        public void RemoveSelectedThemeConfig()
        {
            if (SelectedThemeConfig is not null)
            {
                SkinPack.RemoveThemeConfig(SelectedThemeConfig);
                SelectedThemeConfig = null;
            }
        }
    }
}
