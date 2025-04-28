using DV.CabControls;
using DV.Items;
using DV.JObjectExtstensions;
using DV.Shops;
using DV.Utils;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using SkinManagerMod.Items;
using UnityEngine;

namespace SkinManagerMod.Patches
{
    // reloadablesocket is obsolete, replaced by container system i think
    [HarmonyPatch(typeof(ItemContainer))]
    internal static class ItemContainerPatches
    {
        [HarmonyPatch(nameof(ItemContainer.OnItemSaveDataLoaded))]
        [HarmonyPostfix]
        private static void SaveDataLoaded(ItemContainer __instance, JObject data)
        {
            data = data.GetJObject(ItemContainer.CONTAINER_ID_SAVE_KEY);
            if (data is null)
            {
                return;
            }

            string prefabName = data.GetString("prefabName") ?? string.Empty;
            GameObject can;

            if (PaintFactory.TryParseDummyPrefabName(prefabName, out string? themeName) &&
                SkinProvider.TryGetTheme(themeName!, out var theme))
            {
                // skin manager can
                can = PaintFactory.InstantiateCustomCan(theme, __instance.transform.position, Quaternion.identity);
            }
            else
            {
                // other item
                GameObject prefab = Resources.Load<GameObject>(prefabName);
                if (!prefab)
                {
                    Debug.LogError($"[Reloadable] Couldn't find item prefab with the name '{prefabName}'. Loading of item '{prefabName}' skipped.");
                    return;
                }
                can = Object.Instantiate(prefab, __instance.transform.position, Quaternion.identity);
            }

            can.name = prefabName;
            can.GetComponent<InventoryItemSpec>().BelongsToPlayer = data.GetBool("belongsToPlayer") ?? false;

            ItemSaveData saveData = can.GetComponent<ItemSaveData>();
            saveData?.LoadItemData(data.GetJObject("data"));
        }
    }


    /*[HarmonyPatch(typeof(ReloadableSocket))]
    internal static class ReloadableSocketPatches
    {
        [HarmonyPatch(nameof(ReloadableSocket.SaveDataLoaded))]
        [HarmonyPrefix]
        private static bool SaveDataLoaded(ReloadableSocket __instance, JObject data)
        {
            data = data.GetJObject(ReloadableSocket.KEY_INSERTED_ITEM);
            if (data is null)
            {
                return false;
            }

            string prefabName = data.GetString(ReloadableSocket.KEY_ITEM_PREFAB_NAME) ?? string.Empty;
            GameObject can;

            if (PaintFactory.TryParseDummyPrefabName(prefabName, out string? themeName) &&
                SkinProvider.TryGetTheme(themeName!, out var theme))
            {
                // skin manager can
                can = PaintFactory.InstantiateCustomCan(theme, __instance.transform.position, Quaternion.identity);
            }
            else
            {
                // other item
                GameObject prefab = Resources.Load<GameObject>(prefabName);
                if (!prefab)
                {
                    Debug.LogError($"[Reloadable] Couldn't find item prefab with the name '{prefabName}'. Loading of item '{prefabName}' skipped.");
                    return false;
                }
                can = Object.Instantiate(prefab, __instance.transform.position, Quaternion.identity);
            }

            can.name = prefabName;
            can.GetComponent<InventoryItemSpec>().BelongsToPlayer = data.GetBool(ReloadableSocket.KEY_ITEM_BELONGS_TO_PLAYER) ?? false;

            ItemSaveData saveData = can.GetComponent<ItemSaveData>();
            saveData?.LoadItemData(data.GetJObject(ReloadableSocket.KEY_ITEM_SAVE_DATA));

            if (!can.TryGetComponent<Reloadable>(out var reloadableScript))
            {
                Debug.LogError($"[Reloadable] Loaded reloadable '{prefabName}' is no longer a reloadable! Moving to Lost and Found.");
                StorageController.Instance.AddItemToLostAndFound(can.GetComponent<ItemBase>());
            }
            else
            {
                __instance.Inserted = reloadableScript;
            }

            return false;
        }
    }*/
}
