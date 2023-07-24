using SMShared;
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
            JsonSerializer.Serialize(stream, _model.ModInfoModel, _serializeOptions);
        }

        protected override void WriteSkin(SkinConfigModel skin)
        {
            string folderName = GetSkinFolderName(skin.Name, skin.CarId);
            string folderPath = GetAbsoluteDestination(folderName);
            Directory.CreateDirectory(folderPath);

            string jsonPath = Path.Combine(folderPath, Constants.SKIN_CONFIG_FILE);
            using var jsonStream = File.Open(jsonPath, FileMode.Create);
            JsonSerializer.Serialize(jsonStream, skin, _serializeOptions);

            // textures & whatever else
            foreach (var sourceFile in Directory.EnumerateFiles(skin.FolderPath))
            {
                string relativePath = GetTargetFileName(folderName, sourceFile, skin.CarId);
                string absPath = GetAbsoluteDestination(relativePath);
                File.Copy(sourceFile, absPath, true);
            }
        }
    }
}
