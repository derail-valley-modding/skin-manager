using SMShared;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace SkinConfigurator.ViewModels
{
    public abstract class HasImageFileModel : DependencyObject
    {
        public readonly Guid FileId;
        public string? Extension { get; protected set; }

        public string TempPath => Path.Combine(Environment.CurrentDirectory, "Temp", "Staging", FileId.ToString());


        public bool HasValidImage
        {
            get { return (bool)GetValue(HasValidImageProperty); }
            set { SetValue(HasValidImageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HasValidImage.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HasValidImageProperty =
            DependencyProperty.Register("HasValidImage", typeof(bool), typeof(HasImageFileModel), new PropertyMetadata(false));


        public BitmapImage? Preview
        {
            get { return (BitmapImage)GetValue(PreviewProperty); }
            set { SetValue(PreviewProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Preview.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PreviewProperty =
            DependencyProperty.Register(nameof(Preview), typeof(BitmapImage), typeof(HasImageFileModel), new PropertyMetadata(defaultValue: null));


        protected HasImageFileModel()
        {
            FileId = Guid.NewGuid();
        }

        public void UpdateImageFile(string? path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                File.Copy(path, TempPath, true);

                Extension = Path.GetExtension(path);
                HasValidImage = true;

                if (Constants.IsSupportedExtension(Extension))
                {
                    var img = new BitmapImage();
                    img.BeginInit();
                    img.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.UriSource = new Uri(TempPath);
                    img.EndInit();
                    Preview = img;
                }
                else
                {
                    Preview = null;
                }
            }
            else
            {
                Extension = null;
                HasValidImage = false;
                Preview = null;
            }
        }
    }
}
