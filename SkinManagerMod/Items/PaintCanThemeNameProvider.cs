using DV.Customization.Paint;
using DV.JObjectExtstensions;
using Newtonsoft.Json.Linq;
using SMShared;
using UnityEngine;

namespace SkinManagerMod.Items
{
    public class PaintCanThemeNameProvider : MonoBehaviour, IInventoryItemLocalizer
    {
        public PaintTheme theme;
        private ItemSaveData saveData;

        public string GetCustomDescription() => null;

        public string GetNameParam() => theme.LocalizedName;

        public void Awake()
        {
            saveData = GetComponent<ItemSaveData>();
            if (saveData)
            {
                saveData.ItemSaveDataRequested += OnItemSaveDataRequested;
            }
        }

        private JObject OnItemSaveDataRequested(JObject data)
        {
            data.SetString(Constants.CUSTOM_THEME_SAVEDATA_KEY, theme.name);
            return data;
        }
    }
}
