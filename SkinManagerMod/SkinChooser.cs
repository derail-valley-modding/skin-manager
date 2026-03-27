using System;
using System.Linq;

namespace SkinManagerMod
{
    public abstract class SkinChooser : IComparable<SkinChooser>
    {
        private string? _cachedId = null;

        /// <summary>
        /// Unique identifier for this skin chooser. Defaults to the class name.
        /// </summary>
        public virtual string Id
        {
            get
            {
                _cachedId ??= GetType().Name;
                return _cachedId;
            }
        }

        /// <summary>
        /// Whether this class is provided by Skin Manager. Do not override this.
        /// </summary>
        internal virtual bool IsBuiltIn => false;

        /// <summary>
        /// List of SkinChooser Ids that this class should take priority over.
        /// External choosers are prioritized over built-in SM ones by default.
        /// </summary>
        public virtual string[] PrioritizeOver => Array.Empty<string>();

        /// <summary>
        /// List of SkinChooser Ids that this class should be run after.
        /// External choosers are prioritized over built-in SM ones by default.
        /// </summary>
        public virtual string[] PrioritizeUnder => Array.Empty<string>();

        /// <summary>
        /// Implement this method to choose a skin for the given car.
        /// </summary>
        /// <returns>False if this class has no skin preference</returns>
        public abstract bool TryChooseSkin(TrainCar car, out string? skinName);


        public int CompareTo(SkinChooser other)
        {
            // evaluate explicit ordering rules
            if (PrioritizeOver.Contains(other.Id) || other.PrioritizeUnder.Contains(Id)) return -1;
            if (other.PrioritizeOver.Contains(Id) || PrioritizeUnder.Contains(other.Id)) return 1;

            // prioritize externally added choosers over the built in ones
            if (!IsBuiltIn && other.IsBuiltIn) return -1;
            if (IsBuiltIn && !other.IsBuiltIn) return 1;

            return 0;
        }
    }
}