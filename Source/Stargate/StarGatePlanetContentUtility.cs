using System.Collections.Generic;
using System.Linq;
using RimWorld;
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
                ScatterRuins(map, gateCell, planet);
                ScatterResources(map, gateCell, planet);
                ScatterPlanetFlavor(map, gateCell, planet);
                site.contentGenerated = true;
            }
            finally
            {
                Rand.PopState();
            }
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

        private static void ScatterRuins(Map map, IntVec3 gateCell, StarGatePlanetRecord planet)
        {
            int count = planet.planetType == "ancient_ruins" ? 5 : 2;
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
