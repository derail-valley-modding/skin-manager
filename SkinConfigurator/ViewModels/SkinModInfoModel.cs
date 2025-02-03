using SMShared.Json;
using System.ComponentModel;

namespace SkinConfigurator.ViewModels
{
    public class SkinModInfoModel : INotifyPropertyChanged, IValidated
    {
        public SkinModInfoModel() { }

        public SkinModInfoModel(ModInfoJson modInfo)
        {
            DisplayName = modInfo.DisplayName;
            Version = modInfo.Version;
            Author = modInfo.Author;
        }

        public ModInfoJson JsonModel()
        {
            return new ModInfoJson
            {
                Id = Id,
                DisplayName = DisplayName,
                Version = Version!,
                Author = string.IsNullOrWhiteSpace(Author) ? null : Author,
            };
        }

        public string? Id => DisplayName?.Replace(' ', '_');

        private string? _displayName;
        public string? DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value;
                RaisePropertyChanged(nameof(DisplayName));
                RaisePropertyChanged(nameof(IsValid));
            }
        }

        private string? _version;
        public string? Version
        {
            get => _version;
            set
            {
                _version = value;
                RaisePropertyChanged(nameof(Version));
                RaisePropertyChanged(nameof(IsValid));
            }
        }

        private string? _author;
        public string? Author
        {
            get => _author;
            set
            {
                _author = value;
                RaisePropertyChanged(nameof(Author));
            }
        }

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
