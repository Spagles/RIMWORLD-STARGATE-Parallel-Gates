using RimWorld;
using Verse;

namespace RimGateJaffaKree
{
    public class GenStep_StarGatePlanetSurface : GenStep
    {
        public override int SeedPart => 71943125;

        public override void Generate(Map map, GenStepParams parms)
        {
            TerrainDef soil = TerrainDefOf.Soil;
            TerrainDef richSoil = DefDatabase<TerrainDef>.GetNamedSilentFail("SoilRich") ?? soil;
            TerrainDef gravel = DefDatabase<TerrainDef>.GetNamedSilentFail("Gravel") ?? soil;
            TerrainDef sand = DefDatabase<TerrainDef>.GetNamedSilentFail("Sand") ?? soil;
            TerrainDef marsh = DefDatabase<TerrainDef>.GetNamedSilentFail("MarshyTerrain") ?? soil;
            TerrainDef water = DefDatabase<TerrainDef>.GetNamedSilentFail("WaterMovingShallow") ?? soil;

            foreach (IntVec3 cell in map.AllCells)
            {
                float distance = cell.DistanceTo(map.Center);
                TerrainDef terrain = soil;
                if (distance > map.Size.x * 0.42f || distance > map.Size.z * 0.42f)
                {
                    terrain = gravel;
                }
                else if (Rand.Value < 0.08f)
                {
                    terrain = sand;
                }
                else if (Rand.Value < 0.06f)
                {
                    terrain = richSoil;
                }
                else if (Rand.Value < 0.025f)
                {
                    terrain = marsh;
                }
                else if (Rand.Value < 0.01f)
                {
                    terrain = water;
                }

                map.terrainGrid.SetTerrain(cell, terrain);
            }

            ScatterThings(map, "Plant_TreeOak", 150, 0.85f, 18f);
            ScatterThings(map, "Plant_TreePine", 90, 0.8f, 18f);
            ScatterThings(map, "Plant_Bush", 180, 0.7f, 12f);
            ScatterThings(map, "ChunkGranite", 45, 0f, 10f);
            ScatterThings(map, "ChunkLimestone", 30, 0f, 10f);
            ScatterThings(map, "MineableSteel", 26, 0f, 30f);
            ScatterThings(map, "MineableComponentsIndustrial", 10, 0f, 35f);
            ScatterRuins(map);
        }

        private void ScatterThings(Map map, string defName, int count, float plantGrowth, float clearRadius)
        {
            ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (thingDef == null)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                if (!TryFindScatterCell(map, thingDef, clearRadius, out IntVec3 cell))
                {
                    continue;
                }

                Thing thing = ThingMaker.MakeThing(thingDef);
                Plant plant = thing as Plant;
                if (plant != null)
                {
                    plant.Growth = Rand.Range(plantGrowth * 0.45f, plantGrowth);
                }

                GenSpawn.Spawn(thing, cell, map);
            }
        }

        private bool TryFindScatterCell(Map map, ThingDef thingDef, float clearRadius, out IntVec3 cell)
        {
            for (int i = 0; i < 120; i++)
            {
                IntVec3 candidate = CellFinder.RandomCell(map);
                if (!candidate.InBounds(map) || candidate.DistanceTo(map.Center) < clearRadius || !candidate.Standable(map))
                {
                    continue;
                }

                if (candidate.GetFirstThing(map, thingDef) != null || candidate.GetEdifice(map) != null)
                {
                    continue;
                }

                cell = candidate;
                return true;
            }

            cell = IntVec3.Invalid;
            return false;
        }

        private void ScatterRuins(Map map)
        {
            ThingDef wallDef = ThingDef.Named("Wall");
            ThingDef steelDef = ThingDefOf.Steel;
            for (int i = 0; i < 4; i++)
            {
                IntVec3 center = CellFinder.RandomCell(map);
                if (!center.InBounds(map) || center.DistanceTo(map.Center) < 25f)
                {
                    continue;
                }

                int width = Rand.RangeInclusive(5, 10);
                int height = Rand.RangeInclusive(4, 8);
                CellRect rect = CellRect.CenteredOn(center, width, height).ClipInsideMap(map);
                foreach (IntVec3 cell in rect.EdgeCells)
                {
                    if (!cell.Standable(map) || cell.GetEdifice(map) != null || Rand.Chance(0.25f))
                    {
                        continue;
                    }

                    Thing wall = ThingMaker.MakeThing(wallDef, steelDef);
                    wall.SetFactionDirect(Faction.OfAncientsHostile);
                    GenSpawn.Spawn(wall, cell, map);
                }
            }
        }
    }
}
