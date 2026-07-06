using HarmonyLib;
using RimWorld;
using Verse;

namespace RimGateJaffaKree
{
    public static class StarGateIncidentRedirectUtility
    {
        public static bool TryRedirect(IncidentParms parms, string mode, ref bool result)
        {
            Map map = parms?.target as Map;
            if (map == null)
            {
                return false;
            }

            if (!Rand.Chance(GameComponent_StarGateIncidents.StorytellerRedirectChance))
            {
                return false;
            }

            GameComponent_StarGateIncidents component = Current.Game?.GetComponent<GameComponent_StarGateIncidents>();
            if (component == null)
            {
                return false;
            }

            if (!component.TryTriggerFromIncident(parms, mode))
            {
                return false;
            }

            result = true;
            return true;
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker")]
    public static class StarGatePatch_RaidEnemy
    {
        public static bool Prefix(IncidentParms parms, ref bool __result)
        {
            if (StarGateIncidentRedirectUtility.TryRedirect(parms, "raid", ref __result))
            {
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_TraderCaravanArrival), "TryExecuteWorker")]
    public static class StarGatePatch_TraderCaravan
    {
        public static bool Prefix(IncidentParms parms, ref bool __result)
        {
            if (StarGateIncidentRedirectUtility.TryRedirect(parms, "trader", ref __result))
            {
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_VisitorGroup), "TryExecuteWorker")]
    public static class StarGatePatch_VisitorGroup
    {
        public static bool Prefix(IncidentParms parms, ref bool __result)
        {
            if (StarGateIncidentRedirectUtility.TryRedirect(parms, "ally", ref __result))
            {
                return false;
            }

            return true;
        }
    }
}
