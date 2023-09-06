using SMShared;
using System;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Media.Imaging;

namespace SkinConfigurator.ViewModels
{
    public class SkinFileModel : DependencyObject
    {
        public readonly Guid FileId;
        public string Extension { get; private set; }

        public string TempPath => Path.Combine(Environment.CurrentDirectory, "Temp", "Staging", FileId.ToString());

        public BitmapImage Preview
        {
            get { return (BitmapImage)GetValue(PreviewProperty); }
            set { SetValue(PreviewProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Preview.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PreviewProperty =
            DependencyProperty.Register(nameof(Preview), typeof(BitmapImage), typeof(SkinFileModel), new PropertyMetadata(null));


        public PackComponentModel Parent
        {
            get { return (PackComponentModel)GetValue(ParentProperty); }
            set { SetValue(ParentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Parent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ParentProperty =
            DependencyProperty.Register(nameof(Parent), typeof(PackComponentModel), typeof(SkinFileModel), new PropertyMetadata(null));


        public event Action? FileNameChanged;

        public string FileName
        {
            get { return (string)GetValue(FileNameProperty); }
            set 
            {
                SetValue(FileNameProperty, value);
                CanUpgradeFileName = Remaps.TryGetUpdatedTextureName(Parent.CarId, Path.GetFileNameWithoutExtension(value), out _);
                FileNameChanged?.Invoke();
            }
        }

        // Using a DependencyProperty as the backing store for FileName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FileNameProperty =
            DependencyProperty.Register(nameof(FileName), typeof(string), typeof(SkinFileModel), new PropertyMetadata(string.Empty));


        public bool CanUpgradeFileName
        {
            get { return (bool)GetValue(CanUpgradeFileNameProperty); }
            set { SetValue(CanUpgradeFileNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CanUpgradeFileName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CanUpgradeFileNameProperty =
            DependencyProperty.Register(nameof(CanUpgradeFileName), typeof(bool), typeof(SkinFileModel), new PropertyMetadata(false));



        public SkinFileModel(PackComponentModel parent, string path)
        {
            FileId = Guid.NewGuid();
            Parent = parent;
            FileName = Path.GetFileName(path);

            UpdateSourceFile(path);
            Extension ??= Path.GetExtension(path);
        }

        public void UpdateSourceFile(string path)
        {
            File.Copy(path, TempPath, true);

            Extension = Path.GetExtension(path);
            if (Constants.IsSupportedExtension(Extension))
            {
                var img = new BitmapImage();
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.UriSource = new Uri(TempPath);
                img.EndInit();
                Preview = img;
            }
            else
            {
                Preview = new BitmapImage();
            }
        }

        public void Destroy()
        {
            if (File.Exists(TempPath))
            {
                File.Delete(TempPath);
            }
        }

        public void UpgradeFileName()
        {
            string name = Path.GetFileNameWithoutExtension(FileName);

            if (Remaps.TryGetUpdatedTextureName(Parent.CarId, name, out string newName))
            {
                FileName = $"{newName}{Extension}";
            }
        }
    }
}
