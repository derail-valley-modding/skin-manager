using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace SkinConfigurator
{
    public class SkinPackModel : DependencyObject, INotifyPropertyChanged, IPackageable
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


        public ObservableCollection<SkinConfigModel> SkinConfigModels { get; private set; } = new();

        public void AddSkinConfig(string path, string? carId)
        {
            var skin = new SkinConfigModel(path)
            {
                CarId = carId!
            };
            skin.PropertyChanged += HandleChildPropertyChanged;

            SkinConfigModels.Add(skin);
            SelectedSkinConfig = skin;

            RaisePropertyChanged(nameof(IsValid));
        }

        public void RemoveSelectedSkin()
        {
            if (SelectedSkinConfig != null)
            {
                SelectedSkinConfig.PropertyChanged -= HandleChildPropertyChanged;
                SkinConfigModels.Remove(SelectedSkinConfig);
                SelectedSkinConfig = null;

                RaisePropertyChanged(nameof(IsValid));
            }
        }

        public SkinConfigModel? SelectedSkinConfig
        {
            get { return GetValue(SelectedSkinConfigProperty) as SkinConfigModel; }
            set { SetValue(SelectedSkinConfigProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedSkinConfig.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedSkinConfigProperty =
            DependencyProperty.Register("SelectedSkinConfig", typeof(SkinConfigModel), typeof(SkinPackModel), new PropertyMetadata(null));

        public bool IsValid => ModInfoModel.IsValid && SkinConfigModels.All(s => s.IsValid);


        public event PropertyChangedEventHandler? PropertyChanged;
        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void HandleChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IPackageable.IsValid))
            {
                RaisePropertyChanged(nameof(IsValid));
            }
        }

        public void Trim()
        {
            ModInfoModel.Trim();
            foreach (var skin in SkinConfigModels)
            {
                skin.Trim();
            }
        }
    }
}
