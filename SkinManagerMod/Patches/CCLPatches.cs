using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DV.Customization.Paint;
using DV.ThingTypes;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;

namespace SkinManagerMod.Patches
{
    public static class CCLPatcher
    {
        public static bool Enabled { get; private set; }

        public static void SetupCCLListener()
        {
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        }

        private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs e)
        {
            var assemblyName = new AssemblyName(e.LoadedAssembly.FullName);
            if (assemblyName.Name == "CCL.Importer")
            {
                TryPerformPatching();
            }
        }

        public static void TryPerformPatching()
        {
            var ccl = UnityModManager.FindMod("DVCustomCarLoader");
            Assembly.LoadFrom(Path.Combine(ccl.Path, "CCL.Types.dll"));

            var origMethod = AccessTools.Method("CCL.Importer.PaintLoader:LoadSubstitutions");
            var patchMethod = AccessTools.Method(typeof(CCLPatches), nameof(CCLPatches.AfterLoadSubstitutions));
            Main.Harmony.Patch(origMethod, postfix: patchMethod);

            Main.Log("Patched CCL Methods");
            Enabled = true;
        }
    }

    internal static class CCLPatches
    {
        internal static void AfterLoadSubstitutions(dynamic substitutions)
        {
            foreach (dynamic sub in substitutions)
            {
                var theme = SkinProvider.GetOrCreateTheme(sub.Paint);
                foreach (dynamic s in sub.Substitutions)
                {
                    theme.CCLSubstitutions.Add(new PaintTheme.Substitution()
                    {
                        original = s.Original,
                        substitute = s.Substitute,
                    });
                }
            }
        }

        internal static void CheckCCLSubstitutionSupport(TrainCarLivery cclLivery)
        {
            if (!CCL.Importer.CarTypeInjector.IdToLiveryMap.ContainsKey(cclLivery.id)) return;

            // build ccl material matching
            Main.LogVerbose($"Check CCL livery {cclLivery.id}");

            IEnumerable<TrainCarPaint.MaterialSet> sets = cclLivery.prefab.GetComponentsInChildren<TrainCarPaint>(true).SelectMany(tcp => tcp.sets);

            foreach (var customTheme in SkinProvider.AllThemes)
            {
                foreach (var materialSet in sets)
                {
                    if (!materialSet.OriginalMaterial) continue;

                    if (customTheme.TryGetCCLSubstitution(materialSet.OriginalMaterial, out var substitute))
                    {
                        customTheme.SupportedCCLIds.Add(cclLivery.id);
                    }
                }
            }
        }

        private static void AddSubstitutionToSkin(Skin skin, Material original, Material substitute)
        {
            foreach (string texProperty in TextureUtility.PropNames.UniqueTextures)
            {
                if (!(original.HasProperty(texProperty) && substitute.HasProperty(texProperty))) continue;

                var origTex = original.GetTexture(texProperty) as Texture2D;
                var subTex = substitute.GetTexture(texProperty) as Texture2D;

                if (!origTex || !subTex) continue;

                skin.SkinTextures.Add(new SkinTexture(origTex!.name, subTex!));
                Main.LogVerbose($"Add texture {origTex.name} ({subTex!.name}) to skin {skin.Name}");
            }
        }
    }
}