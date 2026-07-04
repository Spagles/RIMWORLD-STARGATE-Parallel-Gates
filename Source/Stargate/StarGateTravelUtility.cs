using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RimGateJaffaKree
{
    public static class StarGateTravelUtility
    {
        private static readonly IntVec3 DestinationMapSize = new IntVec3(150, 1, 150);

        public static void TravelThrough(CompStarGate origin, Pawn selectedPawn)
        {
            if (origin == null || selectedPawn == null || !selectedPawn.Spawned)
            {
                return;
            }

            List<Pawn> pawns = SelectedTravelPawns(selectedPawn, origin.parent.Map);
            LongEventHandler.QueueLongEvent(delegate
            {
                PrimeOffworldPlanet();
                Map destinationMap = DestinationMapFor(origin.parent.Map);
                if (destinationMap == null)
                {
                    Messages.Message("StarGate nedokazala navazat spojeni.", origin.parent, MessageTypeDefOf.RejectInput, false);
                    return;
                }

                CompStarGate destinationGate = EnsureGateOnMap(destinationMap);
                if (destinationGate == null)
                {
                    Messages.Message("Na cilove mape se nepodarilo vytvorit StarGate.", origin.parent, MessageTypeDefOf.RejectInput, false);
                    return;
                }

                origin.BringOnline(600);
                destinationGate.BringOnline(600);
                MovePawns(pawns, destinationGate);
            }, "StarGate_TravelingWormhole", false, null);
        }

        private static void PrimeOffworldPlanet()
        {
            StarGatePlanetSystem planetSystem = Current.Game.GetComponent<StarGatePlanetSystem>();
            if (planetSystem == null)
            {
                return;
            }

            StarGatePlanetRecord record = planetSystem.EnsurePrimaryOffworldPlanet();
            planetSystem.EnsureTransientWorld(record);
        }

        public static CompStarGate EnsureGateOnMap(Map map)
        {
            ThingDef gateDef = ThingDef.Named("StarGate");
            CompStarGate existingGate = map.listerThings.ThingsOfDef(gateDef)
                .Where(thing => thing.Spawned)
                .Select(thing => thing.TryGetComp<CompStarGate>())
                .FirstOrDefault(comp => comp != null);

            if (existingGate != null)
            {
                return existingGate;
            }

            if (!TryFindGateCell(map, gateDef, out IntVec3 cell))
            {
                return null;
            }

            Thing gate = GenSpawn.Spawn(gateDef, cell, map);
            return gate.TryGetComp<CompStarGate>();
        }

        private static List<Pawn> SelectedTravelPawns(Pawn selectedPawn, Map map)
        {
            List<Pawn> pawns = Find.Selector.SelectedObjects
                .OfType<Pawn>()
                .Where(pawn => pawn.Spawned && pawn.Map == map && pawn.Faction == Faction.OfPlayer)
                .Distinct()
                .ToList();

            if (!pawns.Contains(selectedPawn))
            {
                pawns.Add(selectedPawn);
            }

            return pawns;
        }

        private static Map DestinationMapFor(Map originMap)
        {
            if (originMap != null && !originMap.IsPlayerHome)
            {
                Map homeMap = Current.Game.Maps.FirstOrDefault(map => map != null && map.IsPlayerHome);
                if (homeMap != null)
                {
                    return homeMap;
                }
            }

            MapParent parent = EnsurePlanetSite();
            if (parent == null)
            {
                return null;
            }

            if (parent.HasMap)
            {
                return parent.Map;
            }

            return GetOrGenerateMapUtility.GetOrGenerateMap(parent.Tile, DestinationMapSize, parent.def);
        }

        private static MapParent EnsurePlanetSite()
        {
            WorldObjectDef def = DefDatabase<WorldObjectDef>.GetNamedSilentFail("StarGateWorldSite");
            if (def == null)
            {
                return null;
            }

            MapParent existing = Find.WorldObjects.AllWorldObjects
                .OfType<MapParent>()
                .FirstOrDefault(obj => obj.def == def);

            if (existing != null)
            {
                return existing;
            }

            Settlement playerSettlement = Find.WorldObjects.Settlements.FirstOrDefault(settlement => settlement.Faction == Faction.OfPlayer);
            if (playerSettlement == null || !TryFindTile(playerSettlement.Tile, out int tile))
            {
                return null;
            }

            MapParent site = (MapParent)WorldObjectMaker.MakeWorldObject(def);
            site.Tile = tile;
            Find.WorldObjects.Add(site);
            return site;
        }

        private static bool TryFindTile(int playerTile, out int tile)
        {
            for (int i = 0; i < 2000; i++)
            {
                int candidate = Rand.Range(0, Find.WorldGrid.TilesCount);
                if (Find.WorldGrid.ApproxDistanceInTiles(playerTile, candidate) < 35)
                {
                    continue;
                }

                Tile worldTile = Find.WorldGrid[candidate];
                if (worldTile == null || worldTile.PrimaryBiome == null || worldTile.PrimaryBiome.impassable)
                {
                    continue;
                }

                if (worldTile.hilliness == Hilliness.Impassable || Find.WorldObjects.ObjectsAt(candidate).Any())
                {
                    continue;
                }

                tile = candidate;
                return true;
            }

            tile = -1;
            return false;
        }

        private static bool TryFindGateCell(Map map, ThingDef gateDef, out IntVec3 cell)
        {
            for (int i = 0; i < 2000; i++)
            {
                IntVec3 candidate = CellFinder.RandomCell(map);
                if (!candidate.Standable(map) || candidate.Fogged(map))
                {
                    continue;
                }

                if (!GenAdj.OccupiedRect(candidate, Rot4.North, gateDef.size).InBounds(map))
                {
                    continue;
                }

                if (!GenConstruct.CanPlaceBlueprintAt(gateDef, candidate, Rot4.North, map).Accepted)
                {
                    continue;
                }

                cell = candidate;
                return true;
            }

            cell = IntVec3.Invalid;
            return false;
        }

        private static void MovePawns(List<Pawn> pawns, CompStarGate destinationGate)
        {
            foreach (Pawn pawn in pawns.Where(pawn => pawn != null && pawn.Spawned).ToList())
            {
                IntVec3 targetCell = CellFinder.StandableCellNear(destinationGate.parent.Position, destinationGate.parent.Map, 6f);
                if (!targetCell.IsValid)
                {
                    continue;
                }

                pawn.DeSpawn(DestroyMode.Vanish);
                GenSpawn.Spawn(pawn, targetCell, destinationGate.parent.Map);
                pawn.Notify_Teleported(false, true);
            }

            CameraJumper.TryJump(destinationGate.parent);
        }
    }
}
