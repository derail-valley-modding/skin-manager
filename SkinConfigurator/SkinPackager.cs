using SkinConfigurator.ViewModels;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SkinConfigurator
{
    internal abstract class SkinPackager : IDisposable
    {
        protected readonly string _destPath;
        protected readonly SkinPackModel _model;

        public static readonly JsonSerializerOptions JsonSettings = new()
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

            foreach (var skin in _model.PackComponents)
            {
                WriteSkin(skin);
            }

            WriteThemeConfig();
        }

        protected abstract void WriteModInfo();

        protected abstract void WriteSkin(PackComponentModel skin);

        protected abstract void WriteThemeConfig();

        protected string GetSkinFolderName(string skinName, string liveryId)
        {
            if (_model.PackComponents.Any(s => (s.Name == skinName) && (s.CarId != liveryId)))
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
