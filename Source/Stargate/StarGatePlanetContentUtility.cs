using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using UnityEngine;
using Verse;

namespace RimGateJaffaKree
{
    public static class StarGatePlanetContentUtility
    {
        public static void EnsureContent(Map map, CompStarGate gate)
        {
            if (map == null || gate?.parent == null || map.IsPlayerHome)
            {
                return;
            }

            StarGatePlanetSystem planetSystem = Current.Game.GetComponent<StarGatePlanetSystem>();
            StarGatePlanetRecord planet = planetSystem?.PlanetForMap(map);
            StarGateSiteRecord site = planetSystem?.SiteForMap(map);
            if (planet == null || site == null)
            {
                return;
            }

            ShowDiscoveryLetter(planetSystem, planet, site, gate.parent);
            if (site.contentGenerated)
            {
                return;
            }

            Rand.PushState((site.seed == 0 ? planet.generationSeed : site.seed) + 93071);
            try
            {
                IntVec3 gateCell = gate.parent.Position;
                PrepareArrivalZone(map, gateCell);
                BuildAncientPlatform(map, gateCell);
                GeneratePointOfInterest(map, gateCell, planet, site);
                ScatterResources(map, gateCell, planet);
                ScatterPlanetFlavor(map, gateCell, planet);
                site.contentGenerated = true;
            }
            finally
            {
                Rand.PopState();
            }
        }

        private static void GeneratePointOfInterest(Map map, IntVec3 gateCell, StarGatePlanetRecord planet, StarGateSiteRecord site)
        {
            string contentKind = site.contentKind.NullOrEmpty() ? "wilderness" : site.contentKind;
            if (contentKind == "settlement" || contentKind == "outpost")
            {
                Faction faction = ResolveSettlementFaction(planet, site, contentKind == "outpost");
                if (faction != null && TryGenerateSettlement(map, gateCell, planet, site, faction))
                {
                    return;
                }

                site.contentKind = "ancient_ruins";
                site.factionLoadId = -1;
                site.factionDefName = null;
            }

            if (site.contentKind == "ancient_ruins" || site.contentKind == "ruins")
            {
                ScatterRuins(map, gateCell, planet, site.contentKind == "ancient_ruins" ? 5 : 2);
            }
        }

