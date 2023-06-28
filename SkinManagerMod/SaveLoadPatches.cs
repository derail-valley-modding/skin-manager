using System;
using System.Collections.Generic;
using DV.JObjectExtstensions;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace SkinManagerMod
{
    [HarmonyPatch(typeof(SaveGameManager), "Save")]
    class SaveGameManager_Save_Patch
    {
        static void Prefix( SaveGameManager __instance )
        {
            JObject carsSaveData = SkinManager.GetCarsSaveData();

            SaveGameManager.Instance.data.SetJObject("Mod_Skins", carsSaveData);
        }
    }

    [HarmonyPatch(typeof(CarsSaveManager), "Load")]
    class CarsSaveManager_Load_Patch
    {
        static void Prefix( JObject savedData )
        {
            if( savedData == null )
            {
                Main.Error("Given save data is null, loading will not be performed");
                return;
            }

            JObject carsSaveData = SaveGameManager.Instance.data.GetJObject("Mod_Skins");

            if( carsSaveData != null )
            {
                SkinManager.LoadCarsSaveData(carsSaveData);
            }
        }
    }

    class CarsSkinSaveData
    {
        public string guid;
        public string name;

        public CarsSkinSaveData( string guid, string name )
        {
            this.guid = guid;
            this.name = name;
        }
    }
}
