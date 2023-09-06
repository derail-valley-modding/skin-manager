using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
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

        public List<PackComponentModel> ResourceOptions => PackComponents.Where(c => c.Type == PackComponentType.Resource).ToList();
        

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
            PackComponents.Add(skin);
            RaisePropertyChanged(nameof(IsValid));
        }

        public void RemoveSkin(PackComponentModel component)
        {
            component.PropertyChanged -= HandleChildPropertyChanged;
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
    }
}
