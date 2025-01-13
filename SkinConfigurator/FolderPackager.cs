using SkinConfigurator.ViewModels;
using SMShared;
using SMShared.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SkinConfigurator
{
    internal class FolderPackager : SkinPackager
    {
        public FolderPackager(string path, SkinPackModel model) : base(path, model)
        {
            Directory.CreateDirectory(path);
            ClearDirectory(_destPath);
        }

        private static void ClearDirectory(string dirPath)
        {
            foreach (var file in Directory.EnumerateFiles(dirPath))
            {
                File.Delete(file);
            }
            foreach (var dir in Directory.EnumerateDirectories(dirPath))
            {
                Directory.Delete(dir, true);
            }
        }

        private string GetAbsoluteDestination(string relativeDest) => Path.Combine(_destPath, relativeDest);

        protected override void WriteModInfo()
        {
            string dest = GetAbsoluteDestination(Constants.MOD_INFO_FILE);
            using var stream = File.Open(dest, FileMode.Create);
            JsonSerializer.Serialize(stream, _model.ModInfoModel.JsonModel(), JsonSettings);
        }

        protected override void WriteSkin(PackComponentModel skin)
        {
            string folderName = GetSkinFolderName(skin.Name!, skin.CarId!);
            string folderPath = GetAbsoluteDestination(folderName);
            Directory.CreateDirectory(folderPath);

            string jsonFileName = skin.Type == PackComponentType.Skin ? Constants.SKIN_CONFIG_FILE : Constants.SKIN_RESOURCE_FILE;
            string jsonPath = Path.Combine(folderPath, jsonFileName);
            using var jsonStream = File.Open(jsonPath, FileMode.Create);

            var json = skin.JsonModel();
            JsonSerializer.Serialize(jsonStream, json, json.GetType(), JsonSettings);

            // textures & whatever else
            foreach (var sourceFile in skin.Items)
            {
                string relativePath = Path.Combine(folderName, sourceFile.FileName);
                string absPath = GetAbsoluteDestination(relativePath);
                File.Copy(sourceFile.TempPath, absPath, true);
            }
        }

        protected override void WriteThemeConfig()
        {
            string dest = GetAbsoluteDestination(Constants.THEME_CONFIG_FILE);

            var json = new ThemeConfigJson()
            {
                Version = _model.ModInfoModel.Version,
                Themes = new ThemeConfigItem[_model.ThemeConfigs.Count],
            };

            for (int i = 0; i < _model.ThemeConfigs.Count; i++)
            {
                var config = _model.ThemeConfigs[i];
                if (config.UseCustomTexture)
                {
                    string destPath = config.PackagedLabelTexturePath;
                    File.Copy(config.TempPath, destPath, true);
                }
                json.Themes[i] = config.JsonModel();
            }

            using var stream = File.Open(dest, FileMode.Create);
            JsonSerializer.Serialize(stream, json, json.GetType(), JsonSettings);
        }
    }
}
