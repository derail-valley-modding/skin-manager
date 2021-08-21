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
            JObject carsSaveData = GetCarsSaveData();

            SaveGameManager.data.SetJObject("Mod_Skins", carsSaveData);
        }

        static JObject GetCarsSaveData()
        {
            JObject carsSaveData = new JObject();

            JObject[] array = new JObject[Main.trainCarState.Count];

            int i = 0;

            foreach( KeyValuePair<string, string> entry in Main.trainCarState )
            {
                JObject dataObject = new JObject();

                dataObject.SetString("guid", entry.Key);
                dataObject.SetString("name", entry.Value);

                array[i] = dataObject;

                i++;
            }

            JObject[] skinArray = new JObject[Main.selectedSkin.Count];

            i = 0;

            foreach( KeyValuePair<TrainCarType, string> entry in Main.selectedSkin )
            {
                JObject dataObject = new JObject();

                dataObject.SetInt("type", (int)entry.Key);
                dataObject.SetString("skin", entry.Value);

                skinArray[i] = dataObject;
                i++;
            }

            carsSaveData.SetJObjectArray("carsData", array);
            carsSaveData.SetJObjectArray("carSkins", skinArray);

            return carsSaveData;
        }
    }

    [HarmonyPatch(typeof(CarsSaveManager), "Load")]
    class CarsSaveManager_Load_Patch
    {
        static void Prefix( JObject savedData )
        {
            if( savedData == null )
            {
                Debug.LogError((object)"Given save data is null, loading will not be performed");
                return;
            }

            JObject carsSaveData = SaveGameManager.data.GetJObject("Mod_Skins");

            if( carsSaveData != null )
            {
                JObject[] jobjectArray = carsSaveData.GetJObjectArray("carsData");

                if( jobjectArray != null )
                {
                    foreach( JObject jobject in jobjectArray )
                    {
                        var guid = jobject.GetString("guid");
                        var name = jobject.GetString("name");

                        if( !Main.trainCarState.ContainsKey(guid) )
                        {
                            Main.trainCarState.Add(guid, name);
                        }
                    }
                }

                JObject[] jobjectSkinArray = carsSaveData.GetJObjectArray("carSkins");

                if( jobjectArray != null )
                {
                    foreach( JObject jobject in jobjectSkinArray )
                    {
                        TrainCarType type = (TrainCarType)jobject.GetInt("type").Value;
                        string skin = jobject.GetString("skin");

                        if( Main.selectedSkin.ContainsKey(type) )
                        {
                            Main.selectedSkin[type] = skin;
                        }
                    }
                }
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
