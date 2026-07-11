using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace RimGateJaffaKree
{
    public static class StarGateTravelUtility
    {
        private static readonly IntVec3 DestinationMapSize = new IntVec3(150, 1, 150);

        public static void TravelThrough(CompStarGate origin, Pawn selectedPawn)
        {
            if (origin == null || selectedPawn == null || !selectedPawn.Spawned)
            {
                Messages.Message(StarGateText.Get("StarGate_TravelUnavailable"), MessageTypeDefOf.RejectInput, false);
                return;
            }

            List<Pawn> pawns = SelectedTravelPawns(selectedPawn, origin.parent.Map);
            LongEventHandler.QueueLongEvent(delegate
            {
                try
                {
                    Map destinationMap = DestinationMapFor(origin.parent.Map, origin.DialedAddress, origin.DialedSiteId);
                    if (destinationMap == null)
                    {
                        Messages.Message(StarGateText.Get("StarGate_ConnectionFailed"), origin.parent, MessageTypeDefOf.RejectInput, false);
                        return;
                    }

                    if (destinationMap == origin.parent.Map)
                    {
                        Messages.Message(StarGateText.Get("StarGate_SameMap"), origin.parent, MessageTypeDefOf.RejectInput, false);
                        Log.Warning("StarGate rejected travel because destination map matched source map: " + destinationMap.uniqueID);
                        return;
                    }

                    CompStarGate destinationGate = EnsureGateOnMap(destinationMap, DestinationGateAddress(destinationMap, origin.DialedAddress));
                    if (destinationGate == null)
                    {
                        Messages.Message(StarGateText.Get("StarGate_DestinationGateFailed"), origin.parent, MessageTypeDefOf.RejectInput, false);
                        return;
                    }

                    StarGatePlanetContentUtility.EnsureContent(destinationMap, destinationGate);
                    origin.BringOnline(600);
                    destinationGate.BringOnline(600, true);
                    int movedCount = MovePawns(pawns, destinationGate);
                    if (movedCount == 0)
                    {
                        Messages.Message(StarGateText.Get("StarGate_NoArrivalSpace"), destinationGate.parent, MessageTypeDefOf.RejectInput, false);
                    }
                    else
                    {
                        Current.Game.GetComponent<StarGatePlanetSystem>()?.RegisterVisit(destinationGate.parent.Map);
                        Messages.Message(StarGateText.Get("StarGate_TravelComplete"), destinationGate.parent, MessageTypeDefOf.PositiveEvent, false);
                    }
                }
                catch (System.Exception exception)
                {
                    Log.Error("StarGate travel failed: " + exception);
                    Messages.Message(StarGateText.Get("StarGate_TravelFailed"), origin.parent, MessageTypeDefOf.RejectInput, false);
                }
            }, "StarGate_TravelingWormhole", false, null);
        }

        public static void EnterGate(CompStarGate gate, Pawn pawn)
        {
            if (gate == null || pawn == null || !pawn.Spawned)
            {
                return;
            }

            if (gate.IsIncomingOnly)
            {
                Messages.Message(StarGateText.Format("StarGate_IncomingFatal", pawn.LabelShort), pawn, MessageTypeDefOf.NegativeEvent, false);
                pawn.Kill(null);
                return;
            }

            TravelThrough(gate, pawn);
        }

        public static IntVec3 EntryCellFor(Thing gate, Pawn pawn = null)
        {
            if (gate?.Map == null)
            {
                return IntVec3.Invalid;
            }

            CellRect occupied = GenAdj.OccupiedRect(gate.Position, gate.Rotation, gate.def.size);
            foreach (IntVec3 cell in occupied.Cells.OrderBy(cell => cell.DistanceTo(gate.Position)))
            {
                if (!cell.InBounds(gate.Map) || cell.Fogged(gate.Map))
                {
                    continue;
                }

                if (!cell.Walkable(gate.Map))
                {
                    continue;
                }

                if (pawn != null && !pawn.CanReach(cell, PathEndMode.OnCell, Danger.Some))
                {
                    continue;
                }

                return cell;
            }

            IntVec3 fallback = CellFinder.StandableCellNear(gate.Position, gate.Map, 3f);
            return fallback.IsValid ? fallback : gate.Position;
        }

        public static CompStarGate EnsureGateOnMap(Map map, string gateAddress = null)
        {
            ThingDef gateDef = ThingDef.Named("StarGate");
            CompStarGate existingGate = map.listerThings.ThingsOfDef(gateDef)
                .Where(thing => thing.Spawned)
                .Select(thing => thing.TryGetComp<CompStarGate>())
                .FirstOrDefault(comp => comp != null);

            if (existingGate != null)
            {
                if (!gateAddress.NullOrEmpty())
                {
                    existingGate.SetDialedAddress(gateAddress);
                }

                EnsureControlPanelForGate(existingGate);

                return existingGate;
            }

            if (!TryFindGateCell(map, gateDef, out IntVec3 cell))
            {
                return null;
            }

            Thing gate = GenSpawn.Spawn(gateDef, cell, map);
            CompStarGate gateComp = gate.TryGetComp<CompStarGate>();
            if (gateComp != null && !gateAddress.NullOrEmpty())
            {
                gateComp.SetDialedAddress(gateAddress);
            }

            EnsureControlPanelForGate(gateComp);

            return gateComp;
        }

        private static void EnsureControlPanelForGate(CompStarGate gateComp)
        {
            if (gateComp?.parent?.Map == null)
            {
                return;
            }

            Map map = gateComp.parent.Map;
            ThingDef panelDef = DefDatabase<ThingDef>.GetNamedSilentFail("StarGate_Control_Panel");
            if (panelDef == null)
            {
                return;
            }

            bool existingPanel = map.listerThings.ThingsOfDef(panelDef)
                .Any(thing => thing != null && thing.Spawned && thing.Position.DistanceTo(gateComp.parent.Position) <= 8f);
            if (existingPanel)
            {
                return;
            }

            IntVec3[] candidates =
            {
                gateComp.parent.Position + new IntVec3(-2, 0, -2),
                gateComp.parent.Position + new IntVec3(2, 0, -2),
                gateComp.parent.Position + new IntVec3(-3, 0, -1),
                gateComp.parent.Position + new IntVec3(3, 0, -1)
            };

            foreach (IntVec3 cell in candidates)
            {
                if (!cell.InBounds(map) || !cell.Standable(map) || cell.GetEdifice(map) != null)
                {
                    continue;
                }

                GenSpawn.Spawn(panelDef, cell, map);
                return;
            }
        }

        private static string DestinationGateAddress(Map destinationMap, string dialedAddress)
        {
            StarGatePlanetSystem planetSystem = Current.Game.GetComponent<StarGatePlanetSystem>();
            if (planetSystem == null)
            {
                return dialedAddress;
            }

            if (destinationMap != null && destinationMap.IsPlayerHome)
            {
                return null;
            }

            return planetSystem.HomeAddress;
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

        private static Map DestinationMapFor(Map originMap, string dialedAddress, string dialedSiteId)
        {
            StarGatePlanetSystem planetSystem = Current.Game.GetComponent<StarGatePlanetSystem>();
            planetSystem?.EnsureInitialized();

            if (originMap != null && IsOffworldMap(originMap, planetSystem) && (dialedAddress.NullOrEmpty() || planetSystem == null || planetSystem.IsHomeAddress(dialedAddress)))
            {
                Map homeMap = planetSystem?.HomeMap() ?? Current.Game.Maps.FirstOrDefault(map => map != null && map.IsPlayerHome);
                if (homeMap != null)
                {
                    Messages.Message(StarGateText.Get("StarGate_HomeConnection"), MessageTypeDefOf.PositiveEvent, false);
                    return homeMap;
                }

                Messages.Message(StarGateText.Get("StarGate_HomeMapMissing"), MessageTypeDefOf.RejectInput, false);
                return null;
            }

            StarGatePlanetRecord destination = DestinationRecordForAddress(planetSystem, dialedAddress);
            return EnsureOffworldMap(destination, dialedSiteId, originMap);
        }

        private static bool IsOffworldMap(Map map, StarGatePlanetSystem planetSystem)
        {
            return planetSystem?.PlanetForMap(map) != null;
        }

        private static StarGatePlanetRecord DestinationRecordForAddress(StarGatePlanetSystem planetSystem, string dialedAddress)
        {
            if (planetSystem == null)
            {
                return null;
            }

            if (dialedAddress.NullOrEmpty())
            {
                Messages.Message(StarGateText.Get("StarGate_AddressInvalid"), MessageTypeDefOf.RejectInput, false);
                return null;
            }

            if (planetSystem.IsHomeAddress(dialedAddress))
            {
                return null;
            }

            planetSystem.RegisterUsedAddress(dialedAddress);
            return planetSystem.EnsurePlanetForAddress(dialedAddress);
        }

        private static Map EnsureOffworldMap(StarGatePlanetRecord record, string siteId, Map sourceMap)
        {
            if (record == null)
            {
                return null;
            }

            StarGatePlanetSystem planetSystem = Current.Game.GetComponent<StarGatePlanetSystem>();
            StarGateSiteRecord site = null;
            if (!siteId.NullOrEmpty())
            {
                site = planetSystem?.SiteForId(record, siteId);
                if (site == null || (!site.known && !site.visited))
                {
                    Messages.Message(StarGateText.Get("StarGate_TargetUnavailable"), MessageTypeDefOf.NeutralEvent, false);
                    site = null;
                }
            }

            if (site == null)
            {
                site = record.PrimaryGateSite();
            }

            if (site == null)
            {
                return null;
            }

            if (site.mapUniqueId >= 0)
            {
                Map existing = Current.Game.Maps.FirstOrDefault(existingMap => existingMap != null && existingMap.uniqueID == site.mapUniqueId);
                if (existing != null)
                {
                    if (!StarGatePlanetMapFactory.ParentMatches(existing, record, site))
                    {
                        ClearStaleSiteMap(site);
                        Log.Warning("StarGate rejected a saved map binding that belonged to another address or site.");
                    }
                    else if (existing == sourceMap)
                    {
                        ClearStaleSiteMap(site);
                        Messages.Message(StarGateText.Get("StarGate_CreatingPlanet"), MessageTypeDefOf.NeutralEvent, false);
                    }
                    else
                    {
                    StarGatePlanetWorldUtility.SelectLayerFor(existing);
                    Messages.Message(StarGateText.Format("StarGate_RestoringPlanet", record.displayName), MessageTypeDefOf.PositiveEvent, false);
                    return existing;
                    }
                }
            }

            Map existingWorldObjectMap = ExistingSiteMap(record, site);
            if (existingWorldObjectMap != null)
            {
                if (existingWorldObjectMap == sourceMap)
                {
                    ClearStaleSiteMap(site);
                    Messages.Message(StarGateText.Get("StarGate_CreatingPlanet"), MessageTypeDefOf.NeutralEvent, false);
                }
                else
                {
                StarGatePlanetWorldUtility.SelectLayerFor(existingWorldObjectMap);
                planetSystem?.RegisterGeneratedMap(record, site, existingWorldObjectMap, existingWorldObjectMap.Parent);
                    Messages.Message(StarGateText.Format("StarGate_RestoringPlanet", record.displayName), MessageTypeDefOf.PositiveEvent, false);
                return existingWorldObjectMap;
                }
            }

            try
            {
                Map map = GenerateDetachedPlanetMap(record, site, sourceMap);
                if (map != null)
                {
                    if (map == sourceMap)
                    {
                        ClearStaleSiteMap(site);
                        Log.Warning("StarGate generated destination was the source map; rejecting it.");
                    }
                    else
                    {
                    StarGatePlanetWorldUtility.SelectLayerFor(map);
                    planetSystem?.RegisterGeneratedMap(record, site, map, map.Parent);
                    Messages.Message(StarGateText.Format("StarGate_CreatedPlanet", record.displayName), MessageTypeDefOf.PositiveEvent, false);
                    return map;
                    }
                }
            }
            catch (System.Exception exception)
            {
                Log.Warning("StarGate pocket-map generation failed. " + exception);
            }

            Messages.Message(StarGateText.Get("StarGate_PlanetFailed"), MessageTypeDefOf.RejectInput, false);
            return null;
        }

        private static void ClearStaleSiteMap(StarGateSiteRecord site)
        {
            if (site == null)
            {
                return;
            }

            site.mapUniqueId = -1;
            site.worldObjectId = -1;
            site.tile = -1;
            site.mapState = "uncreated";
            site.contentGenerated = false;
        }

        private static Map ExistingSiteMap(StarGatePlanetRecord record, StarGateSiteRecord site)
        {
            if (site == null || site.worldObjectId < 0)
            {
                return null;
            }

            MapParent parent = StarGatePlanetMapFactory.ParentForId(site.worldObjectId);
            if (parent == null)
            {
                parent = Find.WorldObjects.AllWorldObjects
                .FirstOrDefault(worldObject => worldObject != null && worldObject.ID == site.worldObjectId) as MapParent;
            }

            if (parent == null)
            {
                return null;
            }

            if (parent.HasMap)
            {
                return StarGatePlanetMapFactory.ParentMatches(parent.Map, record, site) ? parent.Map : null;
            }

            if (parent is StarGatePlanetMapParent)
            {
                Find.World.pocketMaps.Remove((StarGatePlanetMapParent)parent);
                return null;
            }

            IntVec3 mapSize = new IntVec3(site.mapSize > 0 ? site.mapSize : DestinationMapSize.x, 1, site.mapSize > 0 ? site.mapSize : DestinationMapSize.z);
            Rand.PushState(site.seed == 0 ? record.generationSeed : site.seed);
            try
            {
                return GetOrGenerateMapUtility.GetOrGenerateMap(parent.Tile, mapSize, parent.def);
            }
            finally
            {
                Rand.PopState();
            }
        }

        private static Map GenerateDetachedPlanetMap(StarGatePlanetRecord record, StarGateSiteRecord site, Map sourceMap)
        {
            if (site == null)
            {
                return null;
            }

            StarGatePlanetWorldUtility.EnsureLayer(record);
            return StarGatePlanetWorldUtility.GenerateLandingMap(record, site, sourceMap);
        }

        private static bool TryFindGateCell(Map map, ThingDef gateDef, out IntVec3 cell)
        {
            foreach (IntVec3 candidate in GenRadial.RadialCellsAround(map.Center, 18f, true).OrderBy(position => position.DistanceTo(map.Center)))
            {
                if (candidate.InBounds(map)
                    && candidate.Standable(map)
                    && GenAdj.OccupiedRect(candidate, Rot4.North, gateDef.size).InBounds(map)
                    && GenConstruct.CanPlaceBlueprintAt(gateDef, candidate, Rot4.North, map).Accepted)
                {
                    cell = candidate;
                    return true;
                }
            }

            for (int i = 0; i < 2000; i++)
            {
                IntVec3 candidate = CellFinder.RandomCell(map);
                if (!candidate.Standable(map))
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

        private static int MovePawns(List<Pawn> pawns, CompStarGate destinationGate)
        {
            List<Pawn> travelers = pawns
                .Where(pawn => pawn != null && pawn.Spawned && pawn.Faction == Faction.OfPlayer)
                .Distinct()
                .ToList();
            if (travelers.Count == 0 || destinationGate?.parent?.Map == null)
            {
                return 0;
            }

            List<IntVec3> targetCells = ArrivalCells(destinationGate, travelers.Count);
            if (targetCells.Count < travelers.Count)
            {
                return 0;
            }

            List<PawnTravelOrigin> origins = travelers
                .Select(pawn => new PawnTravelOrigin(pawn, pawn.Map, pawn.Position))
                .ToList();

            try
            {
                for (int i = 0; i < travelers.Count; i++)
                {
                    Pawn pawn = travelers[i];
                    pawn.DeSpawn(DestroyMode.Vanish);
                    GenSpawn.Spawn(pawn, targetCells[i], destinationGate.parent.Map);
                    pawn.Notify_Teleported(false, true);
                }

                CameraJumper.TryJump(destinationGate.parent);
                return travelers.Count;
            }
            catch (System.Exception exception)
            {
                Log.Error("StarGate pawn transfer failed and will be rolled back: " + exception);
                RollBackPawns(origins);
                return 0;
            }
        }

        private static List<IntVec3> ArrivalCells(CompStarGate destinationGate, int count)
        {
            Map map = destinationGate.parent.Map;
            List<IntVec3> cells = GenRadial.RadialCellsAround(destinationGate.parent.Position, 9f, true)
                .Where(cell => cell.InBounds(map)
                    && cell.Standable(map)
                    && !cell.Fogged(map)
                    && cell.GetFirstPawn(map) == null)
                .OrderBy(cell => cell.DistanceTo(destinationGate.parent.Position))
                .Take(count)
                .ToList();
            return cells;
        }

        private static void RollBackPawns(List<PawnTravelOrigin> origins)
        {
            foreach (PawnTravelOrigin origin in origins)
            {
                Pawn pawn = origin.pawn;
                if (pawn == null || origin.map == null)
                {
                    continue;
                }

                if (pawn.Spawned && pawn.Map == origin.map)
                {
                    continue;
                }

                if (pawn.Spawned)
                {
                    pawn.DeSpawn(DestroyMode.Vanish);
                }

                IntVec3 cell = origin.position.InBounds(origin.map) && origin.position.Standable(origin.map)
                    ? origin.position
                    : CellFinder.StandableCellNear(origin.position, origin.map, 6f);
                if (cell.IsValid)
                {
                    GenSpawn.Spawn(pawn, cell, origin.map);
                    pawn.Notify_Teleported(false, true);
                }
            }
        }

        private sealed class PawnTravelOrigin
        {
            public readonly Pawn pawn;
            public readonly Map map;
            public readonly IntVec3 position;

            public PawnTravelOrigin(Pawn pawn, Map map, IntVec3 position)
            {
                this.pawn = pawn;
                this.map = map;
                this.position = position;
            }
        }
    }
}
