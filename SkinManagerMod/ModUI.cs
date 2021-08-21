using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;

namespace SkinManagerMod
{
    //[HarmonyPatch(typeof(UnityModManager.UI), "OnGUI")]
    class UnityModManagerUI_OnGUI_Patch
    {
        private static void Postfix( UnityModManager.UI __instance )
        {
            ModUI.DrawModUI();
        }
    }

    //[HarmonyPatch(typeof(CarSpawner), "SpawnModeEnable")]
    class CarSpawner_SpawnModeEnable_Patch
    {
        static void Prefix( bool turnOn )
        {
            ModUI.isEnabled = turnOn;
        }
    }

    class ModUI
    {
        public static bool isEnabled;

        static bool showDropdown;

        static Vector2 scrollViewVector;

       
        //static Dictionary<TrainCarType, string> prefabMap;
        public static void Init()
        {

        }

        public static void DrawModUI()
        {
            /*
            if (!isEnabled)
            {
                showDropdown = false;
                return;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            GameObject carToSpawn = SingletonBehaviour<CarSpawner>.Instance.carToSpawn;
            TrainCar trainCar = carToSpawn.GetComponent<TrainCar>();
            TrainCarType carType = trainCar.carType;

            SkinGroup skinGroup = Main.skinGroups[carType];
            string selectSkin = Main.selectedSkin[carType];

            float menuHeight = 60f;

            float menuWidth = 270f;
            float buttonWidth = 240f;
            bool isMaxHeight = false;
            float maxHeight = Screen.height - 200f;

            if (showDropdown)
            {
                float totalItemHeight = skinGroup.skins.Count * 23f;

                if (totalItemHeight > maxHeight)
                {
                    totalItemHeight = maxHeight;
                    isMaxHeight = true;
                }

                menuHeight += totalItemHeight + 46f;
            }

            if (isMaxHeight)
            {
                buttonWidth -= 20f;
            }

            GUI.skin = DVGUI.skin;
            GUI.skin.label.fontSize = 11;
            GUI.skin.button.fontSize = 10;
            GUI.color = new Color32(0, 0, 0, 200);
            GUI.Box(new Rect(20f, 20f, menuWidth, menuHeight), "");
            GUILayout.BeginArea(new Rect(30f, 20f, menuWidth, menuHeight));
            GUI.color = Color.yellow;
            GUILayout.Label("Skin Manager Menu :: " + carToSpawn.name);
            GUI.color = Color.white;
            GUILayout.Space(5f);

            if (showDropdown)
            {
                if (GUILayout.Button("=== " + selectSkin + " ===", GUILayout.Width(240f)))
                {
                    showDropdown = false;
                }

                if (isMaxHeight)
                {
                    scrollViewVector = GUILayout.BeginScrollView(scrollViewVector, GUILayout.Width(245f), GUILayout.Height(menuHeight - 55f));
                }

                if (GUILayout.Button("Random", GUILayout.Width(buttonWidth)))
                {
                    showDropdown = false;
                    Main.selectedSkin[carType] = "Random";
                }

                if (GUILayout.Button("Default", GUILayout.Width(buttonWidth)))
                {
                    showDropdown = false;
                    Main.selectedSkin[carType] = "Default";
                }

                foreach (Skin skin in skinGroup.skins)
                {
                    if (GUILayout.Button(skin.name, GUILayout.Width(buttonWidth)))
                    {
                        showDropdown = false;
                        Main.selectedSkin[carType] = skin.name;
                    }
                }

                if (isMaxHeight)
                {
                    GUILayout.EndScrollView();
                }
            }
			else
            {
                if (GUILayout.Button("=== " + selectSkin + " ===", GUILayout.Width(240f)))
                {
                    showDropdown = true;
                }
            }

            GUILayout.EndArea();
            */
        }
    }
}