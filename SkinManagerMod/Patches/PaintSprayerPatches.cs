using DV.Customization.Paint;
using DV.Interaction;
using DV.InventorySystem;
using DV.Items;
using HarmonyLib;
using UnityEngine;

namespace SkinManagerMod.Patches
{
    [HarmonyPatch(typeof(PaintSprayer))]
    internal static class PaintSprayerPatches
    {
        [HarmonyPatch(nameof(PaintSprayer.Apply))]
        [HarmonyPrefix]
        private static bool ApplyTheme(PaintSprayer __instance, TrainCarPaint target, PaintCan ___insertedCan)
        {
            __instance.paintingProgress = 0f;

            var paintCan = ___insertedCan;
            var train = TrainCar.Resolve(target.gameObject);

            if (!SkinProvider.IsThemeable(train.carLivery) && (paintCan.theme.name == SkinProvider.DefaultNewThemeName))
            {
                target.CurrentTheme = SkinProvider.CustomDefaultTheme;
            }
            else
            {
                target.CurrentTheme = ___insertedCan.theme;
            }
            __instance.UnUse();
            __instance.ignoreMagazineChange = true;
            __instance.magazine.RemoveItem(0, activateItem: true, dropItem: true);
            __instance.ignoreMagazineChange = false;

            // empty can
            if (paintCan.emptyCanPrefab != null)
            {
                GameObject emptyCan = Object.Instantiate(paintCan.emptyCanPrefab, __instance.unloadAnchor.transform.position, __instance.unloadAnchor.transform.rotation).gameObject;
                RespawnOnDrop comp = emptyCan.GetComponent<RespawnOnDrop>();
                if (comp != null)
                {
                    comp.respawnOnDropThroughFloor = false;
                    comp.ignoreDistanceFromSpawnPosition = true;
                }
                emptyCan.GetComponent<InventoryItemSpec>().BelongsToPlayer = true;
                __instance.ignoreMagazineChange = true;
                __instance.magazine.AddItem(emptyCan, 0);
                __instance.ignoreMagazineChange = false;
                __instance.insertedCan = emptyCan.GetComponent<PaintCan>();
                __instance.paintSprayerEffects.OnInserted(emptyCan.transform, playSound: false);
                __instance.paintSprayerEffects.OnSpent();
            }
            else
            {
                __instance.insertedCan = null;
            }
            Inventory.Instance.DestroyItem(paintCan.gameObject);

            return false;
        }
    }
}
