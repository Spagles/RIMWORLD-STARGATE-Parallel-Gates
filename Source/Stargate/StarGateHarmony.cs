using HarmonyLib;
using Verse;

namespace RimGateJaffaKree
{
    [StaticConstructorOnStartup]
    public static class StarGateHarmony
    {
        static StarGateHarmony()
        {
            Harmony harmony = new Harmony("panzmoravylab.rimworldstargate.parallelgates");
            harmony.PatchAll();
        }
    }
}
