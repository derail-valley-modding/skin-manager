using SkinConfigurator.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SkinConfigurator
{
    /// <summary>
    /// Interaction logic for ResourceSelector.xaml
    /// </summary>
    public partial class ResourceSelector : UserControl
    {
        public ResourceSelector()
        {
            SelectionItems = new();
            InitializeComponent();
        }



        public PackComponentModel Skin
        {
            get { return (PackComponentModel)GetValue(SkinProperty); }
            set { SetValue(SkinProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Skin.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SkinProperty =
            DependencyProperty.Register("Skin", typeof(PackComponentModel), typeof(ResourceSelector), new PropertyMetadata(OnSelectedSkinChanged));

        private static void OnSelectedSkinChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var selector = (ResourceSelector)d;

            selector.RefreshAvailableItems();
        }


        public SkinPackModel SkinPack
        {
            get { return (SkinPackModel)GetValue(SkinPackProperty); }
            set { SetValue(SkinPackProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SkinPack.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SkinPackProperty =
            DependencyProperty.Register("SkinPack", typeof(SkinPackModel), typeof(ResourceSelector), new PropertyMetadata(OnSkinPackChanged));


        private void AvailableComponentsChanged(object? sender, EventArgs e)
        {
            RefreshAvailableItems();
        }

        private static void OnSkinPackChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var selector = (ResourceSelector)d;
            if (e.OldValue is SkinPackModel oldPack)
            {
                oldPack.PackComponents.CollectionChanged -= selector.AvailableComponentsChanged;
                oldPack.SkinTypeChanged -= selector.AvailableComponentsChanged;
            }
            
            selector.RefreshAvailableItems();

            if (e.NewValue is SkinPackModel newPack)
            {
                newPack.PackComponents.CollectionChanged += selector.AvailableComponentsChanged;
                newPack.SkinTypeChanged += selector.AvailableComponentsChanged;
            }
        }

        public ObservableCollection<ResourceSelectItem> SelectionItems
        {
            get { return (ObservableCollection<ResourceSelectItem>)GetValue(SelectionItemsProperty); }
            set { SetValue(SelectionItemsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectionItems.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectionItemsProperty =
            DependencyProperty.Register("SelectionItems", typeof(ObservableCollection<ResourceSelectItem>), typeof(ResourceSelector), new PropertyMetadata(defaultValue: null));


        public void RefreshAvailableItems()
        {
            IEnumerable<PackComponentModel> available;

            if ((SkinPack is not null) && (Skin is not null))
            {
                available = SkinPack.PackComponents
                    .Where(c => (c.Type == PackComponentType.Resource) && !string.IsNullOrWhiteSpace(c.Name) && (c.CarId == Skin.CarId))
                    .OrderBy(c => c.Name)
                    .ToList();

                if (Skin.Resources is IList<PackComponentModel> selected)
                {
                    foreach (var currentSelected in selected.ToList())
                    {
                        if (!available.Contains(currentSelected))
                        {
                            Skin.Resources.Remove(currentSelected);
                        }
                    }
                }
            }
            else
            {
                available = Enumerable.Empty<PackComponentModel>();
            }

            SelectionItems.Clear();

            foreach (var item in available)
            {
                var model = new ResourceSelectItem(item);
                SelectionItems.Add(model);
                model.IsSelectedChanged += OnSelectStatusChanged;
            }

            RefreshSelectedItems();
        }

        private bool _codeRefreshing = false;
        private void RefreshSelectedItems()
        {
            if (Skin?.Resources is null)
            {
                return;
            }

            _codeRefreshing = true;

            foreach (var selectItem in SelectionItems)
            {
                selectItem.Selected = Skin.Resources.Contains(selectItem.Resource);
            }
            _codeRefreshing = false;
        }

        private void OnSelectStatusChanged(PackComponentModel resource, bool nowSelected)
        {
            if (_codeRefreshing) return;

            Skin.Resources ??= new ObservableCollection<PackComponentModel>();

            if (nowSelected && !Skin.Resources.Contains(resource))
            {
                Skin.Resources.Add(resource);
            }
            else if (!nowSelected)
            {
                Skin.Resources.Remove(resource);
            }
        }
    }

    public class ResourceSelectItem : DependencyObject
    {
        public event Action<PackComponentModel, bool>? IsSelectedChanged;

        public bool Selected
        {
            get { return (bool)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Selected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedProperty =
            DependencyProperty.Register("Selected", typeof(bool), typeof(ResourceSelectItem), new PropertyMetadata(false, OnSelectedChanged));

        private static void OnSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var item = (ResourceSelectItem)d;
            item.IsSelectedChanged?.Invoke(item.Resource, item.Selected);
        }


        public PackComponentModel Resource
        {
            get { return (PackComponentModel)GetValue(ResourceProperty); }
            set { SetValue(ResourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Resource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ResourceProperty =
            DependencyProperty.Register("Resource", typeof(PackComponentModel), typeof(ResourceSelectItem), new PropertyMetadata(defaultValue: null));


        public ResourceSelectItem(PackComponentModel resource, bool isSelected = false)
        {
            Resource = resource;
            Selected = isSelected;
        }
    }
}
