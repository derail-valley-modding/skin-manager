using SMShared;
using SMShared.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace SkinConfigurator.ViewModels
{
    public class PackComponentModel : INotifyPropertyChanged, IValidated
    {
        public PackComponentModel() { }

        public PackComponentModel(PackComponentType type, ResourceConfigJson json)
        {
            Type = type;
            Name = json.Name;
            CarId = json.CarId;
        }

        public ResourceConfigJson JsonModel()
        {
            if (Type == PackComponentType.Skin)
            {
                return new SkinConfigJson
                {
                    Name = Name,
                    CarId = CarId,
                    ResourceNames = Resources?.Select(r => r.Name).ToArray(),
                };
            }
            else
            {
                return new ResourceConfigJson
                {
                    Name = Name,
                    CarId = CarId
                };
            }
        }

        public ObservableCollection<SkinFileModel> Items { get; private set; } = new ();

        public bool CanUpgrade => Items.Any(file => file.CanUpgradeFileName);


        private PackComponentType _type = PackComponentType.Skin;
        public PackComponentType Type
        {
            get => _type;
            set
            {
                SetValue(nameof(Type), ref _type, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasResources)));
            }
        }

        public bool HasResources => _type == PackComponentType.Skin;

        private string? _name;
        public string? Name
        {
            get => _name;
            set => SetValidationValue(nameof(Name), ref _name, value);
        }

        private string? _carId;
        public string? CarId
        {
            get => _carId;
            set => SetValidationValue(nameof(CarId), ref _carId, value);
        }

        private IList<PackComponentModel>? _resources;
        public IList<PackComponentModel>? Resources
        {
            get => _resources;
            set
            {
                SetValue(nameof(Resources), ref _resources, value);
            }
        }

        public SkinFileModel AddItem(string filePath)
        {
            var item = new SkinFileModel(this, filePath);
            AddItem(item);
            return item;
        }

        public void AddItem(SkinFileModel file)
        {
            file.FileNameChanged += HandleFileChanged;
            Items.Add(file);
            file.Parent = this;
            HandleFileChanged();
        }

        public void AddItemsFromFolder(string folderPath)
        {
            foreach (string filePath in Directory.EnumerateFiles(folderPath))
            {
                if (Constants.IsSkinConfigFile(filePath)) continue;

                AddItem(filePath);
            }
        }

        public void RemoveItem(SkinFileModel item)
        {
            item.FileNameChanged -= HandleFileChanged;
            Items.Remove(item);
            HandleFileChanged();
        }

        private void HandleFileChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanUpgrade)));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void SetValue<T>(string propertyName, ref T destValue, T newValue)
        {
            if (!Equals(newValue, destValue))
            {
                destValue = newValue;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected void SetValidationValue<T>(string propertyName, ref T destValue, T newValue)
        {
            if (!Equals(newValue, destValue))
            {
                destValue = newValue;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsValid)));
            }
        }

        public virtual bool IsValid =>
            !string.IsNullOrWhiteSpace(Name) &&
            !string.IsNullOrWhiteSpace(CarId);
    }

    public enum PackComponentType
    {
        Skin = 0,
        Resource = 1,
    }
}
