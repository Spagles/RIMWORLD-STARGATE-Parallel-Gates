using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimGateJaffaKree
{
    public class StarGatePlanetSystem : GameComponent
    {
        private List<StarGatePlanetRecord> planets = new List<StarGatePlanetRecord>();
        private World transientGeneratedWorld;

        public StarGatePlanetSystem(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref planets, "stargatePlanets", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && planets == null)
            {
                planets = new List<StarGatePlanetRecord>();
            }
        }

        public StarGatePlanetRecord EnsurePrimaryOffworldPlanet()
        {
            StarGatePlanetRecord existing = planets.Find(planet => planet.id == "primary_offworld");
            if (existing != null)
            {
                return existing;
            }

            StarGatePlanetRecord record = new StarGatePlanetRecord
            {
                id = "primary_offworld",
                displayName = "P3X-001",
                seed = "StarGate-P3X-001-" + Rand.Range(100000, 999999),
                planetCoverage = 0.3f,
                rainfall = OverallRainfall.Normal,
                temperature = OverallTemperature.Normal,
                population = OverallPopulation.Little,
                landmarkDensity = LandmarkDensity.SlightlySparse,
                pollution = 0f
            };

            planets.Add(record);
            return record;
        }

        public World EnsureTransientWorld(StarGatePlanetRecord record)
        {
            if (transientGeneratedWorld != null)
            {
                return transientGeneratedWorld;
            }

            List<FactionDef> factions = Current.Game.World.info.factions;
            transientGeneratedWorld = WorldGenerator.GenerateWorld(
                record.planetCoverage,
                record.seed,
                record.rainfall,
                record.temperature,
                record.population,
                record.landmarkDensity,
                factions,
                record.pollution);

            transientGeneratedWorld.info.name = record.displayName;
            return transientGeneratedWorld;
        }
    }

    public class StarGatePlanetRecord : IExposable
    {
        public string id;
        public string displayName;
        public string seed;
        public float planetCoverage;
        public OverallRainfall rainfall;
        public OverallTemperature temperature;
        public OverallPopulation population;
        public LandmarkDensity landmarkDensity;
        public float pollution;

        public void ExposeData()
        {
            Scribe_Values.Look(ref id, "id");
            Scribe_Values.Look(ref displayName, "displayName");
            Scribe_Values.Look(ref seed, "seed");
            Scribe_Values.Look(ref planetCoverage, "planetCoverage", 0.3f);
            Scribe_Values.Look(ref rainfall, "rainfall", OverallRainfall.Normal);
            Scribe_Values.Look(ref temperature, "temperature", OverallTemperature.Normal);
            Scribe_Values.Look(ref population, "population", OverallPopulation.Little);
            Scribe_Values.Look(ref landmarkDensity, "landmarkDensity", LandmarkDensity.SlightlySparse);
            Scribe_Values.Look(ref pollution, "pollution", 0f);
        }
    }
}
