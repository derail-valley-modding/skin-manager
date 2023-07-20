using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace SkinConfigurator
{
    [Serializable]
    public class SkinModInfoModel : INotifyPropertyChanged, IPackageable
    {
        public string? Id;
        public string? DisplayName;
        public string? Version = "1.0.0";
        public string? Author;
        public readonly string ManagerVersion = "0.26";
        public readonly string[] Requirements = { "SkinManagerMod" };

        public void Trim()
        {
            if (string.IsNullOrWhiteSpace(Author)) Author = null;
        }

        [JsonIgnore]
        public string? BindingName
        {
            get => DisplayName;
            set
            {
                DisplayName = value;
                Id = DisplayName?.Replace(' ', '_');
                RaisePropertyChanged(nameof(BindingName));
                RaisePropertyChanged(nameof(IsValid));
            }
        }

        [JsonIgnore]
        public string? BindingVersion
        {
            get => Version;
            set
            {
                Version = value;
                RaisePropertyChanged(nameof(BindingVersion));
                RaisePropertyChanged(nameof(IsValid));
            }
        }

        [JsonIgnore]
        public string? BindingAuthor
        {
            get => Author;
            set
            {
                Author = value;
                RaisePropertyChanged(nameof(BindingAuthor));
            }
        }

        [JsonIgnore]
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(DisplayName) && 
            !string.IsNullOrWhiteSpace(Version);

        public event PropertyChangedEventHandler? PropertyChanged;
        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
