using RimWorld;
using LudeonTK;
using Verse;

namespace RimGateJaffaKree
{
    public static class StarGateDebugActions
    {
        [DebugAction("StarGate - Parallel Gates", "Random arrival", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void RandomArrival()
        {
            Trigger("random", -1f);
        }

        [DebugAction("StarGate - Parallel Gates", "Execute raid with points (weak)", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void RaidWeak()
        {
            Trigger("raid", 300f);
        }

        [DebugAction("StarGate - Parallel Gates", "Execute raid with points (medium)", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void RaidMedium()
        {
            Trigger("raid", 700f);
        }

        [DebugAction("StarGate - Parallel Gates", "Execute raid with points (strong)", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void RaidStrong()
        {
            Trigger("raid", 1400f);
        }

        [DebugAction("StarGate - Parallel Gates", "Do trade caravan arrival", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void TraderArrival()
        {
            Trigger("trader", 350f);
        }

        [DebugAction("StarGate - Parallel Gates", "Do ally / visitor arrival", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void AllyArrival()
        {
            Trigger("ally", 350f);
        }

        private static void Trigger(string mode, float points)
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                Messages.Message(StarGateText.Get("StarGate_DebugMapRequired"), MessageTypeDefOf.RejectInput, false);
                return;
            }

            GameComponent_StarGateIncidents component = Current.Game?.GetComponent<GameComponent_StarGateIncidents>();
            bool ok = mode == "random"
                ? (component?.DebugTriggerOnMap(map) ?? false)
                : (component?.DebugTriggerOnMap(map, mode, points) ?? false);

            if (!ok)
            {
                Messages.Message(StarGateText.Get("StarGate_DebugIncidentFailed"), MessageTypeDefOf.RejectInput, false);
            }
        }
    }
}
