using HarmonyLib;
using Newtonsoft.Json.Linq;

namespace SkinManagerMod
{
    [HarmonyPatch(typeof(SaveGameManager))]
    internal static class SaveGameManager_Save_Patch
    {
        [HarmonyPatch(nameof(SaveGameManager.Save))]
        [HarmonyPrefix]
        static void SavePrefix()
        {
            JObject carsSaveData = SkinManager.GetCarsSaveData();

            SaveGameManager.Instance.data.SetJObject("Mod_Skins", carsSaveData);
        }
    }

    [HarmonyPatch(typeof(CarsSaveManager))]
    internal static class CarsSaveManager_Load_Patch
    {
        [HarmonyPatch(nameof(CarsSaveManager.Load))]
        [HarmonyPrefix]
        static void LoadPrefix(JObject savedData)
        {
            if (savedData == null)
            {
                Main.Error("Given save data is null, loading will not be performed");
                return;
            }

            JObject carsSaveData = SaveGameManager.Instance.data.GetJObject("Mod_Skins");

            if (carsSaveData != null)
            {
                SkinManager.LoadCarsSaveData(carsSaveData);
            }
        }
    }

    public class CarsSkinSaveData
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
