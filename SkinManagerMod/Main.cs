using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DV.JObjectExtstensions;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityModManagerNet;

namespace SkinManagerMod
{
    public class Main
    {
		public static Dictionary<string, string> trainCarState = new Dictionary<string, string>();

        public static string modPath;

        static Vector2 scrollViewVector = Vector2.zero;

        public static Dictionary<TrainCarType, string> prefabMap;

        static TrainCarType trainCarSelected = TrainCarType.NotSet;

        static bool showDropdown = false;

        public static Dictionary<TrainCarType, SkinGroup> skinGroups = new Dictionary<TrainCarType, SkinGroup>();

        public static Dictionary<TrainCarType, string> selectedSkin = new Dictionary<TrainCarType, string>();

		public static Settings settings;

		static List<string> excludedExports = new List<string>
        {
        };

        static Dictionary<string, string> aliasNames = new Dictionary<string, string>
        {
            { "exterior_d", "body" },
            { "LocoDiesel_exterior_d", "body" },
            { "SH_exterior_d", "body" },
            { "SH_tender_01d", "body" }
        };
        
        static bool Load(UnityModManager.ModEntry modEntry)
        {
			// Load the settings
			settings = Settings.Load<Settings>(modEntry);

            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Type type = typeof(CarTypes);
            FieldInfo fieldInfo = type.GetField("prefabMap", BindingFlags.Static | BindingFlags.NonPublic);

            prefabMap = fieldInfo.GetValue(null) as Dictionary<TrainCarType, string>;

            modPath = modEntry.Path;

			LoadSkins();

            foreach (TrainCarType carType in prefabMap.Keys)
            {
                selectedSkin.Add(carType, "Random");
            }

            modEntry.OnGUI = OnGUI;
			// OnSaveGUI
			modEntry.OnSaveGUI = OnSaveGUI;

			QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;

            return true;
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
		{
			GUILayout.BeginVertical();

			bool newAniso = GUILayout.Toggle(settings.aniso5, "Increase Anisotropic Filtering (Requires Manual Game Restart)");
			if (newAniso != settings.aniso5)
			{
				settings.aniso5 = newAniso;
			}
            settings.parallelLoading = GUILayout.Toggle(settings.parallelLoading, "Multi-threaded texture loading");
			GUILayout.Space(2);

			GUILayout.Label("Export Textures");

            GUILayout.BeginHorizontal(GUILayout.Width(250));

			GUILayout.BeginVertical();

            if (GUILayout.Button(trainCarSelected == TrainCarType.NotSet ? "Select Train Car" : prefabMap[trainCarSelected], GUILayout.Width(220)))
            {
                showDropdown = !showDropdown;
            }

            if (showDropdown)
            {
                scrollViewVector = GUILayout.BeginScrollView(scrollViewVector, GUILayout.Height(350));

                foreach (var entry in prefabMap)
                {
                    if (GUILayout.Button(entry.Value, GUILayout.Width(220)))
                    {
                        showDropdown = false;
                        trainCarSelected = entry.Key;
                    }
                }

                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();

            GUILayout.EndVertical();

			if (trainCarSelected != TrainCarType.NotSet)
            {
                if (GUILayout.Button("Export Textures", GUILayout.Width(180)))
                {
                    DumpTextures(trainCarSelected);
                }
            }

            GUILayout.EndHorizontal();
        }

		static void OnSaveGUI(UnityModManager.ModEntry modEntry)
		{
			settings.Save(modEntry);
		}

        public static void DumpTextures(TrainCarType trainCarType)
        {
            MeshRenderer[] cmps;
            Dictionary<string, Texture> textureList;

            var obj = CarTypes.GetCarPrefab(trainCarType);

            var path = modPath + "Exported\\" + obj.name;

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            cmps = obj.GetComponentsInChildren<MeshRenderer>();
            textureList = new Dictionary<string, Texture>();

            foreach (var cmp in cmps)
            {
                if (!cmp.material)
                {
                    continue;
                }

                var diffuse = GetMaterialTexture(cmp, "_MainTex");
                var normal = GetMaterialTexture(cmp, "_BumpMap");
                var specular = GetMaterialTexture(cmp, "_MetallicGlossMap");
                var emission = GetMaterialTexture(cmp, "_EmissionMap");

                ExportTexture(path, diffuse, textureList);
                ExportTexture(path, normal, textureList, true);
                ExportTexture(path, specular, textureList);
                ExportTexture(path, emission, textureList);
            }

            var trainCar = obj.GetComponent<TrainCar>();

            if (trainCar.interiorPrefab != null)
            {
                cmps = trainCar.interiorPrefab.GetComponentsInChildren<MeshRenderer>();
                textureList = new Dictionary<string, Texture>();

                foreach (var cmp in cmps)
                {
                    if (!cmp.material)
                    {
                        continue;
                    }

                    var diffuse = GetMaterialTexture(cmp, "_MainTex");
                    var normal = GetMaterialTexture(cmp, "_BumpMap");
                    var specular = GetMaterialTexture(cmp, "_MetallicGlossMap");
                    var emission = GetMaterialTexture(cmp, "_EmissionMap");

                    ExportTexture(path, diffuse, textureList);
                    ExportTexture(path, normal, textureList, true);
                    ExportTexture(path, specular, textureList);
                    ExportTexture(path, emission, textureList);
                }
            }
        }

        public static Texture2D GetMaterialTexture(MeshRenderer cmp, string materialName)
        {
            if (cmp.material == null || !cmp.material.HasProperty(materialName))
            {
                return null;
            }

            var texture = cmp.material.GetTexture(materialName);

            if (texture is Texture2D)
            {
                return (Texture2D)texture;
            } else
            {
                return null;
            }
        }

        public static void ExportTexture(string path, Texture2D texture, Dictionary<string, Texture> textureList, bool isNormal = false)
        {
            if (texture != null && !textureList.ContainsKey(texture.name))
            {
                textureList.Add(texture.name, texture);

                if (!excludedExports.Contains(texture.name))
                {
                    var tex = DuplicateTexture(texture as Texture2D, isNormal);

                    if (isNormal)
                    {
                        tex = DTXnm2RGBA(tex);
                    }

                    SaveTextureAsPNG(tex, path + "\\" + texture.name + ".png");
                }
            }
        }

        public static void SaveTextureAsPNG(Texture2D _texture, string _fullPath)
        {
            byte[] _bytes = _texture.EncodeToPNG();
            File.WriteAllBytes(_fullPath, _bytes);
            Debug.Log(_bytes.Length / 1024 + "Kb was saved as: " + _fullPath);
        }

        static Texture2D DuplicateTexture(Texture2D source, bool isNormal)
        {
            RenderTexture renderTex;
            Texture2D readableText;

            if (isNormal)
            {
                readableText = new Texture2D(source.width, source.height, TextureFormat.ARGB32, true, true);
                renderTex = RenderTexture.GetTemporary(
                            source.width,
                            source.height,
                            0,
                            RenderTextureFormat.ARGB32,
                            RenderTextureReadWrite.Linear);
            } else
            {
                readableText = new Texture2D(source.width, source.height);
                renderTex = RenderTexture.GetTemporary(
                            source.width,
                            source.height,
                            0,
                            RenderTextureFormat.Default);
            }

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            return readableText;
        }

        public static Dictionary<TrainCarType, Skin> defaultSkins = new Dictionary<TrainCarType, Skin>();

        private static Skin CreateDefaultSkin( TrainCarType carType, string typeName )
        {
            GameObject carPrefab = CarTypes.GetCarPrefab(carType);
            if( carPrefab == null ) return null;

            Skin defSkin = new Skin($"Default_{typeName}");

            var cmps = carPrefab.gameObject.GetComponentsInChildren<MeshRenderer>();
            foreach( var cmp in cmps )
            {
                if( !cmp.material ) continue;

                if( GetMaterialTexture(cmp, "_MainTex") is Texture2D diffuse )
                    defSkin.skinTextures.Add(new SkinTexture(diffuse.name, diffuse));

                if( GetMaterialTexture(cmp, "_BumpMap") is Texture2D normal )
                    defSkin.skinTextures.Add(new SkinTexture(normal.name, normal));

                if( GetMaterialTexture(cmp, "_MetallicGlossMap") is Texture2D specular )
                    defSkin.skinTextures.Add(new SkinTexture(specular.name, specular));

                if( GetMaterialTexture(cmp, "_EmissionMap") is Texture2D emission )
                    defSkin.skinTextures.Add(new SkinTexture(emission.name, emission));
            }

            var trainCar = carPrefab.GetComponent<TrainCar>();

            if( trainCar?.interiorPrefab != null )
            {
                foreach( var cmp in trainCar.interiorPrefab.GetComponentsInChildren<MeshRenderer>() )
                {
                    if( !cmp.material ) continue;

                    if( GetMaterialTexture(cmp, "_MainTex") is Texture2D diffuse )
                        defSkin.skinTextures.Add(new SkinTexture(diffuse.name, diffuse));

                    if( GetMaterialTexture(cmp, "_BumpMap") is Texture2D normal )
                        defSkin.skinTextures.Add(new SkinTexture(normal.name, normal));

                    if( GetMaterialTexture(cmp, "_MetallicGlossMap") is Texture2D specular )
                        defSkin.skinTextures.Add(new SkinTexture(specular.name, specular));

                    if( GetMaterialTexture(cmp, "_EmissionMap") is Texture2D emission )
                        defSkin.skinTextures.Add(new SkinTexture(emission.name, emission));
                }
            }

            return defSkin;
        }

        static readonly List<string> TextureNames = new List<string>() { "_MainTex", "_BumpMap", "_MetallicGlossMap", "_EmissionMap" };

        static void BeginLoadSkin(SkinGroup skinGroup, IEnumerable<MeshRenderer> cmps, string subDir)
        {
            var dirInfo = new DirectoryInfo(subDir);
            var files = dirInfo.GetFiles();
            var skin = new Skin(dirInfo.Name);

            var loading = new HashSet<string>();

            bool Matches(Texture2D texture, string fileName)
            {
                return texture != null &&
                    !loading.Contains(texture.name) &&
                    (texture.name == fileName ||
                        (aliasNames.TryGetValue(texture.name, out var alias) && alias == fileName));
            }

            foreach (var file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file.Name);

                foreach (var cmp in cmps)
                {
                    foreach (var textureName in TextureNames)
                    {
                        var texture = GetMaterialTexture(cmp, textureName);
                        if (Matches(texture, fileName))
                        {
                            var linear = textureName == "_BumpMap";
                            loading.Add(texture.name);
                            if (settings.parallelLoading)
                            {
                                skin.skinTextures.Add(new SkinTexture(fileName, TextureLoader.Add(file, linear)));
                            }
                            else
                            {
                                var tex = new Texture2D(0, 0, textureFormat: TextureFormat.RGBA32, mipChain: true, linear: linear);
                                tex.LoadImage(File.ReadAllBytes(file.FullName));
                                skin.skinTextures.Add(new SkinTexture(fileName, tex));
                            }
                        }
                    }
                }
            }

            skinGroup.skins.Add(skin);
        }

        static void LoadSkins()
        {
            foreach (var prefab in prefabMap)
            {
                Skin defSkin = CreateDefaultSkin(prefab.Key, prefab.Value);
                defaultSkins.Add(prefab.Key, defSkin);

                var dir = modPath + "Skins\\" + prefab.Value;

                if (Directory.Exists(dir))
                {
                    var subDirectories = Directory.GetDirectories(dir);
                    var skinGroup = new SkinGroup(prefab.Key);
                    var carPrefab = CarTypes.GetCarPrefab(prefab.Key);
                    IEnumerable<MeshRenderer> cmps = carPrefab.gameObject.GetComponentsInChildren<MeshRenderer>();

                    var trainCar = carPrefab.GetComponent<TrainCar>();

                    if (trainCar.interiorPrefab != null)
                    {
                        var interiorCmps = trainCar.interiorPrefab.GetComponentsInChildren<MeshRenderer>();
                        cmps = cmps.Concat(interiorCmps);
                    }

                    foreach (var subDir in subDirectories)
                        BeginLoadSkin(skinGroup, cmps, subDir);

                    skinGroups.Add(prefab.Key, skinGroup);
                }
            }
        }

		// Set anisoLevel of imported textures to 5 for better visuals. (3 gives bareley an improvement, anything over 9 is pointless)
		public static void SetTextureOptions (Texture2D tex)
		{
			if (!settings.aniso5) return;
			tex.anisoLevel = 5;
		}

        static Texture2D DTXnm2RGBA(Texture2D tex)
        {
            Color[] colors = tex.GetPixels();
            for (int i = 0; i < colors.Length; i++)
            {
                Color c = colors[i];
                c.r = c.a * 2 - 1;  //red<-alpha (x<-w)
                c.g = c.g * 2 - 1; //green is always the same (y)
                Vector2 xy = new Vector2(c.r, c.g); //this is the xy vector
                c.b = Mathf.Sqrt(1 - Mathf.Clamp01(Vector2.Dot(xy, xy))); //recalculate the blue channel (z)
                colors[i] = new Color(c.r * 0.5f + 0.5f, c.g * 0.5f + 0.5f, c.b * 0.5f + 0.5f); //back to 0-1 range
            }
            tex.SetPixels(colors); //apply pixels to the texture
            tex.Apply();
            return tex;
        }

        public static Skin FindTrainCarSkin(TrainCar trainCar, string findSkinName = "")
        {
            if (!skinGroups.ContainsKey(trainCar.carType))
            {
                return null;
            }

            var skinGroup = skinGroups[trainCar.carType];

            if (skinGroup.skins.Count == 0)
            {
                return null;
            }

            Skin skin;

            if (trainCarState.ContainsKey(trainCar.logicCar.carGuid))
            {
                string skinName = trainCarState[trainCar.logicCar.carGuid];

                if (skinName == "__default")
                {
                    return null;
                }

                skin = skinGroup.GetSkin(skinName);

                if (skin == null)
                {
                    return null;
                }
            }
            else
            {
                string selectedSkin = Main.selectedSkin[trainCar.carType];

                if (findSkinName != "")
                {
                    selectedSkin = findSkinName;
                }

                if (selectedSkin == "Random" && skinGroup.skins.Count > 0)
                {
                    var range = UnityEngine.Random.Range(0, skinGroup.skins.Count);

                    // default skin if it hits out of index
                    if (range == skinGroup.skins.Count)
                    {
                        SetCarState(trainCar.logicCar.carGuid, "__default");

                        return null;
                    }

                    skin = skinGroup.skins[range];

                    SetCarState(trainCar.logicCar.carGuid, skin.name);
                }
                else if (selectedSkin == "Default")
                {
                    SetCarState(trainCar.logicCar.carGuid, "__default");

                    return null;
                }
                else
                {
                    skin = skinGroup.GetSkin(selectedSkin);

                    if (skin != null)
                    {
                        SetCarState(trainCar.logicCar.carGuid, skin.name);
                    }
                }
            }

            return skin;
        }

        public static Skin lastSteamerSkin;

        public static void ReplaceTexture(TrainCar trainCar)
        {
            string findSkin = "";

            if (trainCar.carType == TrainCarType.Tender && lastSteamerSkin != null)
            {
                findSkin = lastSteamerSkin.name;
            }

            Skin skin = FindTrainCarSkin(trainCar, findSkin);

            if(trainCar.carType == TrainCarType.LocoSteamHeavy)
            {
                lastSteamerSkin = skin;
            } else
            {
                lastSteamerSkin = null;
            }

            if (skin == null)
            {
                return;
            }

            var cmps = trainCar.gameObject.GetComponentsInChildren<MeshRenderer>();

            foreach (var cmp in cmps)
            {
                if (!cmp.material)
                {
                    continue;
                }

                var diffuse = GetMaterialTexture(cmp, "_MainTex");
                var normal = GetMaterialTexture(cmp, "_BumpMap");
                var specular = GetMaterialTexture(cmp, "_MetallicGlossMap");
                var emission = GetMaterialTexture(cmp, "_EmissionMap");
                var occlusion = GetMaterialTexture(cmp, "_OcclusionMap");

                if( !defaultSkins.TryGetValue(trainCar.carType, out Skin defSkin) )
                {
                    defSkin = null;
                }

                if (diffuse != null )
                {
                    if( skin.ContainsTexture(diffuse.name) )
                    {
                        var skinTexture = skin.GetTexture(diffuse.name);

                        cmp.material.SetTexture("_MainTex", skinTexture.TextureData);
                    }
                    else if( (defSkin != null) && defSkin.ContainsTexture(diffuse.name) )
                    {
                        var skinTexture = defSkin.GetTexture(diffuse.name);
                        cmp.material.SetTexture("_MainTex", skinTexture.TextureData);
                    }
                }

                if (normal != null)
                {
                    if( skin.ContainsTexture(normal.name) )
                    {
                        var skinTexture = skin.GetTexture(normal.name);

                        cmp.material.SetTexture("_BumpMap", skinTexture.TextureData);
                    }
                    else if( (defSkin != null) && defSkin.ContainsTexture(normal.name) )
                    {
                        var skinTexture = defSkin.GetTexture(normal.name);
                        cmp.material.SetTexture("_BumpMap", skinTexture.TextureData);
                    }
                }

                if (specular != null)
                {
                    if( skin.ContainsTexture(specular.name) )
                    {
                        var skinTexture = skin.GetTexture(specular.name);

                        cmp.material.SetTexture("_MetallicGlossMap", skinTexture.TextureData);

                        if( occlusion != null )
                        {
                            // occlusion is in green channel of specular map
                            cmp.material.SetTexture("_OcclusionMap", skinTexture.TextureData);
                        }
                    }
                    else if( (defSkin != null) && defSkin.ContainsTexture(specular.name) )
                    {
                        var skinTexture = defSkin.GetTexture(specular.name);
                        cmp.material.SetTexture("_MetallicGlossMap", skinTexture.TextureData);

                        if( occlusion != null )
                        {
                            // occlusion is in green channel of specular map
                            cmp.material.SetTexture("_OcclusionMap", skinTexture.TextureData);
                        }
                    }
                }

                if (emission != null)
                {
                    if( skin.ContainsTexture(emission.name) )
                    {
                        var skinTexture = skin.GetTexture(emission.name);

                        cmp.material.SetTexture("_EmissionMap", skinTexture.TextureData);
                    }
                    else if( (defSkin != null) && defSkin.ContainsTexture(emission.name) )
                    {
                        var skinTexture = defSkin.GetTexture(emission.name);
                        cmp.material.SetTexture("_EmissionMap", skinTexture.TextureData);
                    }
                }
            }
        }

        public static void ReplaceInteriorTexture(TrainCar trainCar)
        {
            var skin = FindTrainCarSkin(trainCar);

            if (skin == null)
            {
                return;
            }

            var cmps = trainCar.interior.GetComponentsInChildren<MeshRenderer>();

            foreach (var cmp in cmps)
            {
                if (!cmp.material)
                {
                    continue;
                }

                var diffuse = GetMaterialTexture(cmp, "_MainTex");
                var normal = GetMaterialTexture(cmp, "_BumpMap");
                var specular = GetMaterialTexture(cmp, "_MetallicGlossMap");
                var emission = GetMaterialTexture(cmp, "_EmissionMap");
                var occlusion = GetMaterialTexture(cmp, "_OcclusionMap");

                if( !defaultSkins.TryGetValue(trainCar.carType, out Skin defSkin) )
                {
                    defSkin = null;
                }

                if (diffuse != null)
                {
                    if( skin.ContainsTexture(diffuse.name) )
                    {
                        var skinTexture = skin.GetTexture(diffuse.name);

                        cmp.material.SetTexture("_MainTex", skinTexture.TextureData);
                    }
                    else if( (defSkin != null) && defSkin.ContainsTexture(diffuse.name))
                    {
                        var skinTexture = defSkin.GetTexture(diffuse.name);
                        cmp.material.SetTexture("_MainTex", skinTexture.TextureData);
                    }
                }

                if (normal != null)
                {
                    if( skin.ContainsTexture(normal.name) )
                    {
                        var skinTexture = skin.GetTexture(normal.name);

                        cmp.material.SetTexture("_BumpMap", skinTexture.TextureData);
                    }
                    else if( (defSkin != null) && defSkin.ContainsTexture(normal.name) )
                    {
                        var skinTexture = defSkin.GetTexture(normal.name);
                        cmp.material.SetTexture("_BumpMap", skinTexture.TextureData);
                    }
                }

                if (specular != null)
                {
                    if( skin.ContainsTexture(specular.name) )
                    {
                        var skinTexture = skin.GetTexture(specular.name);

                        cmp.material.SetTexture("_MetallicGlossMap", skinTexture.TextureData);

                        if( occlusion != null )
                        {
                            // occlusion is in green channel of specular map
                            cmp.material.SetTexture("_OcclusionMap", skinTexture.TextureData);
                        }
                    }
                    else if( (defSkin != null) && defSkin.ContainsTexture(specular.name) )
                    {
                        var skinTexture = defSkin.GetTexture(specular.name);
                        cmp.material.SetTexture("_MetallicGlossMap", skinTexture.TextureData);

                        if( occlusion != null )
                        {
                            // occlusion is in green channel of specular map
                            cmp.material.SetTexture("_OcclusionMap", skinTexture.TextureData);
                        }
                    }
                }

                if (emission != null)
                {
                    if( skin.ContainsTexture(emission.name) )
                    {
                        var skinTexture = skin.GetTexture(emission.name);

                        cmp.material.SetTexture("_EmissionMap", skinTexture.TextureData);
                    }
                    else if( (defSkin != null) && defSkin.ContainsTexture(emission.name) )
                    {
                        var skinTexture = defSkin.GetTexture(emission.name);
                        cmp.material.SetTexture("_EmissionMap", skinTexture.TextureData);
                    }
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

    [HarmonyPatch(typeof(CarSpawner), "SpawnCar")]
    class CarSpawner_SpawnCar_Patch
    {
        static void Postfix(TrainCar __result)
        {
            Main.ReplaceTexture(__result);
        }
    }

    [HarmonyPatch(typeof(CarSpawner), "SpawnLoadedCar")]
    class CarSpawner_SpawnExistingCar_Patch
    {
        static void Postfix(TrainCar __result)
        {
            Main.ReplaceTexture(__result);
        }
    }

    [HarmonyPatch(typeof(TrainCar), "LoadInterior")]
    class TrainCar_LoadInterior_Patch
    {
        static void Postfix(TrainCar __instance)
        {
            Main.ReplaceInteriorTexture(__instance);
        }
    }


	// Mod settings
	public class Settings : UnityModManager.ModSettings
	{
		public bool aniso5 = false;
        public bool parallelLoading = true;

		public override void Save (UnityModManager.ModEntry modEntry)
		{
			Save(this, modEntry);
		}

		public void OnChange()
		{

		}
	}
}