using DV.Customization.Paint;
using DV.Interaction;
using DV.InventorySystem;
using DV.Items;
using HarmonyLib;

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

            // empty can
            if (paintCan.emptyCanPrefab != null)
            {
                Reloadable reloadable = UnityEngine.Object.Instantiate(paintCan.emptyCanPrefab);
                RespawnOnDrop component = reloadable.GetComponent<RespawnOnDrop>();
                if (component != null)
                {
                    component.respawnOnDropThroughFloor = false;
                    component.ignoreDistanceFromSpawnPosition = true;
                }
                reloadable.GetComponent<InventoryItemSpec>().BelongsToPlayer = true;
                __instance.socket.Inserted = reloadable;
            }
            else
            {
                __instance.socket.Inserted = null;
            }
            Inventory.Instance.DestroyItem(paintCan.gameObject);

            return false;
        }
    }
}
