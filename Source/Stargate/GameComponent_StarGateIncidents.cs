using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace RimGateJaffaKree
{
    public class GameComponent_StarGateIncidents : GameComponent
    {
        private const int MinIncidentIntervalTicks = 60000;
        private const int MaxIncidentIntervalTicks = 120000;
        public const float StorytellerRedirectChance = 0.40f;

        private int nextIncidentTick = -1;

        public GameComponent_StarGateIncidents(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextIncidentTick, "nextIncidentTick", -1);
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            ScheduleNextIncident();
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (nextIncidentTick < 0)
            {
                ScheduleNextIncident();
            }

            if (Find.TickManager.TicksGame >= nextIncidentTick)
            {
                TryTriggerIncomingEvent();
                ScheduleNextIncident();
            }
        }

        public bool DebugTriggerOnMap(Map map)
        {
            if (map == null)
            {
                return false;
            }

            return TryTriggerIncomingEvent(map, true);
        }

        public bool DebugTriggerOnMap(Map map, string mode, float points)
        {
            if (map == null)
            {
                return false;
            }

            IncomingEventKind kind = ParseKind(mode);
            return TryTriggerIncomingEvent(map, true, kind, points);
        }

        public bool TryTriggerFromIncident(IncidentParms parms, string mode)
        {
            Map map = parms?.target as Map;
            if (map == null)
            {
                return false;
            }

            IncomingEventKind kind = ParseKind(mode);
            return TryTriggerIncomingEvent(map, true, kind, parms.points, parms.faction);
        }

        private void ScheduleNextIncident()
        {
            nextIncidentTick = Find.TickManager.TicksGame + Rand.RangeInclusive(MinIncidentIntervalTicks, MaxIncidentIntervalTicks);
        }

        private bool TryTriggerIncomingEvent()
        {
            Map map = Current.Game?.Maps?.FirstOrDefault(candidate => candidate != null && candidate.IsPlayerHome);
            return TryTriggerIncomingEvent(map, false);
        }

        private bool TryTriggerIncomingEvent(Map map, bool force)
        {
            return TryTriggerIncomingEvent(map, force, null, -1f);
        }

        private bool TryTriggerIncomingEvent(Map map, bool force, IncomingEventKind? forcedKind, float forcedPoints)
        {
            return TryTriggerIncomingEvent(map, force, forcedKind, forcedPoints, null);
        }

        private bool TryTriggerIncomingEvent(Map map, bool force, IncomingEventKind? forcedKind, float forcedPoints, Faction forcedFaction)
        {
            if (map == null)
            {
                return false;
            }

            CompStarGate gate = FindGate(map);
            if (gate == null || gate.IsOnline || gate.IsWarmingUp)
            {
                return false;
            }

            if (!force && map.mapPawns != null && map.mapPawns.FreeColonistsCount == 0)
            {
                return false;
            }

            IncomingEventKind eventKind = forcedKind ?? ChooseEventKind();
            Faction faction = forcedFaction ?? ChooseFactionFor(eventKind);
            if (faction == null)
            {
                return false;
            }

            List<Pawn> pawns = GeneratePawnsFor(map, faction, eventKind, forcedPoints);
            if (pawns == null || pawns.Count == 0)
            {
                return false;
            }

            gate.BringOnline(900, true);
            IntVec3 arrivalCell = ArrivalCellFor(gate);
            SpawnPawnsNearGate(map, pawns, arrivalCell);
            AssignLord(map, faction, pawns, arrivalCell, eventKind);
            SendArrivalMessage(gate, faction, eventKind, pawns.Count);
            return true;
        }

        private static CompStarGate FindGate(Map map)
        {
            ThingDef gateDef = DefDatabase<ThingDef>.GetNamedSilentFail("StarGate");
            if (gateDef == null)
            {
                return null;
            }

            return map.listerThings.ThingsOfDef(gateDef)
                .Where(thing => thing != null && thing.Spawned)
                .Select(thing => thing.TryGetComp<CompStarGate>())
                .FirstOrDefault(comp => comp != null);
        }

        private static IncomingEventKind ChooseEventKind()
        {
            float roll = Rand.Value;
            if (roll < 0.55f)
            {
                return IncomingEventKind.HostileRaid;
            }

            if (roll < 0.8f)
            {
                return IncomingEventKind.TraderVisit;
            }

            return IncomingEventKind.AlliedVisit;
        }

        private static Faction ChooseFactionFor(IncomingEventKind eventKind)
        {
            List<Faction> factions = Find.FactionManager?.AllFactionsListForReading
                ?.Where(faction => faction != null && !faction.defeated && !faction.IsPlayer && !faction.Hidden)
                .ToList();

            if (factions == null || factions.Count == 0)
            {
                return null;
            }

            if (eventKind == IncomingEventKind.HostileRaid)
            {
                List<Faction> jaffaFactions = factions
                    .Where(faction => faction.def != null && (faction.def.defName == "JaffaApophis" || faction.def.defName == "JaffaAnubis" || faction.def.defName == "JaffaRa"))
                    .ToList();
                if (jaffaFactions.Count > 0)
                {
                    return jaffaFactions.RandomElement();
                }

                List<Faction> hostile = factions.Where(faction => faction.HostileTo(Faction.OfPlayer)).ToList();
                if (hostile.Count > 0)
                {
                    return hostile.RandomElement();
                }
            }
            else if (eventKind == IncomingEventKind.AlliedVisit)
            {
                List<Faction> allies = factions.Where(faction => !faction.HostileTo(Faction.OfPlayer) && faction.PlayerRelationKind == FactionRelationKind.Ally).ToList();
                if (allies.Count > 0)
                {
                    return allies.RandomElement();
                }
            }
            else
            {
                List<Faction> neutral = factions.Where(faction => !faction.HostileTo(Faction.OfPlayer)).ToList();
                if (neutral.Count > 0)
                {
                    return neutral.RandomElement();
                }
            }

            return factions.RandomElement();
        }

        private static List<Pawn> GeneratePawnsFor(Map map, Faction faction, IncomingEventKind eventKind, float forcedPoints)
        {
            float points = forcedPoints > 0f
                ? forcedPoints
                : Mathf.Max(220f, StorytellerUtility.DefaultThreatPointsNow(map) * (eventKind == IncomingEventKind.HostileRaid ? 0.8f : 0.45f));

            PawnGroupMakerParms parms = new PawnGroupMakerParms
            {
                faction = faction,
                tile = map.Tile,
                points = points,
                inhabitants = false,
                groupKind = eventKind == IncomingEventKind.HostileRaid ? PawnGroupKindDefOf.Combat : PawnGroupKindDefOf.Combat
            };

            List<Pawn> pawns = PawnGroupMakerUtility.GeneratePawns(parms, true).ToList();
            return pawns;
        }

        private static IntVec3 ArrivalCellFor(CompStarGate gate)
        {
            IntVec3 preferred = gate.parent.Position + new IntVec3(0, 0, -2);
            if (preferred.InBounds(gate.parent.Map) && preferred.Standable(gate.parent.Map))
            {
                return preferred;
            }

            return CellFinder.StandableCellNear(gate.parent.Position, gate.parent.Map, 4f, null);
        }

        private static void SpawnPawnsNearGate(Map map, List<Pawn> pawns, IntVec3 arrivalCell)
        {
            foreach (Pawn pawn in pawns)
            {
                IntVec3 cell = CellFinder.StandableCellNear(arrivalCell, map, 4f, candidate => candidate.Standable(map) && !candidate.Fogged(map));
                if (!cell.IsValid)
                {
                    cell = arrivalCell;
                }

                GenSpawn.Spawn(pawn, cell, map, Rot4.South);
                if (pawn.RaceProps.Humanlike)
                {
                    pawn.mindState?.Reset(false, false);
                }
            }
        }

        private static void AssignLord(Map map, Faction faction, List<Pawn> pawns, IntVec3 arrivalCell, IncomingEventKind eventKind)
        {
            LordJob lordJob;
            if (eventKind == IncomingEventKind.HostileRaid)
            {
                lordJob = new LordJob_AssaultColony(faction, true, true, false, true, false, false, false);
            }
            else if (eventKind == IncomingEventKind.TraderVisit)
            {
                lordJob = new LordJob_TradeWithColony(faction, arrivalCell);
            }
            else
            {
                lordJob = new LordJob_VisitColony(faction, arrivalCell, null);
            }

            LordMaker.MakeNewLord(faction, lordJob, map, pawns);
        }

        private static void SendArrivalMessage(CompStarGate gate, Faction faction, IncomingEventKind eventKind, int pawnCount)
        {
            string title;
            string text;
            LetterDef letterDef;

            if (eventKind == IncomingEventKind.HostileRaid)
            {
                title = StarGateText.Get("StarGate_RaidLetterTitle");
                text = StarGateText.Format("StarGate_RaidLetterText", faction.Name, pawnCount);
                letterDef = LetterDefOf.ThreatBig;
            }
            else if (eventKind == IncomingEventKind.TraderVisit)
            {
                title = StarGateText.Get("StarGate_VisitLetterTitle");
                text = StarGateText.Format("StarGate_VisitLetterText", faction.Name);
                letterDef = LetterDefOf.PositiveEvent;
            }
            else
            {
                title = StarGateText.Get("StarGate_AllyLetterTitle");
                text = StarGateText.Format("StarGate_AllyLetterText", faction.Name);
                letterDef = LetterDefOf.PositiveEvent;
            }

            Find.LetterStack.ReceiveLetter(title, text, letterDef, gate.parent);
        }

        private static IncomingEventKind ParseKind(string mode)
        {
            switch ((mode ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "raid":
                    return IncomingEventKind.HostileRaid;
                case "trader":
                case "trade":
                    return IncomingEventKind.TraderVisit;
                case "ally":
                case "allies":
                    return IncomingEventKind.AlliedVisit;
                default:
                    return ChooseEventKind();
            }
        }

        private enum IncomingEventKind
        {
            HostileRaid,
            TraderVisit,
            AlliedVisit
        }
    }
}
