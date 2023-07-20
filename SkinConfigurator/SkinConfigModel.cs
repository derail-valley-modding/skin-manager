using SMShared;
using System.ComponentModel;
using System.IO;
using System.Text.Json.Serialization;

namespace SkinConfigurator
{
    public class SkinConfigModel : SkinConfigBase, INotifyPropertyChanged, IPackageable
    {
        [JsonIgnore]
        public string FolderPath { get; private set; }

        [JsonIgnore]
        public string? BindingName
        {
            get => Name;
            set
            {
                Name = value!;
                RaisePropertyChanged(nameof(BindingName));
                RaisePropertyChanged(nameof(IsValid));
            }
        }

        [JsonIgnore]
        public string? BindingCarId
        {
            get => CarId;
            set
            {
                CarId = value!;
                RaisePropertyChanged(nameof(BindingCarId));
                RaisePropertyChanged(nameof(IsValid));
            }
        }

        [JsonIgnore]
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(Name) &&
            !string.IsNullOrWhiteSpace(CarId);

        public event PropertyChangedEventHandler? PropertyChanged;
        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public SkinConfigModel(string path)
        {
            FolderPath = path;
            Name = Path.GetFileName(path);
        }

        public void Trim()
        {
            
        }
    }
}
