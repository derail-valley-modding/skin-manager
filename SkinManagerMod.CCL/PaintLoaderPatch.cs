using System.Collections.Generic;
using CCL.Importer;
using CCL.Types;
using DV.Customization.Paint;
using HarmonyLib;

namespace SkinManagerMod.CCL
{
    [HarmonyPatch(typeof(PaintLoader))]
    public static class PaintLoaderPatch
    {
        [HarmonyPatch(nameof(PaintLoader.LoadSubstitutions))]
        [HarmonyPostfix]
        internal static void AfterLoadSubstitutions(IEnumerable<PaintSubstitutions> substitutions)
        {
            foreach (var sub in substitutions)
            {
                var theme = SkinProvider.GetOrCreateTheme(sub.Paint);
                foreach (var s in sub.Substitutions)
                {
                    theme.CCLSubstitutions.Add(new PaintTheme.Substitution()
                    {
                        original = s.Original,
                        substitute = s.Substitute,
                    });
                }
            }
        }
    }
}
