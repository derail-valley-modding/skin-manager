using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using UnityModManagerNet;
using Harmony12;
using UnityEngine;
using Newtonsoft.Json;

namespace SkinManagerMod
{
    public class Main
    {
        public static Dictionary<string, string> trainCarState = new Dictionary<string, string>();

        public static string modPath;

        static Vector2 scrollViewVector = Vector2.zero;

        static Dictionary<TrainCarType, string> prefabMap;

        static TrainCarType trainCarSelected = TrainCarType.NotSet;

        static bool showDropdown = false;

        static List<string> excludedExports = new List<string>
        {
            "bogie2_d",
            "car_lods",
            "SH_glass_01d",
            "windows_01d",
            "window_d"
        };

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Type type = typeof(CarTypes);
            FieldInfo fieldInfo = type.GetField("prefabMap", BindingFlags.Static | BindingFlags.NonPublic);

            prefabMap = fieldInfo.GetValue(null) as Dictionary<TrainCarType, string>;

            modPath = modEntry.Path;

            LoadSkins();

            modEntry.OnGUI = OnGUI;

            return true; // If false the mod will show an error.
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(420));

            GUILayout.BeginVertical();

            if (GUILayout.Button(trainCarSelected == TrainCarType.NotSet ? "Select Train Car" : prefabMap[trainCarSelected], GUILayout.Width(220)))
            {
                showDropdown = !showDropdown;
            }

            if (showDropdown)
            {
                scrollViewVector = GUILayout.BeginScrollView(scrollViewVector, GUILayout.Height(200), GUILayout.Width(220));

                foreach (var entry in prefabMap)
                {
                    if (GUILayout.Button(entry.Value, GUILayout.Width(200)))
                    {
                        showDropdown = false;
                        trainCarSelected = entry.Key;
                    }
                }

                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();

            if (trainCarSelected != TrainCarType.NotSet)
            {
                if (GUILayout.Button("Dump Textures", GUILayout.Width(200)))
                {
                    DumpTextures(trainCarSelected);
                }
            }

            GUILayout.EndHorizontal();
        }

        public static void DumpTextures(TrainCarType trainCar)
        {
            var obj = CarTypes.GetCarPrefab(trainCar);

            var path = modPath + "Dump\\" + obj.name;

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var cmps = obj.GetComponentsInChildren<MeshRenderer>();
            var textureList = new Dictionary<string, Texture>();

            foreach (var cmp in cmps)
            {
                if (!cmp.material || !cmp.material.mainTexture)
                {
                    continue;
                }

                var mainTexture = cmp.material.mainTexture;

                if (mainTexture && mainTexture is Texture2D && !textureList.ContainsKey(mainTexture.name))
                {
                    textureList.Add(mainTexture.name, mainTexture);

                    if (!excludedExports.Contains(mainTexture.name))
                    {
                        SaveTextureAsPNG(DuplicateTexture(mainTexture as Texture2D), path + "\\" + mainTexture.name + ".png");
                    }
                }
            }
        }

        public static void SaveTextureAsPNG(Texture2D _texture, string _fullPath)
        {
            byte[] _bytes = _texture.EncodeToPNG();
            File.WriteAllBytes(_fullPath, _bytes);
            Debug.Log(_bytes.Length / 1024 + "Kb was saved as: " + _fullPath);
        }

        static Texture2D DuplicateTexture(Texture2D source)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(
                        source.width,
                        source.height,
                        0,
                        RenderTextureFormat.Default,
                        RenderTextureReadWrite.Linear);

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(source.width, source.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            return readableText;
        }

        static Dictionary<TrainCarType, SkinGroup> skinGroups = new Dictionary<TrainCarType, SkinGroup>();

        static void LoadSkins()
        {
            foreach (var prefab in prefabMap)
            {
                var dir = modPath + "Skins\\" + prefab.Value;

                if (Directory.Exists(dir))
                {
                    var subDirectories = Directory.GetDirectories(dir);
                    var skinGroup = new SkinGroup(prefab.Key);
                    var carPrefab = CarTypes.GetCarPrefab(prefab.Key);
                    var cmps = carPrefab.gameObject.GetComponentsInChildren<MeshRenderer>();

                    foreach (var subDir in subDirectories)
                    {
                        var dirInfo = new DirectoryInfo(subDir);
                        var files = Directory.GetFiles(subDir);
                        var skin = new Skin(dirInfo.Name);

                        foreach(var file in files)
                        {
                            FileInfo fileInfo = new FileInfo(file);
                            string fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
                            byte[] fileData = File.ReadAllBytes(file);
                            Texture2D texture = null;

                            foreach (var cmp in cmps)
                            {
                                if (cmp.material.mainTexture.name == fileName)
                                {
                                    texture = DuplicateTexture(cmp.material.mainTexture as Texture2D);
                                    texture.LoadImage(fileData);

                                    break;
                                }
                            }

                            if (texture)
                            {
                                var skinTexture = new SkinTexture(fileName, texture);

                                skin.skinTextures.Add(skinTexture);
                            }
                        }

                        skinGroup.skins.Add(skin);
                    }

                    skinGroups.Add(prefab.Key, skinGroup);
                }
            }
        }

