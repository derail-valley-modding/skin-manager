using DV.Shops;
using HarmonyLib;
using SkinManagerMod.Items;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace SkinManagerMod.Patches
{
    [HarmonyPatch]
    static internal class ShopPatches
    {
        [HarmonyPatch(typeof(Shop), nameof(Shop.Awake))]
        [HarmonyPostfix]
        public static void ShopAwake(Shop __instance)
        {
            var stocker = __instance.gameObject.AddComponent<ShopPaintCanStocker>();

            string name = __instance.gameObject.name;
            Main.Log($"Shop {name} awoken");
        }

        [HarmonyPatch(typeof(GlobalShopController), nameof(GlobalShopController.UpdateItemStocksOnGameLoad))]
        [HarmonyPrefix]
        public static void BeforeUpdateItemStocks()
        {
            if (!GlobalShopController.Instance) return;

            PaintFactory.InjectShopData();
        }


        private static readonly MethodInfo _itemPrefabNameGetter = 
            AccessTools.PropertyGetter(typeof(InventoryItemSpec), nameof(InventoryItemSpec.ItemPrefabName));


        [HarmonyPatch(typeof(GlobalShopController), nameof(GlobalShopController.InstantiatePurchasedItems), MethodType.Enumerator)]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> InstantiatePurchasedItems(IEnumerable<CodeInstruction> instructions)
        {
            // replace starred lines with custom logic
            //
            //   Vector3 position = zero + right * num;
            // * string itemPrefabName = item3.specs.ItemPrefabName;
            // * GameObject obj = Object.Instantiate(Resources.Load(itemPrefabName) as GameObject, position, Quaternion.identity);
            //   Transform transform = obj.transform;

            bool skippingInstantiate = false;
            foreach (var instruction in instructions)
            {
                if (skippingInstantiate)
                {
                    if (instruction.opcode == OpCodes.Dup)
                    {
                        yield return instruction;
                        skippingInstantiate = false;
                    }
                }
                else if (instruction.Calls(_itemPrefabNameGetter))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 9); // load position
                    yield return CodeInstruction.Call(typeof(ShopPatches), nameof(InstantiatePaintCan));

                    skippingInstantiate = true;
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        private static GameObject InstantiatePaintCan(InventoryItemSpec itemSpec, Vector3 position)
        {
            if (!(itemSpec is CustomPaintInventorySpec customSpec))
            {
                var prefab = Resources.Load(itemSpec.ItemPrefabName) as GameObject;
                return UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
            }

            return PaintFactory.InstantiateCustomCan(customSpec.Theme, position, Quaternion.identity);
        }

        [HarmonyPatch(typeof(GlobalShopController), nameof(GlobalShopController.GetShopItemData), typeof(InventoryItemSpec))]
        [HarmonyPrefix]
        private static bool GetShopItemData(GlobalShopController __instance, InventoryItemSpec itemSpec, ref ShopItemData __result)
        {
            __result = __instance.GetShopItemData(itemSpec.itemPrefabName);
            return false;
        }
    }
}
