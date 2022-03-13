﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkinManagerMod
{
    [HarmonyPatch(typeof(CarSpawner), "SpawnCar")]
    class CarSpawner_SpawnCar_Patch
    {
        static void Postfix(TrainCar __result)
        {
            var skin = SkinManager.GetCurrentCarSkin(__result);
            SkinManager.ApplySkin(__result, skin);
        }
    }

    [HarmonyPatch(typeof(CarSpawner), "SpawnLoadedCar")]
    class CarSpawner_SpawnExistingCar_Patch
    {
        static void Postfix(TrainCar __result)
        {
            var skin = SkinManager.GetCurrentCarSkin(__result);
            SkinManager.ApplySkin(__result, skin);
        }
    }

    [HarmonyPatch(typeof(TrainCar), "LoadInterior")]
    class TrainCar_LoadInterior_Patch
    {
        static void Postfix(TrainCar __instance)
        {
            var skin = SkinManager.GetCurrentCarSkin(__instance);
            SkinManager.ApplySkinToInterior(__instance, skin);
        }
    }
}