using RimWorld;
using Verse;

namespace RimGateJaffaKree
{
    public class GenStep_StarGatePlanetSurface : GenStep
    {
        public override int SeedPart => 71943125;

        public override void Generate(Map map, GenStepParams parms)
        {
            StarGatePlanetMapParent parent = map.Parent as StarGatePlanetMapParent;
            int seed = parent?.generationSeed ?? map.ConstantRandSeed;
            Rand.PushState(seed ^ SeedPart);
            try
            {
                TerrainDef soil = TerrainDefOf.Soil;
                TerrainDef richSoil = DefDatabase<TerrainDef>.GetNamedSilentFail("SoilRich") ?? soil;
                TerrainDef gravel = DefDatabase<TerrainDef>.GetNamedSilentFail("Gravel") ?? soil;
                TerrainDef sand = DefDatabase<TerrainDef>.GetNamedSilentFail("Sand") ?? soil;
                TerrainDef marsh = DefDatabase<TerrainDef>.GetNamedSilentFail("MarshyTerrain") ?? soil;
                TerrainDef water = DefDatabase<TerrainDef>.GetNamedSilentFail("WaterMovingShallow") ?? soil;
                TerrainDef ice = DefDatabase<TerrainDef>.GetNamedSilentFail("Ice") ?? gravel;

                string biome = map.Biome?.defName ?? string.Empty;
                bool desert = biome == "Desert" || biome == "AridShrubland";
                bool frozen = biome == "IceSheet" || biome == "Tundra";
                bool swamp = biome == "TemperateSwamp";

                foreach (IntVec3 cell in map.AllCells)
                {
                    float distance = cell.DistanceTo(map.Center);
                    TerrainDef terrain = soil;
                    if (distance > map.Size.x * 0.44f || distance > map.Size.z * 0.44f)
                    {
                        terrain = gravel;
                    }
                    else if (frozen)
                    {
                        terrain = Rand.Chance(0.82f) ? ice : gravel;
                    }
                    else if (desert)
                    {
                        terrain = Rand.Chance(0.84f) ? sand : gravel;
                    }
                    else if (swamp && Rand.Chance(0.18f))
                    {
                        terrain = Rand.Chance(0.22f) ? water : marsh;
                    }
                    else if (Rand.Chance(0.08f))
                    {
                        terrain = richSoil;
                    }
                    else if (Rand.Chance(0.05f))
                    {
                        terrain = sand;
                    }

                    map.terrainGrid.SetTerrain(cell, terrain);
                }

                if (frozen)
                {
                    ScatterThings(map, "ChunkGranite", 65, 0f, 10f);
                }
                else if (desert)
                {
                    ScatterThings(map, "Plant_Bush", 70, 0.65f, 12f);
                    ScatterThings(map, "ChunkSandstone", 55, 0f, 10f);
                }
                else
                {
                    int treeCount = biome == "BorealForest" ? 260 : 170;
                    ScatterThings(map, biome == "BorealForest" ? "Plant_TreePine" : "Plant_TreeOak", treeCount, 0.85f, 18f);
                    ScatterThings(map, "Plant_Bush", swamp ? 220 : 150, 0.7f, 12f);
                    ScatterThings(map, "ChunkGranite", 45, 0f, 10f);
                    ScatterThings(map, "ChunkLimestone", 30, 0f, 10f);
                }

                ScatterThings(map, "MineableSteel", 26, 0f, 30f);
                ScatterThings(map, "MineableComponentsIndustrial", 10, 0f, 35f);
                ScatterRuins(map);
            }
            finally
            {
                Rand.PopState();
            }
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

                if (thingDef.plant != null && map.fertilityGrid.FertilityAt(candidate) <= 0.01f)
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
