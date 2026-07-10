using System;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimGateJaffaKree
{
    public static class StarGatePlanetMapFactory
    {
        public const int MapSize = 150;

        public static Map Generate(StarGatePlanetRecord planet, StarGateSiteRecord site, Map homeMap)
        {
            if (planet == null || site == null || homeMap == null || Find.World == null)
            {
                return null;
            }

            WorldObjectDef parentDef = DefDatabase<WorldObjectDef>.GetNamedSilentFail("StarGatePlanetPocketMap");
            MapGeneratorDef generatorDef = GeneratorFor(planet.planetType);
            if (parentDef == null || generatorDef == null)
            {
                Log.Error("StarGate cannot generate a planet map because its pocket-map defs are missing.");
                return null;
            }

            StarGatePlanetMapParent parent = WorldObjectMaker.MakeWorldObject(parentDef) as StarGatePlanetMapParent;
            if (parent == null)
            {
                Log.Error("StarGatePlanetPocketMap did not create a StarGatePlanetMapParent.");
                return null;
            }

            parent.sourceMap = homeMap;
            parent.mapGenerator = generatorDef;
            // Pocket maps are not world objects, but several vanilla temperature systems
            // still require a valid tile while the map is finishing initialization.
            parent.Tile = homeMap.Tile;
            parent.address = planet.address;
            parent.siteId = site.id;
            parent.generationSeed = EffectiveSeed(planet, site);
            parent.generationVersion = StarGatePlanetMapParent.CurrentGenerationVersion;

            int size = site.mapSize > 0 ? site.mapSize : MapSize;
            Map map = null;
            Rand.PushState(parent.generationSeed);
            try
            {
                map = MapGenerator.GenerateMap(
                    new IntVec3(size, 1, size),
                    parent,
                    generatorDef,
                    null,
                    null,
                    true,
                    false);

                Find.World.pocketMaps.Add(parent);
                return map;
            }
            catch (Exception exception)
            {
                Log.Error("StarGate pocket planet generation failed for " + planet.address + " / " + site.id + ": " + exception);
                if (Current.Game?.Maps != null)
                {
                    foreach (Map generatedMap in Current.Game.Maps
                        .Where(candidate => candidate != null && candidate.Parent == parent)
                        .ToList())
                    {
                        try
                        {
                            Current.Game.DeinitAndRemoveMap(generatedMap, false);
                        }
                        catch (Exception cleanupException)
                        {
                            Log.Warning("StarGate could not fully clean up a failed pocket map: " + cleanupException);
                        }
                    }
                }

                Find.World.pocketMaps.Remove(parent);
                return null;
            }
            finally
            {
                Rand.PopState();
            }
        }

        public static StarGatePlanetMapParent ParentForId(int worldObjectId)
        {
            if (worldObjectId < 0 || Find.World?.pocketMaps == null)
            {
                return null;
            }

            return Find.World.pocketMaps.Find(parent => parent != null && parent.ID == worldObjectId) as StarGatePlanetMapParent;
        }

        public static bool ParentMatches(Map map, StarGatePlanetRecord planet, StarGateSiteRecord site)
        {
            StarGatePlanetMapParent parent = map?.Parent as StarGatePlanetMapParent;
            if (parent == null)
            {
                return true;
            }

            return parent.address == planet?.address && parent.siteId == site?.id;
        }

        private static int EffectiveSeed(StarGatePlanetRecord planet, StarGateSiteRecord site)
        {
            return site.seed != 0 ? site.seed : planet.generationSeed;
        }

        private static MapGeneratorDef GeneratorFor(string planetType)
        {
            string defName;
            switch (planetType)
            {
                case "forest":
                    defName = "StarGatePlanetForest";
                    break;
                case "desert":
                    defName = "StarGatePlanetDesert";
                    break;
                case "ice":
                    defName = "StarGatePlanetIce";
                    break;
                case "toxic":
                    defName = "StarGatePlanetToxic";
                    break;
                case "ancient_ruins":
                    defName = "StarGatePlanetAncientRuins";
                    break;
                default:
                    defName = "StarGatePlanetTemperate";
                    break;
            }

            return DefDatabase<MapGeneratorDef>.GetNamedSilentFail(defName)
                ?? DefDatabase<MapGeneratorDef>.GetNamedSilentFail("StarGatePlanet");
        }
    }
}
