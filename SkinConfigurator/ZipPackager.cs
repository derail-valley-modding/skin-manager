using SMShared;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
            JsonSerializer.Serialize(stream, _model.ModInfoModel, _serializeOptions);
        }

        protected override void WriteSkin(SkinConfigModel skin)
        {
            // skin.json
            string folderName = GetSkinFolderName(skin.Name, skin.CarId);
            var jsonEntry = _archive.CreateEntry(Path.Combine(folderName, Constants.SKIN_CONFIG_FILE));
            using var jsonStream = jsonEntry.Open();
            JsonSerializer.Serialize(jsonStream, skin, _serializeOptions);
            jsonStream.Close();

            // textures & whatever else
            foreach (var sourceFile in Directory.EnumerateFiles(skin.FolderPath))
            {
                string relativePath = GetTargetFileName(folderName, sourceFile, skin.CarId);
                _archive.CreateEntryFromFile(sourceFile, relativePath);
            }
        }
    }
}
