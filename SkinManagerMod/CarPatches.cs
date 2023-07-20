using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SkinManagerMod
{
    [HarmonyPatch]
    class CarSpawner_SpawnCar_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(CarSpawner), nameof(CarSpawner.SpawnCar));
            yield return AccessTools.Method(typeof(CarSpawner), nameof(CarSpawner.SpawnLoadedCar));
        }

        static void Postfix(TrainCar __result)
        {
            var skin = SkinManager.GetCurrentCarSkin(__result);
            if ((skin != null) && !skin.IsDefault)
            {
                // only need to replace textures if not staying with default skin
                SkinManager.ApplySkin(__result, skin);
            }
        }
    }

    [HarmonyPatch]
    class TrainCar_LoadInterior_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(TrainCar), nameof(TrainCar.LoadInterior));
            yield return AccessTools.Method(typeof(TrainCar), nameof(TrainCar.LoadExternalInteractables));
            yield return AccessTools.Method(typeof(TrainCar), nameof(TrainCar.LoadDummyExternalInteractables));
        }

        static void Postfix(TrainCar __instance)
        {
            var skin = SkinManager.GetCurrentCarSkin(__instance);
            if ((skin != null) && !skin.IsDefault)
            {
                SkinManager.ApplySkinToInterior(__instance, skin);
            }
        }
    }
}