        public static void ReplaceTexture(TrainCar trainCar)
        {
            if (!skinGroups.ContainsKey(trainCar.carType))
            {
                return;
            }

            var skinGroup = skinGroups[trainCar.carType];

            if (skinGroup.skins.Count == 0)
            {
                return;
            }

            Skin skin;

            if (trainCarState.ContainsKey(trainCar.logicCar.carGuid))
            {
                string skinName = trainCarState[trainCar.logicCar.carGuid];

                if (skinName == "__default")
                {
                    return;
                }

                skin = skinGroup.GetSkin(skinName);

                if (skin == null)
                {
                    return;
                }
            }
            else
            {
                var range = UnityEngine.Random.Range(0, skinGroup.skins.Count + 1);

                // default skin if it hits out of index
                if (range == skinGroup.skins.Count)
                {
                    SetCarState(trainCar.logicCar.carGuid, "__default");

                    return;
                }

                skin = skinGroup.skins[range];

                SetCarState(trainCar.logicCar.carGuid, skin.name);
            }

            var cmps = trainCar.gameObject.GetComponentsInChildren<MeshRenderer>();

            foreach (var cmp in cmps)
            {
                var mainTextureName = cmp.material.mainTexture.name;

                if (skin.ContainsTexture(mainTextureName))
                {
                    var skinTexture = skin.GetTexture(mainTextureName);

                    cmp.material.mainTexture = skinTexture.textureData;
                }
            }
        }

        public static void SetCarState(string guid, string name)
        {
            if (trainCarState.ContainsKey(guid))
            {
                trainCarState[guid] = name;
            } else
            {
                trainCarState.Add(guid, name);
            }
        }
    }

    public class SkinGroup
    {
        TrainCarType trainCarType;
        public List<Skin> skins = new List<Skin>();

        public SkinGroup(TrainCarType trainCarType)
        {
            this.trainCarType = trainCarType;
        }

        public Skin GetSkin(string name)
        {
            foreach(var skin in skins)
            {
                if (skin.name == name)
                {
                    return skin;
                }
            }

            return null;
        }
    }

    public class Skin
    {
        public string name;
        public List<SkinTexture> skinTextures = new List<SkinTexture>();

        public Skin(string name)
        {
            this.name = name;
        }

        public bool ContainsTexture(string name)
        {
            foreach(var tex in skinTextures)
            {
                if (tex.name == name)
                {
                    return true;
                }
            }

            return false;
        }

        public SkinTexture GetTexture(string name)
        {
            foreach (var tex in skinTextures)
            {
                if (tex.name == name)
                {
                    return tex;
                }
            }

            return null;
        }
    }

    public class SkinTexture
    {
        public string name;
        public Texture2D textureData;

        public SkinTexture(string name, Texture2D textureData)
        {
            this.name = name;
            this.textureData = textureData;
        }
    }

    [HarmonyPatch(typeof(CarSpawner), "SpawnCar")]
    class CarSpawner_SpawnCar_Patch
    {
        static void Postfix(TrainCar __result)
        {
            Main.ReplaceTexture(__result);
        }
    }

    [HarmonyPatch(typeof(CarSpawner), "SpawnExistingCar")]
    class CarSpawner_SpawnExistingCar_Patch
    {
        static void Postfix(TrainCar __result)
        {
            Main.ReplaceTexture(__result);
        }
    }

    [HarmonyPatch(typeof(SaveGameManager), "Save")]
    class SaveGameManager_Save_Patch
    {
        static void Prefix(ShopInstantiator __instance)
        {
            CarsSkinSaveData[] carsSaveData = GetCarsSaveData();

            SaveGameManager.data.SetObject("Mod_Skins", carsSaveData, (JsonSerializerSettings)null);
        }

        static CarsSkinSaveData[] GetCarsSaveData()
        {
            CarsSkinSaveData[] carsSkinSaveDatas = new CarsSkinSaveData[Main.trainCarState.Count];

            int i = 0;

            foreach (KeyValuePair<string, string> entry in Main.trainCarState)
            {
                carsSkinSaveDatas[i] = new CarsSkinSaveData(entry.Key, entry.Value);
                i++;
            }

            return carsSkinSaveDatas;
        }
    }

    [HarmonyPatch(typeof(CarsSaveManager), "Load")]
    class CarsSaveManager_Load_Patch
    {
        static void Prefix(CarsSaveData savedData)
        {
            if (savedData == null)
            {
                Debug.LogError((object)"Given save data is null, loading will not be performed");
                return;
            }
            if (savedData.tracksHash != CarsSaveManager.TracksHash)
            {
                Debug.LogWarning((object)"Given save data was made in a different scene, loading will not be performed");
                Debug.Log((object)("DEBUG: Current rail track hash '" + CarsSaveManager.TracksHash + "' doesn't match save data hash '" + savedData.tracksHash + "', will not load"));
                return;
            }

            CarsSkinSaveData[] carsSaveData = SaveGameManager.data.GetObject<CarsSkinSaveData[]>("Mod_Skins");

            if (carsSaveData != null)
            {
                foreach(var carSave in carsSaveData)
                {
                    if (!Main.trainCarState.ContainsKey(carSave.guid))
                    {
                        Main.trainCarState.Add(carSave.guid, carSave.name);
                    }
                }
            }
        }
    }

    class CarsSkinSaveData
    {
        public string guid;
        public string name;

        public CarsSkinSaveData(string guid, string name)
        {
            this.guid = guid;
            this.name = name;
        }
    }
}