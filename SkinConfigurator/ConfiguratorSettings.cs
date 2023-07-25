using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SkinConfigurator
{
    [Serializable]
    public class ConfiguratorSettings
    {
        private static string SettingsFile => Path.Combine(Environment.CurrentDirectory, "settings.json");
        private static JsonSerializerOptions _serializeOptions = new()
        {
            IncludeFields = true,
            WriteIndented = true,
        };

        public string DefaultSkinWorkFolder;
        public string DerailValleyDirectory;

        public ConfiguratorSettings()
        {
            DefaultSkinWorkFolder = Environment.CurrentDirectory;
            DerailValleyDirectory = SteamHelper.GetModsDirectory() ?? Environment.CurrentDirectory;
        }

        public ConfiguratorSettings(ConfiguratorSettings other)
        {
            DefaultSkinWorkFolder = other.DefaultSkinWorkFolder;
            DerailValleyDirectory = other.DerailValleyDirectory;
        }

        public static ConfiguratorSettings LoadConfig()
        {
            try
            {
                using var stream = File.OpenRead(SettingsFile);
                return JsonSerializer.Deserialize<ConfiguratorSettings>(stream, _serializeOptions) ?? new ConfiguratorSettings();
            }
            catch
            {
                return new ConfiguratorSettings();
            }
        }

        public static void SaveConfig(ConfiguratorSettings settings)
        {
            try
            {
                using var stream = File.Open(SettingsFile, FileMode.Create);
                JsonSerializer.Serialize(stream, settings, _serializeOptions);
            }
            catch
            {

            }
        }
    }
    
    public class SettingsModel : INotifyPropertyChanged
    {
        public ConfiguratorSettings Data { get; private set; }

        public SettingsModel(ConfiguratorSettings data)
        {
            Data = new ConfiguratorSettings(data);
        }

        [JsonIgnore]
        public string WorkingFolder
        {
            get => Data.DefaultSkinWorkFolder;
            set
            {
                Data.DefaultSkinWorkFolder = value;
                OnPropertyChanged(nameof(WorkingFolder));
            }
        }

        [JsonIgnore]
        public string DerailValleyDirectory
        {
            get => Data.DerailValleyDirectory;
            set
            {
                Data.DerailValleyDirectory = value;
                OnPropertyChanged(nameof(DerailValleyDirectory));
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
