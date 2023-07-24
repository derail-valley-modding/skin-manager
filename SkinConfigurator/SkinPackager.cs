using SMShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SkinConfigurator
{
    internal abstract class SkinPackager : IDisposable
    {
        protected readonly string _destPath;
        protected readonly SkinPackModel _model;

        protected static readonly JsonSerializerOptions _serializeOptions = new()
        {
            WriteIndented = true,
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        /// <exception cref="SkinPackageException"></exception>
        public static void Package<T>(string path, SkinPackModel model) where T : SkinPackager
        {
            try
            {
                model.Trim();
                using var packager = (T)Activator.CreateInstance(typeof(T), new object[] { path, model })!;
                packager.WriteModel();
            }
            catch (Exception ex)
            {
                throw new SkinPackageException($"Error packaging skin mod {model.ModInfoModel.Id}: {ex.Message}", ex);
            }
        }

        public SkinPackager(string path, SkinPackModel model)
        {
            _destPath = path;
            _model = model;
        }

        private void WriteModel()
        {
            WriteModInfo();

            foreach (var skin in _model.SkinConfigModels)
            {
                WriteSkin(skin);
            }
        }

        protected abstract void WriteModInfo();

        protected abstract void WriteSkin(SkinConfigModel skin);

        protected static string GetTargetFileName(string folderName, string sourcePath, string liveryId)
        {
            if (Constants.IsSupportedExtension(Path.GetExtension(sourcePath)))
            {
                string filename = Path.GetFileNameWithoutExtension(sourcePath);
                string extension = Path.GetExtension(sourcePath);

                if (Remaps.TryGetUpdatedTextureName(liveryId, filename, out string newName))
                {
                    return Path.Combine(folderName, string.Concat(newName, extension));
                }
            }

            return Path.Combine(folderName, Path.GetFileName(sourcePath));
        }

        protected string GetSkinFolderName(string skinName, string liveryId)
        {
            if (_model.SkinConfigModels.Any(s => (s.Name == skinName) && (s.CarId != liveryId)))
            {
                // exporting same skin for another car type, prefix name
                return $"{skinName}_{liveryId}";
            }
            return skinName;
        }

        public virtual void Dispose() { }
    }

    internal class SkinPackageException : Exception
    {
        public SkinPackageException(string message) : base(message) { }

        public SkinPackageException(string message, Exception innerException) : base(message, innerException) { }
    }
}
