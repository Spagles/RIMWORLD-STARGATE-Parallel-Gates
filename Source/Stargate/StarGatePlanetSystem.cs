using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimGateJaffaKree
{
    public class StarGatePlanetSystem : GameComponent
    {
        private List<StarGatePlanetRecord> planets = new List<StarGatePlanetRecord>();
        private string homeAddress;

        public StarGatePlanetSystem(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref planets, "stargatePlanets", LookMode.Deep);
            Scribe_Values.Look(ref homeAddress, "homeAddress");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && planets == null)
            {
                planets = new List<StarGatePlanetRecord>();
            }
        }

        public string HomeAddress => EnsureHomeAddress();

        public string EnsureHomeAddress()
        {
            if (!homeAddress.NullOrEmpty())
            {
                return homeAddress;
            }

            homeAddress = GenerateAddress();
            return homeAddress;
        }

        public StarGatePlanetRecord EnsurePlanetForAddress(string address)
        {
            if (address.NullOrEmpty())
            {
                return null;
            }

            if (address == HomeAddress)
            {
                return null;
            }

            StarGatePlanetRecord existing = planets.FirstOrDefault(planet => planet.address == address);
            if (existing != null)
            {
                return existing;
            }

            StarGatePlanetRecord record = new StarGatePlanetRecord
            {
                id = "planet_" + address.Replace("-", string.Empty),
                displayName = "P-" + address.Replace("-", string.Empty).Substring(0, 6),
                address = address,
                seed = "StarGate-" + address,
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
                address = "O01-M02-I03-O04-M05-I06-O07",
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

        private string GenerateAddress()
        {
            List<string> symbols = new List<string>();
            string[] rings = { "O", "M", "I" };
            for (int i = 0; i < 7; i++)
            {
                symbols.Add(rings[Rand.Range(0, rings.Length)] + Rand.RangeInclusive(1, 20).ToString("00"));
            }

            return string.Join("-", symbols.ToArray());
        }
    }

    public class StarGatePlanetRecord : IExposable
    {
        public string id;
        public string displayName;
        public string address;
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
            Scribe_Values.Look(ref address, "address");
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