        private static Faction ResolveSettlementFaction(StarGatePlanetRecord planet, StarGateSiteRecord site, bool hostileOnly)
        {
            if (Find.FactionManager == null)
            {
                return null;
            }

            Faction stored = Find.FactionManager.AllFactions
                .FirstOrDefault(faction => faction != null && faction.loadID == site.factionLoadId && !faction.defeated);
            if (stored == null && !site.factionDefName.NullOrEmpty())
            {
                stored = Find.FactionManager.AllFactions
                    .FirstOrDefault(faction => faction != null && faction.def?.defName == site.factionDefName && !faction.defeated);
            }

            if (stored != null)
            {
                return stored;
            }

            if (site.factionLoadId >= 0 || !site.factionDefName.NullOrEmpty())
            {
                return null;
            }

            List<Faction> candidates = Find.FactionManager.AllFactionsVisible
                .Where(IsEligibleSettlementFaction)
                .ToList();

            if (hostileOnly)
            {
                List<Faction> hostile = candidates
                    .Where(faction => faction.PlayerRelationKind == FactionRelationKind.Hostile)
                    .ToList();
                if (hostile.Count > 0)
                {
                    candidates = hostile;
                }
            }
            else if (planet.civilizationLevel == "Scattered tribes")
            {
                List<Faction> tribal = candidates
                    .Where(faction => faction.def.techLevel <= TechLevel.Neolithic)
                    .ToList();
                if (tribal.Count > 0)
                {
                    candidates = tribal;
                }
            }

            if (planet.IsJaffaControlled)
            {
                List<Faction> jaffa = candidates
                    .Where(faction => faction.def.defName.StartsWith("Jaffa"))
                    .ToList();
                if (jaffa.Count > 0)
                {
                    candidates = hostileOnly ? jaffa : candidates.Where(faction => !faction.def.defName.StartsWith("Jaffa") && faction.def.techLevel <= TechLevel.Neolithic).ToList();
                    if (candidates.Count == 0)
                    {
                        candidates = jaffa;
                    }
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            List<Faction> weighted = new List<Faction>();
            foreach (Faction candidate in candidates.OrderBy(faction => faction.def.defName).ThenBy(faction => faction.loadID))
            {
                weighted.Add(candidate);
                if (candidate.def.defName.StartsWith("Jaffa"))
                {
                    weighted.Add(candidate);
                    weighted.Add(candidate);
                }
            }

            Rand.PushState((planet.IsJaffaControlled ? planet.generationSeed : (site.seed == 0 ? planet.generationSeed : site.seed)) ^ 0x4F31A);
            try
            {
                Faction selected = weighted[Rand.Range(0, weighted.Count)];
                site.factionLoadId = selected.loadID;
                site.factionDefName = selected.def.defName;
                return selected;
            }
            finally
            {
                Rand.PopState();
            }
        }

        private static bool IsEligibleSettlementFaction(Faction faction)
        {
            return faction != null
                && !faction.IsPlayer
                && !faction.defeated
                && faction.def != null
                && !faction.def.hidden
                && faction.def.humanlikeFaction
                && faction.def.settlementGenerationWeight > 0f
                && faction.def.pawnGroupMakers != null
                && faction.def.pawnGroupMakers.Count > 0;
        }

        private static bool TryGenerateSettlement(Map map, IntVec3 gateCell, StarGatePlanetRecord planet, StarGateSiteRecord site, Faction faction)
        {
            if (!TryFindSettlementRect(map, gateCell, out CellRect rect))
            {
                return false;
            }

            try
            {
                BaseGen.Reset();
                BaseGen.globalSettings.map = map;
                BaseGen.globalSettings.mainRect = rect;
                ResolveParams resolveParams = new ResolveParams
                {
                    rect = rect,
                    faction = faction,
                    settlementPawnGroupPoints = Mathf.Max(300f, site.threatLevel * 140f)
                };
                BaseGen.symbolStack.Push("settlement", resolveParams, null);
                BaseGen.Generate();
                return true;
            }
            catch (System.Exception exception)
            {
                Log.Warning("StarGate settlement generation failed for " + planet.address + " / " + site.id + ": " + exception);
                return false;
            }
            finally
            {
                BaseGen.Reset();
            }
        }

        private static bool TryFindSettlementRect(Map map, IntVec3 gateCell, out CellRect rect)
        {
            int width = 34;
            int height = 30;
            IntVec3[] centers =
            {
                new IntVec3(map.Size.x - 35, 0, map.Size.z - 35),
                new IntVec3(35, 0, map.Size.z - 35),
                new IntVec3(map.Size.x - 35, 0, 35),
                new IntVec3(35, 0, 35)
            };

            foreach (IntVec3 center in centers.OrderBy(candidate => StarGatePlanetSystem.StableSeed(candidate.ToString())))
            {
                CellRect candidate = CellRect.CenteredOn(center, width, height).ClipInsideMap(map);
                if (candidate.Width < width || candidate.Height < height || candidate.ClosestCellTo(gateCell).DistanceTo(gateCell) < 20f)
                {
                    continue;
                }

                int standable = candidate.Cells.Count(cell => cell.Standable(map));
                if (standable >= candidate.Area * 0.7f)
                {
                    rect = candidate;
                    return true;
                }
            }

            rect = CellRect.Empty;
            return false;
        }

        private static void ShowDiscoveryLetter(StarGatePlanetSystem planetSystem, StarGatePlanetRecord planet, StarGateSiteRecord site, Thing gate)
        {
            if (planet.discovered)
            {
                return;
            }

            planetSystem.MarkPlanetDiscovered(planet);
            string title = "StarGate planet discovered";
            string text = "Your colonists have stepped through the wormhole and reached " + planet.displayName + ".\n\n"
                + "Address: " + planet.address + "\n"
                + "Planet seed: " + planet.generationSeed + "\n"
                + "Planet type: " + planet.planetType + "\n"
                + "Atmosphere: " + planet.atmosphere + "\n"
                + "Civilization trace: " + planet.civilizationLevel + "\n"
                + "Threat level: " + planet.threatLevel + "/10\n"
                + "Resource richness: " + planet.resourceRichness + "/10\n"
                + "Primary site: " + site.displayName;

            Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.PositiveEvent, gate);
        }

        private static void PrepareArrivalZone(Map map, IntVec3 center)
        {
            TerrainDef floor = DefDatabase<TerrainDef>.GetNamedSilentFail("PavedTile")
                ?? DefDatabase<TerrainDef>.GetNamedSilentFail("Concrete")
                ?? TerrainDefOf.Soil;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 8f, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                foreach (Thing thing in cell.GetThingList(map).ToList())
                {
                    if (thing.def.defName == "StarGate" || thing.def.defName == "StarGate_Control_Panel" || thing is Pawn)
                    {
                        continue;
                    }

                    if (thing.def.category == ThingCategory.Building || thing.def.category == ThingCategory.Plant || thing.def.category == ThingCategory.Item)
                    {
                        thing.Destroy(DestroyMode.Vanish);
                    }
                }

                if (!cell.GetTerrain(map).IsWater)
                {
                    map.terrainGrid.SetTerrain(cell, floor);
                }

                map.fogGrid.Unfog(cell);
            }
        }

