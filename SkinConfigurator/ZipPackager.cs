using SkinConfigurator.ViewModels;
using SMShared;
using SMShared.Json;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace SkinConfigurator
{
    internal class ZipPackager : SkinPackager
    {
        private readonly FileStream _stream;
        private readonly ZipArchive _archive;

        public ZipPackager(string archivePath, SkinPackModel model) : base(archivePath, model)
        {
            if (File.Exists(archivePath))
            {
                File.Delete(archivePath);
            }

            _stream = File.OpenWrite(archivePath);
            _archive = new ZipArchive(_stream, ZipArchiveMode.Create);
        }

        public override void Dispose()
        {
            _archive.Dispose();
            _stream.Dispose();
        }

        protected override void WriteModInfo()
        {
            using var stream = _archive.CreateEntry(Constants.MOD_INFO_FILE).Open();
            JsonSerializer.Serialize(stream, _model.ModInfoModel.JsonModel(), JsonSettings);
        }

        protected override void WriteSkin(PackComponentModel skin)
        {
            // skin.json
            string folderName = GetSkinFolderName(skin.Name!, skin.CarId!);
            string jsonFileName = skin.Type == PackComponentType.Skin ? Constants.SKIN_CONFIG_FILE : Constants.SKIN_RESOURCE_FILE;
            var jsonEntry = _archive.CreateEntry($"{folderName}/{jsonFileName}");
            using var jsonStream = jsonEntry.Open();

            var json = skin.JsonModel();
            JsonSerializer.Serialize(jsonStream, json, json.GetType(), JsonSettings);
            jsonStream.Close();

            // textures & whatever else
            foreach (var sourceFile in skin.Items)
            {
                string entryPath = $"{folderName}/{sourceFile.FileName}";
                _archive.CreateEntryFromFile(sourceFile.TempPath, entryPath);
            }
        }

        protected override void WriteThemeConfig()
        {
            var json = new ThemeConfigJson()
            {
                Version = _model.ModInfoModel.Version,
                Themes = new ThemeConfigItem[_model.ThemeConfigs.Count],
            };

            // label texture files
            for (int i = 0; i < _model.ThemeConfigs.Count; i++)
            {
                var config = _model.ThemeConfigs[i];
                if (config.HasValidImage)
                {
                    string destPath = config.PackagedLabelTexturePath;
                    _archive.CreateEntryFromFile(config.TempPath, destPath);
                }
                json.Themes[i] = config.JsonModel();
            }

            var jsonEntry = _archive.CreateEntry(Constants.THEME_CONFIG_FILE);
            using var stream = jsonEntry.Open();

            JsonSerializer.Serialize(stream, json, json.GetType(), JsonSettings);
        }
    }
}
