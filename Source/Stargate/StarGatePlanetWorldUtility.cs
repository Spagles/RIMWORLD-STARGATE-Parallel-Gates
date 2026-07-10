using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RimGateJaffaKree
{
    public static class StarGatePlanetWorldUtility
    {
        private const string LayerDefName = "StarGatePlanetLayer";
        private const string SettingsDefName = "StarGatePlanetLayer";
        private const string GatewayDefName = "StarGatePlanetGateway";
        private static readonly FieldInfo CreatingWorldField = typeof(Current).GetField("creatingWorldInt", BindingFlags.Static | BindingFlags.NonPublic);

        public static Map GenerateLandingMap(StarGatePlanetRecord planet, StarGateSiteRecord site, Map homeMap)
        {
            StarGatePlanetLayer layer = EnsureLayer(planet);
            if (layer == null || site == null)
            {
                return null;
            }

            PlanetTile tile = EnsureSiteTile(layer, planet, site);
            if (!tile.Valid)
            {
                return null;
            }

            WorldObjectDef def = DefDatabase<WorldObjectDef>.GetNamedSilentFail(GatewayDefName)
                ?? DefDatabase<WorldObjectDef>.GetNamedSilentFail("StarGatePocketPlanet");
            if (def == null)
            {
                Log.Error("StarGatePlanetGateway world object definition is missing.");
                return null;
            }

            MapParent parent = Find.WorldObjects.AllWorldObjects
                .OfType<MapParent>()
                .FirstOrDefault(candidate => candidate.Tile == tile && candidate.def == def);
            if (parent == null)
            {
                parent = WorldObjectMaker.MakeWorldObject(def) as MapParent;
                if (parent == null)
                {
                    return null;
                }

                parent.Tile = tile;
                Find.WorldObjects.Add(parent);
            }

            int size = site.mapSize > 0 ? site.mapSize : StarGatePlanetMapFactory.MapSize;
            Rand.PushState(site.seed == 0 ? planet.generationSeed : site.seed);
            try
            {
                Map map = GetOrGenerateMapUtility.GetOrGenerateMap(parent.Tile, new IntVec3(size, 1, size), parent.def);
                if (map == null)
                {
                    Find.WorldObjects.Remove(parent);
                    return null;
                }

                site.planetLayerId = layer.LayerID;
                site.planetTileId = tile.tileId;
                return map;
            }
            catch
            {
                if (!parent.HasMap)
                {
                    Find.WorldObjects.Remove(parent);
                }

                throw;
            }
            finally
            {
                Rand.PopState();
            }
        }

        public static StarGatePlanetLayer EnsureLayer(StarGatePlanetRecord planet)
        {
            if (planet == null || Find.WorldGrid == null)
            {
                return null;
            }

            StarGatePlanetLayer existing = Find.WorldGrid.PlanetLayers.Values
                .OfType<StarGatePlanetLayer>()
                .FirstOrDefault(layer => layer.LayerID == planet.planetLayerId
                    || (!planet.address.NullOrEmpty() && layer.stargateAddress == planet.address));
            if (existing != null)
            {
                planet.planetLayerId = existing.LayerID;
                return existing;
            }

            PlanetLayerDef layerDef = DefDatabase<PlanetLayerDef>.GetNamedSilentFail(LayerDefName);
            PlanetLayerSettingsDef settingsDef = DefDatabase<PlanetLayerSettingsDef>.GetNamedSilentFail(SettingsDefName);
            if (layerDef == null || settingsDef == null)
            {
                Log.Error("StarGate planet layer definitions are missing.");
                return null;
            }

            PlanetLayer registeredLayer = Find.WorldGrid.RegisterPlanetLayer(layerDef, settingsDef.settings);
            StarGatePlanetLayer created = registeredLayer as StarGatePlanetLayer;
            if (created == null)
            {
                Log.Error("StarGatePlanetLayer definition did not create StarGatePlanetLayer.");
                Find.WorldGrid.RemovePlanetLayer(registeredLayer);
                return null;
            }

            created.stargateAddress = planet.address;
            created.generationSeed = planet.generationSeed;
            created.generationVersion = StarGatePlanetSystem.CurrentGenerationVersion;
            World previousCreatingWorld = Current.CreatingWorld;
            Rand.PushState(planet.generationSeed);
            try
            {
                CreatingWorldField?.SetValue(null, Find.World);
                created.RunWorldGeneration(planet.seed.NullOrEmpty() ? "StarGate-" + planet.address : planet.seed, planet.generationSeed);
                planet.planetLayerId = created.LayerID;
                return created;
            }
            catch
            {
                Find.WorldGrid.RemovePlanetLayer(created);
                throw;
            }
            finally
            {
                CreatingWorldField?.SetValue(null, previousCreatingWorld);
                Rand.PopState();
            }
        }

        public static StarGatePlanetLayer LayerFor(StarGatePlanetRecord planet)
        {
            if (planet == null || Find.WorldGrid == null)
            {
                return null;
            }

            return Find.WorldGrid.PlanetLayers.Values.OfType<StarGatePlanetLayer>()
                .FirstOrDefault(layer => layer.LayerID == planet.planetLayerId
                    || (!planet.address.NullOrEmpty() && layer.stargateAddress == planet.address));
        }

        public static void SelectLayerFor(Map map)
        {
            StarGatePlanetLayer layer = map?.Parent?.Tile.Layer as StarGatePlanetLayer;
            if (layer != null)
            {
                PlanetLayer.Selected = layer;
            }
        }

        private static PlanetTile EnsureSiteTile(StarGatePlanetLayer layer, StarGatePlanetRecord planet, StarGateSiteRecord site)
        {
            if (site.planetLayerId == layer.LayerID && site.planetTileId >= 0 && site.planetTileId < layer.TilesCount)
            {
                PlanetTile saved = layer.PlanetTileForID(site.planetTileId);
                if (saved.tileId >= 0 && saved.tileId < layer.TilesCount && saved.Tile != null)
                {
                    return saved;
                }
            }

            List<PlanetTile> candidates = new List<PlanetTile>();
            for (int i = 0; i < layer.TilesCount; i++)
            {
                PlanetTile candidate = layer.PlanetTileForID(i);
                if (candidate.tileId >= 0 && candidate.tileId < layer.TilesCount && candidate.Tile != null
                    && candidate.Tile.PrimaryBiome != null && !candidate.Tile.PrimaryBiome.impassable)
                {
                    candidates.Add(candidate);
                }
            }

            if (candidates.Count == 0)
            {
                return PlanetTile.Invalid;
            }

            Rand.PushState(site.seed == 0 ? planet.generationSeed : site.seed);
            try
            {
                PlanetTile selected = candidates[Rand.Range(0, candidates.Count)];
                site.planetLayerId = layer.LayerID;
                site.planetTileId = selected.tileId;
                return selected;
            }
            finally
            {
                Rand.PopState();
            }
        }
    }
}
