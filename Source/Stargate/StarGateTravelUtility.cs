using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
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
                Messages.Message("StarGate: nelze projit, kolonista neni dostupny.", MessageTypeDefOf.RejectInput, false);
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
                        Messages.Message("StarGate nedokazala navazat spojeni.", origin.parent, MessageTypeDefOf.RejectInput, false);
                        return;
                    }

                    if (destinationMap == origin.parent.Map)
                    {
                        Messages.Message("StarGate cilova mapa je stejna jako aktualni. Spojeni bylo zruseno, aby nevznikla falesna planeta.", origin.parent, MessageTypeDefOf.RejectInput, false);
                        Log.Warning("StarGate rejected travel because destination map matched source map: " + destinationMap.uniqueID);
                        return;
                    }

                    CompStarGate destinationGate = EnsureGateOnMap(destinationMap, DestinationGateAddress(destinationMap, origin.DialedAddress));
                    if (destinationGate == null)
                    {
                        Messages.Message("Na cilove mape se nepodarilo vytvorit StarGate.", origin.parent, MessageTypeDefOf.RejectInput, false);
                        return;
                    }

                    StarGatePlanetContentUtility.EnsureContent(destinationMap, destinationGate);
                    origin.BringOnline(600);
                    destinationGate.BringOnline(600, true);
                    int movedCount = MovePawns(pawns, destinationGate);
                    if (movedCount == 0)
                    {
                        Messages.Message("StarGate nenasla volne misto pro prichod.", destinationGate.parent, MessageTypeDefOf.RejectInput, false);
                    }
                    else
                    {
                        Current.Game.GetComponent<StarGatePlanetSystem>()?.RegisterVisit(destinationGate.parent.Map);
                        Messages.Message("StarGate pruchod dokoncen: mapa " + origin.parent.Map.uniqueID + " -> " + destinationMap.uniqueID + ".", destinationGate.parent, MessageTypeDefOf.PositiveEvent, false);
                    }
                }
                catch (System.Exception exception)
                {
                    Log.Error("StarGate travel failed: " + exception);
                    Messages.Message("StarGate pruchod selhal. Podivej se do error logu.", origin.parent, MessageTypeDefOf.RejectInput, false);
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
                Messages.Message(pawn.LabelShort + " vstoupil do prichoziho cerviho otvoru. Prichozi StarGate je jednosmerna.", pawn, MessageTypeDefOf.NegativeEvent, false);
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
                    Messages.Message("StarGate navazuje spojeni s domovskou planetou.", MessageTypeDefOf.PositiveEvent, false);
                    return homeMap;
                }

                Messages.Message("StarGate nenasla ulozenou domovskou mapu.", MessageTypeDefOf.RejectInput, false);
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
                Messages.Message("StarGate nema vytočenou platnou adresu.", MessageTypeDefOf.RejectInput, false);
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
                    Messages.Message("StarGate cilova site neni dostupna. Spojeni se presmeruje na hlavni branu planety.", MessageTypeDefOf.NeutralEvent, false);
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
                    if (existing == sourceMap)
                    {
                        ClearStaleSiteMap(site);
                        Messages.Message("StarGate cil ukazoval na aktualni mapu. Vytvarim novou planetarni mapu.", MessageTypeDefOf.NeutralEvent, false);
                    }
                    else
                    {
                    Messages.Message("StarGate obnovuje spojeni se znamou planetou: " + record.displayName, MessageTypeDefOf.PositiveEvent, false);
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
                    Messages.Message("StarGate ulozeny site smeroval na aktualni mapu. Vytvarim novou planetarni mapu.", MessageTypeDefOf.NeutralEvent, false);
                }
                else
                {
                planetSystem?.RegisterGeneratedMap(record, site, existingWorldObjectMap, existingWorldObjectMap.Parent);
                Messages.Message("StarGate znovu otevira ulozenou planetu: " + record.displayName, MessageTypeDefOf.PositiveEvent, false);
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
                    planetSystem?.RegisterGeneratedMap(record, site, map, map.Parent);
                    Messages.Message("StarGate vytvorila novou planetarni lokaci: " + record.displayName + " / " + site.displayName + " (map " + map.uniqueID + ")", MessageTypeDefOf.PositiveEvent, false);
                    return map;
                    }
                }
            }
            catch (System.Exception exception)
            {
                Log.Warning("StarGate pocket-map generation failed. " + exception);
            }

            Messages.Message("StarGate nedokazala vytvorit samostatnou planetarni mapu. Spojeni bylo odmitnuto.", MessageTypeDefOf.RejectInput, false);
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
        }

        private static Map ExistingSiteMap(StarGatePlanetRecord record, StarGateSiteRecord site)
        {
            if (site == null || site.worldObjectId < 0)
            {
                return null;
            }

            MapParent parent = Find.WorldObjects.AllWorldObjects
                .FirstOrDefault(worldObject => worldObject != null && worldObject.ID == site.worldObjectId) as MapParent;
            if (parent == null)
            {
                return null;
            }

            if (parent.HasMap)
            {
                return parent.Map;
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
            WorldObjectDef def = DefDatabase<WorldObjectDef>.GetNamedSilentFail("StarGatePocketPlanet");
            MapGeneratorDef generator = MapGeneratorDefOf.Base_Player;
            if (def == null || generator == null || site == null)
            {
                return null;
            }

            int tile = site.tile;
            if (tile < 0)
            {
                Rand.PushState(site.seed == 0 ? record.generationSeed : site.seed);
                try
                {
                    if (!TryFindPlanetTile(sourceMap, record, out tile))
                    {
                        return null;
                    }
                }
                finally
                {
                    Rand.PopState();
                }
            }

            MapParent parent = (MapParent)WorldObjectMaker.MakeWorldObject(def);
            parent.Tile = tile;
            Find.WorldObjects.Add(parent);
            site.tile = tile;
            site.worldObjectId = parent.ID;

            IntVec3 mapSize = new IntVec3(site.mapSize > 0 ? site.mapSize : DestinationMapSize.x, 1, site.mapSize > 0 ? site.mapSize : DestinationMapSize.z);
            Rand.PushState(site.seed == 0 ? record.generationSeed : site.seed);
            try
            {
                return MapGenerator.GenerateMap(mapSize, parent, generator, null, null, false, false);
            }
            finally
            {
                Rand.PopState();
            }
        }

        private static Map EnsureFallbackWorldSiteMap(StarGatePlanetRecord record, StarGateSiteRecord site, Map sourceMap)
        {
            WorldObjectDef def = DefDatabase<WorldObjectDef>.GetNamedSilentFail("StarGateWorldSite");
            Settlement playerSettlement = Find.WorldObjects.Settlements.FirstOrDefault(settlement => settlement.Faction == Faction.OfPlayer);
            if (def == null || playerSettlement == null || !TryFindFallbackTile(playerSettlement.Tile, record, out int tile))
            {
                return null;
            }

            MapParent parent = (MapParent)WorldObjectMaker.MakeWorldObject(def);
            parent.Tile = tile;
            Find.WorldObjects.Add(parent);
            if (site == null)
            {
                site = record.PrimaryGateSite();
            }

            site.tile = tile;
            site.worldObjectId = parent.ID;
            Map map = GetOrGenerateMapUtility.GetOrGenerateMap(parent.Tile, DestinationMapSize, parent.def);
            if (map != null)
            {
                if (map == sourceMap)
                {
                    ClearStaleSiteMap(site);
                    Messages.Message("StarGate fallback vratil aktualni mapu. Spojeni bylo odmitnuto.", MessageTypeDefOf.RejectInput, false);
                    return null;
                }

                Current.Game.GetComponent<StarGatePlanetSystem>()?.RegisterGeneratedMap(record, site, map, parent);
            }

            return map;
        }

        private static bool TryFindPlanetTile(Map sourceMap, StarGatePlanetRecord record, out int tile)
        {
            int originTile = -1;
            if (sourceMap != null && sourceMap.Tile >= 0)
            {
                originTile = sourceMap.Tile;
            }

            if (originTile < 0)
            {
                Settlement playerSettlement = Find.WorldObjects.Settlements.FirstOrDefault(settlement => settlement.Faction == Faction.OfPlayer);
                if (playerSettlement != null)
                {
                    originTile = playerSettlement.Tile;
                }
            }

            if (originTile >= 0 && TryFindFallbackTile(originTile, record, out tile))
            {
                return true;
            }

            for (int i = 0; i < 2000; i++)
            {
                int candidate = Rand.Range(0, Find.WorldGrid.TilesCount);
                Tile worldTile = Find.WorldGrid[candidate];
                if (worldTile == null || worldTile.PrimaryBiome == null || worldTile.PrimaryBiome.impassable)
                {
                    continue;
                }

                if (worldTile.hilliness == Hilliness.Impassable || !TileMatchesPlanetType(worldTile, record))
                {
                    continue;
                }

                tile = candidate;
                return true;
            }

            tile = -1;
            return false;
        }

        private static bool TryFindFallbackTile(int playerTile, out int tile)
        {
            return TryFindFallbackTile(playerTile, null, out tile);
        }

        private static bool TryFindFallbackTile(int playerTile, StarGatePlanetRecord record, out int tile)
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

                if (worldTile.hilliness == Hilliness.Impassable || Find.WorldObjects.ObjectsAt(candidate).Any() || !TileMatchesPlanetType(worldTile, record))
                {
                    continue;
                }

                tile = candidate;
                return true;
            }

            tile = -1;
            return record != null && TryFindFallbackTile(playerTile, null, out tile);
        }

        private static bool TileMatchesPlanetType(Tile tile, StarGatePlanetRecord record)
        {
            if (tile == null || record == null || record.planetType.NullOrEmpty())
            {
                return true;
            }

            string biome = tile.PrimaryBiome?.defName?.ToLowerInvariant() ?? string.Empty;
            switch (record.planetType)
            {
                case "desert":
                    return biome.Contains("desert") || biome.Contains("arid") || biome.Contains("dune");
                case "ice":
                    return biome.Contains("ice") || biome.Contains("tundra") || biome.Contains("seaice");
                case "toxic":
                    return biome.Contains("pollution") || biome.Contains("waste") || biome.Contains("toxic") || biome.Contains("swamp");
                case "forest":
                    return biome.Contains("forest") || biome.Contains("temperate") || biome.Contains("boreal");
                case "ancient_ruins":
                    return !biome.Contains("ocean") && !biome.Contains("seaice");
                default:
                    return true;
            }
        }

        private static bool TryFindGateCell(Map map, ThingDef gateDef, out IntVec3 cell)
        {
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
            int movedCount = 0;
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
                movedCount++;
            }

            if (movedCount > 0)
            {
                CameraJumper.TryJump(destinationGate.parent);
            }

            return movedCount;
        }
    }
}