        private static void BuildAncientPlatform(Map map, IntVec3 center)
        {
            ThingDef wallDef = ThingDef.Named("Wall");
            ThingDef material = DefDatabase<ThingDef>.GetNamedSilentFail("BlocksGranite")
                ?? ThingDefOf.Steel;

            CellRect ring = CellRect.CenteredOn(center, 11, 9).ClipInsideMap(map);
            foreach (IntVec3 cell in ring.EdgeCells)
            {
                if (!CanSpawnBuildingAt(map, cell) || Rand.Chance(0.35f))
                {
                    continue;
                }

                Thing wall = ThingMaker.MakeThing(wallDef, material);
                wall.HitPoints = Mathf.Max(1, Mathf.RoundToInt(wall.MaxHitPoints * Rand.Range(0.25f, 0.7f)));
                GenSpawn.Spawn(wall, cell, map);
            }
        }

        private static void ScatterRuins(Map map, IntVec3 gateCell, StarGatePlanetRecord planet, int requestedCount)
        {
            int count = planet.planetType == "ancient_ruins" ? System.Math.Max(5, requestedCount) : requestedCount;
            ThingDef wallDef = ThingDef.Named("Wall");
            ThingDef material = DefDatabase<ThingDef>.GetNamedSilentFail("BlocksSandstone")
                ?? DefDatabase<ThingDef>.GetNamedSilentFail("BlocksGranite")
                ?? ThingDefOf.Steel;

            for (int i = 0; i < count; i++)
            {
                if (!TryFindDistantCell(map, gateCell, 16f, 46f, out IntVec3 center))
                {
                    continue;
                }

                CellRect rect = CellRect.CenteredOn(center, Rand.RangeInclusive(5, 9), Rand.RangeInclusive(4, 8)).ClipInsideMap(map);
                foreach (IntVec3 cell in rect.EdgeCells)
                {
                    if (!CanSpawnBuildingAt(map, cell) || Rand.Chance(0.28f))
                    {
                        continue;
                    }

                    Thing wall = ThingMaker.MakeThing(wallDef, material);
                    wall.HitPoints = Mathf.Max(1, Mathf.RoundToInt(wall.MaxHitPoints * Rand.Range(0.2f, 0.65f)));
                    GenSpawn.Spawn(wall, cell, map);
                }

                ScatterThingNear(map, "ChunkSlagSteel", center, Rand.RangeInclusive(2, 5), 6f);
            }
        }

