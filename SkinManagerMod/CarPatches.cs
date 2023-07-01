using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkinManagerMod
{
    [HarmonyPatch(typeof(CarSpawner))]
    class CarSpawner_SpawnCar_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(CarSpawner.SpawnCar))]
        [HarmonyPatch(nameof(CarSpawner.SpawnLoadedCar))]
        static void SpawnCar(TrainCar __result)
        {
            var skin = SkinManager.GetCurrentCarSkin(__result);
            if ((skin != null) && !skin.IsDefault)
            {
                // only need to replace textures if not staying with default skin
                SkinManager.ApplySkin(__result, skin);
            }
        }
    }

    [HarmonyPatch(typeof(TrainCar))]
    class TrainCar_LoadInterior_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(TrainCar.LoadInterior))]
        [HarmonyPatch(nameof(TrainCar.LoadExternalInteractables))]
        static void LoadInterior(TrainCar __instance)
        {
            var skin = SkinManager.GetCurrentCarSkin(__instance);
            if ((skin != null) && !skin.IsDefault)
            {
                SkinManager.ApplySkinToInterior(__instance, skin);
            }
        }
    }
}
