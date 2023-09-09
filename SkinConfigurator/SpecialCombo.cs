using System;
using System.Collections.Specialized;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Data;

namespace SkinConfigurator
{
    public class SpecialCombo : ComboBox
    {
        private bool _ignoreSelectionChanged = false;

        private static readonly MethodInfo _selectedItemUpdated =
            typeof(ComboBox).GetMethod("SelectedItemUpdated", BindingFlags.NonPublic | BindingFlags.Instance)!;

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            if (!_ignoreSelectionChanged)
            {
                base.OnSelectionChanged(e);

                var textBinding = BindingOperations.GetBindingExpression(this, TextProperty);
                if (textBinding == null) return;

                _selectedItemUpdated.Invoke(this, Array.Empty<object>());
                textBinding.UpdateSource();
            }
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            _ignoreSelectionChanged = true;
            try
            {
                base.OnItemsChanged(e);
            }
            finally
            {
                _ignoreSelectionChanged = false;
            }
        }
    }
}