        private static void ScatterResources(Map map, IntVec3 gateCell, StarGatePlanetRecord planet)
        {
            ScatterThingNear(map, "MineableSteel", gateCell, 8, 34f, 60f);
            ScatterThingNear(map, "MineableComponentsIndustrial", gateCell, planet.planetType == "ancient_ruins" ? 8 : 4, 30f, 62f);

            switch (planet.planetType)
            {
                case "desert":
                    ScatterThingNear(map, "MineableGold", gateCell, 4, 34f, 64f);
                    break;
                case "ice":
                    ScatterThingNear(map, "MineableUranium", gateCell, 3, 36f, 66f);
                    break;
                case "toxic":
                    ScatterThingNear(map, "MineablePlasteel", gateCell, 4, 34f, 66f);
                    break;
                case "forest":
                    ScatterThingNear(map, "Plant_TreeOak", gateCell, 14, 18f, 52f);
                    break;
                case "ancient_ruins":
                    ScatterThingNear(map, "MineableComponentsIndustrial", gateCell, 8, 24f, 60f);
                    ScatterThingNear(map, "ShipChunk", gateCell, 2, 28f, 58f);
                    break;
            }
        }

        private static void ScatterPlanetFlavor(Map map, IntVec3 gateCell, StarGatePlanetRecord planet)
        {
            if (planet.planetType == "toxic")
            {
                TerrainDef marsh = DefDatabase<TerrainDef>.GetNamedSilentFail("MarshyTerrain");
                if (marsh != null)
                {
                    PaintTerrainPatches(map, gateCell, marsh, 5, 4f);
                }
            }
            else if (planet.planetType == "desert")
            {
                TerrainDef sand = DefDatabase<TerrainDef>.GetNamedSilentFail("Sand");
                if (sand != null)
                {
                    PaintTerrainPatches(map, gateCell, sand, 7, 5f);
                }
            }
            else if (planet.planetType == "ice")
            {
                ScatterThingNear(map, "ChunkGranite", gateCell, 10, 18f, 56f);
            }
        }

        private static void PaintTerrainPatches(Map map, IntVec3 gateCell, TerrainDef terrain, int count, float radius)
        {
            for (int i = 0; i < count; i++)
            {
                if (!TryFindDistantCell(map, gateCell, 14f, 58f, out IntVec3 patchCenter))
                {
                    continue;
                }

                foreach (IntVec3 cell in GenRadial.RadialCellsAround(patchCenter, radius, true))
                {
                    if (cell.InBounds(map) && cell.Standable(map) && Rand.Chance(0.75f))
                    {
                        map.terrainGrid.SetTerrain(cell, terrain);
                    }
                }
            }
        }

        private static void ScatterThingNear(Map map, string defName, IntVec3 center, int count, float radius)
        {
            ScatterThingNear(map, defName, center, count, 8f, radius);
        }

        private static void ScatterThingNear(Map map, string defName, IntVec3 center, int count, float minRadius, float maxRadius)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                if (!TryFindDistantCell(map, center, minRadius, maxRadius, out IntVec3 cell))
                {
                    continue;
                }

                if (!CanSpawnThingAt(map, def, cell))
                {
                    continue;
                }

                Thing thing = ThingMaker.MakeThing(def);
                Plant plant = thing as Plant;
                if (plant != null)
                {
                    plant.Growth = Rand.Range(0.35f, 0.95f);
                }

                GenSpawn.Spawn(thing, cell, map);
            }
        }

        private static bool TryFindDistantCell(Map map, IntVec3 origin, float minRadius, float maxRadius, out IntVec3 cell)
        {
            for (int i = 0; i < 120; i++)
            {
                IntVec3 candidate = CellFinder.RandomCell(map);
                float distance = candidate.DistanceTo(origin);
                if (distance < minRadius || distance > maxRadius || !candidate.InBounds(map) || !candidate.Standable(map) || candidate.Fogged(map))
                {
                    continue;
                }

                cell = candidate;
                return true;
            }

            cell = IntVec3.Invalid;
            return false;
        }

        private static bool CanSpawnBuildingAt(Map map, IntVec3 cell)
        {
            return cell.InBounds(map)
                && cell.Standable(map)
                && !cell.Fogged(map)
                && cell.GetEdifice(map) == null;
        }

        private static bool CanSpawnThingAt(Map map, ThingDef def, IntVec3 cell)
        {
            if (!cell.InBounds(map) || cell.Fogged(map))
            {
                return false;
            }

            if (def.category == ThingCategory.Building)
            {
                return cell.Standable(map) && cell.GetEdifice(map) == null;
            }

            return cell.Standable(map) && cell.GetFirstThing(map, def) == null;
        }
    }
}
