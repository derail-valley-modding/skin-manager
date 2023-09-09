using SMShared;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SkinConfigurator.ViewModels;

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
    }
}
