using SMShared;
using System;
using System.IO;
using System.Windows;

namespace SkinConfigurator.ViewModels
{
    public class SkinFileModel : HasImageFileModel
    {
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
                CanUpgradeFileName = Remaps.TryGetUpdatedTextureName(Parent.CarId!, Path.GetFileNameWithoutExtension(value), out _);
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



        public SkinFileModel(PackComponentModel parent, string path) : base()
        {
            Parent = parent;
            FileName = Path.GetFileName(path);

            UpdateImageFile(path);
            Extension ??= Path.GetExtension(path);
        }

        public void UpgradeFileName()
        {
            string name = Path.GetFileNameWithoutExtension(FileName);

            if (Remaps.TryGetUpdatedTextureName(Parent.CarId!, name, out string? newName))
            {
                FileName = $"{newName}{Extension}";
            }
        }
    }
}
