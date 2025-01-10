using DV.CabControls;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using SMShared;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace SkinManagerMod.Items
{
    [HarmonyPatch]
    internal static class InventoryPatches
    {
        #region On Load

        [HarmonyPatch(typeof(StartingItemsController), nameof(StartingItemsController.InstantiateItem))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> TranspileInstantiateItem(IEnumerable<CodeInstruction> instructions)
        {
            // replace Instantiate call

            CodeInstruction previous = null;
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Stloc_3)
                {
                    Main.LogVerbose("Patched instantiate item");
                    yield return new CodeInstruction(OpCodes.Ldarg_1); // push itemData
                    yield return CodeInstruction.Call(typeof(InventoryPatches), nameof(CustomInstantiateItem));
                    previous = instruction;
                }
                else
                {
                    if (previous != null) yield return previous;
                    previous = instruction;
                }
            }

            if (previous != null) yield return previous;
        }

        private static GameObject CustomInstantiateItem(GameObject prefab, Vector3 position, Quaternion rotation, StorageItemData itemData)
        {
            if (itemData.state.TryGetValue(Constants.CUSTOM_THEME_SAVEDATA_KEY, out JToken themeToken))
            {
                if ((string)themeToken is string themeName)
                {
                    if (SkinProvider.TryGetTheme(themeName, out var theme))
                    {
                        Main.Log($"Creating saved custom paint can for theme {themeName}");
                        return PaintFactory.InstantiateCustomCan(theme, position, rotation);
                    }
                    else
                    {
                        Main.Error($"Couldn't find theme {themeName} for custom paint can");
                    }
                }
            }

            return UnityEngine.Object.Instantiate(prefab, position, rotation);
        }

        #endregion
    }
}
