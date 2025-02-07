using HarmonyLib;
using Newtonsoft.Json.Linq;
using SkinManagerMod.Items;
using SMShared;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace SkinManagerMod.Patches
{
    [HarmonyPatch]
    internal static class InventoryPatches
    {
        #region On Load

        [HarmonyPatch(typeof(StartingItemsController), nameof(StartingItemsController.InstantiateItem))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> TranspileInstantiateItem(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // replace first chunk of method that checks for a valid prefab
            
            // push itemData [StorageItemData]
            yield return new CodeInstruction(OpCodes.Ldarg_1);

            // push this [StartingItemsController]
            yield return new CodeInstruction(OpCodes.Ldarg_0);

            // pop/push this.instantiatedItemCount
            var itemCountField = AccessTools.Field(typeof(StartingItemsController), nameof(StartingItemsController.instantiatedItemCount));
            yield return new CodeInstruction(OpCodes.Ldfld, itemCountField);

            // pop/pop/call CustomInstantiateItem(itemData, instantiatedItemCount) => push result [GameObject]
            yield return CodeInstruction.Call(typeof(InventoryPatches), nameof(CustomInstantiateItem));

            // if (result == null) return null;
            Label? notNullLabel = generator.DefineLabel();

            yield return new CodeInstruction(OpCodes.Dup);
            yield return new CodeInstruction(OpCodes.Brtrue_S, notNullLabel);

            yield return new CodeInstruction(OpCodes.Ret);

            // skip everything up until the instantiate call in original method, then continue with the original code
            bool skipping = true;
            foreach (var instruction in instructions)
            {
                if (skipping)
                {
                    if (instruction.Calls(nameof(Object.Instantiate)))
                    {
                        skipping = false;
                    }
                }
                else
                {
                    if (notNullLabel.HasValue)
                    {
                        yield return instruction.WithLabels(notNullLabel.Value);
                        notNullLabel = null;
                    }
                    else
                    {
                        yield return instruction;
                    }
                }
            }
        }

        private static GameObject? CustomInstantiateItem(StorageItemData itemData, int instantiatedItemCount)
        {
            Vector3 position = StartingItemsController.ITEM_INSTANTIATION_SAFETY_POSITION +
                (StartingItemsController.ITEM_INSTANTIATION_SAFETY_OFFSET * instantiatedItemCount);

            string prefabName = itemData.itemPrefabName;

            // check for skin manager paint can theme key
            if (itemData.state != null && itemData.state.TryGetValue(Constants.CUSTOM_THEME_SAVEDATA_KEY, out JToken? themeToken))
            {
                if ((string?)themeToken is string themeName)
                {
                    if (SkinProvider.TryGetTheme(themeName, out var theme))
                    {
                        Main.Log($"Creating saved custom paint can for theme {themeName}");
                        return PaintFactory.InstantiateCustomCan(theme, position, Quaternion.identity);
                    }
                    else
                    {
                        Main.Error($"Couldn't find theme {themeName} for custom paint can");
                        prefabName = PaintFactory.DEFAULT_CAN_PREFAB_NAME;
                    }
                }
            }

            // not a skin manager item, fallback to default
            if (string.IsNullOrEmpty(prefabName))
            {
                return null;
            }

            if (Resources.Load(prefabName) is not GameObject prefab)
            {
                Debug.LogError($"Couldn't find item prefab with the name '{prefabName}'. Loading of item '{prefabName}' skipped.");
                return null;
            }

            return Object.Instantiate(prefab, position, Quaternion.identity);
        }

        #endregion

        private static bool Calls(this CodeInstruction instruction, string methodName)
        {
            if (instruction.operand is MethodInfo method)
            {
                return method.Name == methodName;
            }
            return false;
        }
    }
}
