using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimGateJaffaKree
{
    public class StarGatePlanetSystem : GameComponent
    {
        public const int CurrentDataVersion = 2;
        public const int CurrentGenerationVersion = 1;
        private const int RecentAddressLimit = 5;
        private List<StarGatePlanetRecord> planets = new List<StarGatePlanetRecord>();
        private List<string> recentAddresses = new List<string>();
        private int dataVersion = CurrentDataVersion;
        private string homeAddress;
        private int homeTile = -1;
        private int homeMapUniqueId = -1;
        private bool initialized;
        private bool homeAddressMessageShown;

        public StarGatePlanetSystem(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref planets, "stargatePlanets", LookMode.Deep);
            Scribe_Collections.Look(ref recentAddresses, "recentStarGateAddresses", LookMode.Value);
            Scribe_Values.Look(ref dataVersion, "starGatePlanetDataVersion", CurrentDataVersion);
            Scribe_Values.Look(ref homeAddress, "homeAddress");
            Scribe_Values.Look(ref homeTile, "homeTile", -1);
            Scribe_Values.Look(ref homeMapUniqueId, "homeMapUniqueId", -1);
            Scribe_Values.Look(ref initialized, "initialized", false);
            Scribe_Values.Look(ref homeAddressMessageShown, "homeAddressMessageShown", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (planets == null)
                {
                    planets = new List<StarGatePlanetRecord>();
                }

                if (recentAddresses == null)
                {
                    recentAddresses = new List<string>();
                }

                foreach (StarGatePlanetRecord planet in planets)
                {
                    planet.PostLoadNormalize();
                }

                PurgeLegacyPrototypePlanet();
                dataVersion = CurrentDataVersion;
                recentAddresses = recentAddresses
                    .Where(address => IsValidAddress(address) && !IsHomeAddress(address))
                    .Distinct()
                    .Take(RecentAddressLimit)
                    .ToList();
            }
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            RepairInvalidPlanetLayers();
            EnsureInitialized();
            ReconcileLoadedMaps();
        }

        private void RepairInvalidPlanetLayers()
        {
            if (Find.WorldGrid == null)
            {
                return;
            }

            List<StarGatePlanetLayer> invalidLayers = Find.WorldGrid.PlanetLayers.Values
                .OfType<StarGatePlanetLayer>()
                .Where(layer => layer == null || layer.TilesCount <= 0)
                .ToList();
            foreach (StarGatePlanetLayer layer in invalidLayers)
            {
                if (layer == null)
                {
                    continue;
                }

                foreach (StarGatePlanetRecord planet in planets.Where(candidate => candidate != null && candidate.planetLayerId == layer.LayerID))
                {
                    planet.planetLayerId = -1;
                    planet.primaryLayerTileId = -1;
                    foreach (StarGateSiteRecord site in planet.sites ?? new List<StarGateSiteRecord>())
                    {
                        if (site != null && site.planetLayerId == layer.LayerID)
                        {
                            site.planetLayerId = -1;
                            site.planetTileId = -1;
                            site.worldObjectId = -1;
                            site.mapUniqueId = -1;
                            site.tile = -1;
                            site.mapState = "uncreated";
                        }
                    }
                }

                Find.WorldGrid.RemovePlanetLayer(layer);
            }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (!initialized)
            {
                EnsureInitialized();
            }

            TryShowHomeAddressMessage();
        }

        public void EnsureInitialized()
        {
            EnsureHomeAddress();
            EnsureHomeMapRecord();
            PurgeLegacyPrototypePlanet();
            initialized = true;
            TryShowHomeAddressMessage();
        }

        public void TryShowHomeAddressMessage()
        {
            if (homeAddressMessageShown || homeAddress.NullOrEmpty() || Current.ProgramState != ProgramState.Playing)
            {
                return;
            }

            Messages.Message(StarGateText.Format("StarGate_HomeAddress", homeAddress), MessageTypeDefOf.PositiveEvent, false);
            homeAddressMessageShown = true;
        }

        public string HomeAddress => EnsureHomeAddress();
        public int HomeTile => homeTile;
        public int HomeMapUniqueId => homeMapUniqueId;
        public int PlanetCount => planets.Count;
        public IEnumerable<string> RecentAddresses => recentAddresses.Where(address => IsValidAddress(address) && !IsHomeAddress(address));
        public IEnumerable<StarGatePlanetRecord> Planets => planets;
        public IEnumerable<StarGatePlanetRecord> KnownPlanets => planets.Where(planet => planet != null && planet.IsKnownToPlayer);

        public string GenerateUnknownAddress()
        {
            for (int i = 0; i < 200; i++)
            {
                string address = GenerateAddress(Rand.RangeInclusive(100000, int.MaxValue - 1) ^ Find.TickManager.TicksGame ^ i);
                if (!IsHomeAddress(address) && PlanetForAddress(address) == null)
                {
                    return address;
                }
            }

            return GenerateAddress(StableSeed("fallback-unknown-" + Find.TickManager.TicksGame));
        }

        public Map HomeMap()
        {
            EnsureHomeMapRecord();
            if (homeMapUniqueId >= 0)
            {
                Map savedHome = Current.Game?.Maps?.FirstOrDefault(map => map != null && map.uniqueID == homeMapUniqueId);
                if (savedHome != null)
                {
                    return savedHome;
                }
            }

            return Current.Game?.Maps?.FirstOrDefault(map => map != null && map.IsPlayerHome);
        }

        public string EnsureHomeAddress()
        {
            if (!homeAddress.NullOrEmpty())
            {
                return homeAddress;
            }

            homeAddress = GenerateAddress(StableSeed("home-" + Find.World?.info?.seedString));
            return homeAddress;
        }

        public void EnsureHomeMapRecord()
        {
            Map homeMap = Current.Game?.Maps?.FirstOrDefault(map => map != null && map.IsPlayerHome);
            if (homeMap == null)
            {
                return;
            }

            homeMapUniqueId = homeMap.uniqueID;
            if (homeMap.Tile >= 0)
            {
                homeTile = homeMap.Tile;
            }
        }

        public StarGatePlanetRecord EnsurePlanetForAddress(string address)
        {
            if (!IsValidAddress(address) || IsHomeAddress(address))
            {
                return null;
            }

            StarGatePlanetRecord existing = planets.FirstOrDefault(planet => planet.address == address);
            if (existing != null)
            {
                existing.EnsureGenerated();
                return existing;
            }

            int generationSeed = StableSeed(address);
            StarGatePlanetProfile profile = StarGatePlanetProfile.ForSeed(generationSeed);
            StarGatePlanetRecord record = new StarGatePlanetRecord
            {
                id = "planet_" + address.Replace("-", string.Empty),
                displayName = PlanetNameFor(address),
                address = address,
                seed = "StarGate-" + address,
                generationSeed = generationSeed,
                planetCoverage = 0.3f,
                rainfall = OverallRainfall.Normal,
                temperature = OverallTemperature.Normal,
                population = OverallPopulation.Little,
                landmarkDensity = LandmarkDensity.SlightlySparse,
                pollution = 0f,
                planetType = PlanetTypeForSeed(generationSeed),
                atmosphere = profile.atmosphere,
                civilizationLevel = profile.civilizationLevel,
                threatLevel = profile.threatLevel,
                resourceRichness = profile.resourceRichness
            };

            record.EnsureGenerated();
            planets.Add(record);
            return record;
        }

        public StarGatePlanetRecord PlanetForAddress(string address)
        {
            if (!IsValidAddress(address))
            {
                return null;
            }

            return planets.FirstOrDefault(planet => planet.address == address);
        }

        public StarGatePlanetRecord PlanetForMap(Map map)
        {
            if (map == null)
            {
                return null;
            }

            StarGatePlanetRecord record = planets.FirstOrDefault(planet => planet.HasMap(map.uniqueID));
            if (record != null)
            {
                return record;
            }

            StarGatePlanetMapParent parent = map.Parent as StarGatePlanetMapParent;
            if (parent == null || !IsValidAddress(parent.address))
            {
                return null;
            }

            record = EnsurePlanetForAddress(parent.address);
            StarGateSiteRecord site = SiteForId(record, parent.siteId) ?? record.PrimaryGateSite();
            RegisterGeneratedMap(record, site, map, parent);
            return record;
        }

        public StarGateSiteRecord SiteForMap(Map map)
        {
            if (map == null)
            {
                return null;
            }

            StarGatePlanetMapParent pocketParent = map.Parent as StarGatePlanetMapParent;
            if (pocketParent != null)
            {
                StarGatePlanetRecord pocketPlanet = PlanetForMap(map);
                StarGateSiteRecord pocketSite = SiteForId(pocketPlanet, pocketParent.siteId);
                if (pocketSite != null)
                {
                    return pocketSite;
                }
            }

            foreach (StarGatePlanetRecord planet in planets)
            {
                StarGateSiteRecord site = planet.SiteForMap(map.uniqueID);
                if (site != null)
                {
                    return site;
                }
            }

            return null;
        }

        public StarGateSiteRecord SiteForId(StarGatePlanetRecord planet, string siteId)
        {
            if (planet == null || siteId.NullOrEmpty())
            {
                return null;
            }

            planet.EnsureGenerated();
            return planet.sites?.FirstOrDefault(site => site != null && site.id == siteId);
        }

        public bool IsHomeAddress(string address)
        {
            return !address.NullOrEmpty() && address == HomeAddress;
        }

        public void RegisterUsedAddress(string address)
        {
            if (!IsValidAddress(address) || IsHomeAddress(address))
            {
                return;
            }

            recentAddresses.Remove(address);
            recentAddresses.Insert(0, address);
            while (recentAddresses.Count > RecentAddressLimit)
            {
                recentAddresses.RemoveAt(recentAddresses.Count - 1);
            }
        }

        public void RegisterGeneratedMap(StarGatePlanetRecord planet, StarGateSiteRecord site, Map map, MapParent parent)
        {
            if (planet == null || site == null || map == null)
            {
                return;
            }

            site.mapUniqueId = map.uniqueID;
            site.mapState = "generated";
            site.generationVersion = CurrentGenerationVersion;
            if (parent != null)
            {
                site.worldObjectId = parent.ID;
                site.tile = parent.Tile;
                if (parent.Tile.Layer is StarGatePlanetLayer layer)
                {
                    site.planetLayerId = layer.LayerID;
                    site.planetTileId = parent.Tile.tileId;
                    planet.planetLayerId = layer.LayerID;
                    if (site.siteType == "primary_gate")
                    {
                        planet.primaryLayerTileId = parent.Tile.tileId;
                    }
                }
            }

            if (site.siteType == "primary_gate" || planet.mapUniqueId < 0)
            {
                planet.mapUniqueId = map.uniqueID;
                planet.tile = site.tile;
                planet.worldObjectId = site.worldObjectId;
            }
        }

        public void ReconcileLoadedMaps()
        {
            if (Current.Game?.Maps == null)
            {
                return;
            }

            foreach (Map map in Current.Game.Maps.Where(candidate => candidate != null))
            {
                StarGatePlanetMapParent parent = map.Parent as StarGatePlanetMapParent;
                if (parent == null || !IsValidAddress(parent.address))
                {
                    continue;
                }

                StarGatePlanetRecord planet = EnsurePlanetForAddress(parent.address);
                StarGateSiteRecord site = SiteForId(planet, parent.siteId) ?? planet.PrimaryGateSite();
                RegisterGeneratedMap(planet, site, map, parent);
            }
        }

        public void MarkPlanetDiscovered(StarGatePlanetRecord planet)
        {
            if (planet == null)
            {
                return;
            }

            planet.discovered = true;
        }

        public void RegisterVisit(Map map)
        {
            StarGatePlanetRecord planet = PlanetForMap(map);
            StarGateSiteRecord site = SiteForMap(map);
            if (planet == null || site == null)
            {
                return;
            }

            int ticks = Find.TickManager?.TicksGame ?? 0;
            planet.discovered = true;
            planet.visitCount++;
            planet.lastVisitTick = ticks;
            site.known = true;
            site.visited = true;
            site.visitCount++;
            site.lastVisitTick = ticks;
        }

        public StarGateSiteRecord RevealNextSiteOnMap(Map map)
        {
            StarGatePlanetRecord planet = PlanetForMap(map);
            if (planet == null)
            {
                return null;
            }

            return RevealNextSite(planet);
        }

        public StarGateSiteRecord RevealNextSite(StarGatePlanetRecord planet)
        {
            if (planet == null)
            {
                return null;
            }

            planet.EnsureGenerated();
            StarGateSiteRecord site = planet.sites
                .Where(candidate => candidate != null && !candidate.known && !candidate.visited)
                .OrderBy(candidate => candidate.seed)
                .FirstOrDefault();

            if (site == null)
            {
                return null;
            }

            site.known = true;
            if (site.threatLevel <= 0)
            {
                site.threatLevel = planet.threatLevel;
            }

            planet.scanCount++;
            planet.lastScanTick = Find.TickManager?.TicksGame ?? 0;
            return site;
        }

        private static string GenerateAddress(int seed)
        {
            List<string> symbols = new List<string>();
            string[] rings = { "O", "M", "I" };
            Rand.PushState(seed);
            try
            {
                for (int i = 0; i < 7; i++)
                {
                    symbols.Add(rings[Rand.Range(0, rings.Length)] + Rand.RangeInclusive(1, 20).ToString("00"));
                }
            }
            finally
            {
                Rand.PopState();
            }

            return string.Join("-", symbols.ToArray());
        }

        public static bool IsValidAddress(string address)
        {
            if (address.NullOrEmpty())
            {
                return false;
            }

            string[] parts = address.Split('-');
            if (parts.Length != 7)
            {
                return false;
            }

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (part.Length != 3)
                {
                    return false;
                }

                string prefix = part.Substring(0, 1);
                if (prefix != "O" && prefix != "M" && prefix != "I")
                {
                    return false;
                }

                if (!int.TryParse(part.Substring(1), out int symbol) || symbol < 1 || symbol > 20)
                {
                    return false;
                }
            }

            return true;
        }

        private void PurgeLegacyPrototypePlanet()
        {
            if (planets == null)
            {
                planets = new List<StarGatePlanetRecord>();
            }

            const string legacyAddress = "O01-M02-I03-O04-M05-I06-O07";
            planets.RemoveAll(planet => planet != null && (planet.id == "primary_offworld" || planet.address == legacyAddress));
            recentAddresses?.RemoveAll(address => address == legacyAddress || !IsValidAddress(address));
        }

        private static string PlanetNameFor(string address)
        {
            string compact = address.Replace("-", string.Empty);
            if (compact.Length > 6)
            {
                compact = compact.Substring(0, 6);
            }

            return "P-" + compact;
        }

        public static int StableSeed(string text)
        {
            unchecked
            {
                int hash = (int)2166136261;
                if (text != null)
                {
                    for (int i = 0; i < text.Length; i++)
                    {
                        hash ^= text[i];
                        hash *= 16777619;
                    }
                }

                return hash == int.MinValue ? 42 : System.Math.Abs(hash);
            }
        }

        public static string PlanetTypeForSeed(int seed)
        {
            string[] types = { "temperate", "desert", "ice", "forest", "toxic", "ancient_ruins" };
            return types[System.Math.Abs(seed) % types.Length];
        }
    }

    public class StarGatePlanetRecord : IExposable
    {
        public int dataVersion = StarGatePlanetSystem.CurrentDataVersion;
        public int generationVersion = StarGatePlanetSystem.CurrentGenerationVersion;
        public string id;
        public string displayName;
        public string address;
        public string seed;
        public int generationSeed;
        public int tile = -1;
        public int worldObjectId = -1;
        public int mapUniqueId = -1;
        public int planetLayerId = -1;
        public int primaryLayerTileId = -1;
        public float planetCoverage;
        public OverallRainfall rainfall;
        public OverallTemperature temperature;
        public OverallPopulation population;
        public LandmarkDensity landmarkDensity;
        public float pollution;
        public string planetType;
        public string atmosphere;
        public string civilizationLevel;
        public int threatLevel;
        public int resourceRichness;
        public bool generated;
        public bool discovered;
        public int visitCount;
        public int lastVisitTick = -1;
        public int scanCount;
        public int lastScanTick = -1;
        public List<StarGateSiteRecord> sites = new List<StarGateSiteRecord>();

        public bool IsKnownToPlayer => discovered || visitCount > 0 || (sites != null && sites.Any(site => site != null && site.visited));

        public void ExposeData()
        {
            Scribe_Values.Look(ref dataVersion, "dataVersion", StarGatePlanetSystem.CurrentDataVersion);
            Scribe_Values.Look(ref generationVersion, "generationVersion", StarGatePlanetSystem.CurrentGenerationVersion);
            Scribe_Values.Look(ref id, "id");
            Scribe_Values.Look(ref displayName, "displayName");
            Scribe_Values.Look(ref address, "address");
            Scribe_Values.Look(ref seed, "seed");
            Scribe_Values.Look(ref generationSeed, "generationSeed", 0);
            Scribe_Values.Look(ref tile, "tile", -1);
            Scribe_Values.Look(ref worldObjectId, "worldObjectId", -1);
            Scribe_Values.Look(ref mapUniqueId, "mapUniqueId", -1);
            Scribe_Values.Look(ref planetLayerId, "planetLayerId", -1);
            Scribe_Values.Look(ref primaryLayerTileId, "primaryLayerTileId", -1);
            Scribe_Values.Look(ref planetCoverage, "planetCoverage", 0.3f);
            Scribe_Values.Look(ref rainfall, "rainfall", OverallRainfall.Normal);
            Scribe_Values.Look(ref temperature, "temperature", OverallTemperature.Normal);
            Scribe_Values.Look(ref population, "population", OverallPopulation.Little);
            Scribe_Values.Look(ref landmarkDensity, "landmarkDensity", LandmarkDensity.SlightlySparse);
            Scribe_Values.Look(ref pollution, "pollution", 0f);
            Scribe_Values.Look(ref planetType, "planetType");
            Scribe_Values.Look(ref atmosphere, "atmosphere");
            Scribe_Values.Look(ref civilizationLevel, "civilizationLevel");
            Scribe_Values.Look(ref threatLevel, "threatLevel", 0);
            Scribe_Values.Look(ref resourceRichness, "resourceRichness", 0);
            Scribe_Values.Look(ref generated, "generated", false);
            Scribe_Values.Look(ref discovered, "discovered", false);
            Scribe_Values.Look(ref visitCount, "visitCount", 0);
            Scribe_Values.Look(ref lastVisitTick, "lastVisitTick", -1);
            Scribe_Values.Look(ref scanCount, "scanCount", 0);
            Scribe_Values.Look(ref lastScanTick, "lastScanTick", -1);
            Scribe_Collections.Look(ref sites, "sites", LookMode.Deep);
        }

        public void PostLoadNormalize()
        {
            if (sites == null)
            {
                sites = new List<StarGateSiteRecord>();
            }

            if (generationSeed == 0 && !address.NullOrEmpty())
            {
                generationSeed = StarGatePlanetSystem.StableSeed(address);
            }

            if (planetType.NullOrEmpty())
            {
                planetType = StarGatePlanetSystem.PlanetTypeForSeed(generationSeed == 0 ? StarGatePlanetSystem.StableSeed(address ?? id ?? "stargate-planet") : generationSeed);
            }

            EnsureProfile();

            foreach (StarGateSiteRecord site in sites.Where(candidate => candidate != null))
            {
                site.PostLoadNormalize(this);
            }

            if (sites.Count == 0 && mapUniqueId >= 0)
            {
                StarGateSiteRecord site = CreatePrimaryGateSite();
                site.mapUniqueId = mapUniqueId;
                site.tile = tile;
                site.worldObjectId = worldObjectId;
                sites.Add(site);
            }

            EnsureGenerated();
            dataVersion = StarGatePlanetSystem.CurrentDataVersion;
        }

        public void EnsureGenerated()
        {
            if (sites == null)
            {
                sites = new List<StarGateSiteRecord>();
            }

            if (generationSeed == 0)
            {
                generationSeed = StarGatePlanetSystem.StableSeed(address ?? id ?? "stargate-planet");
            }

            if (planetType.NullOrEmpty())
            {
                planetType = StarGatePlanetSystem.PlanetTypeForSeed(generationSeed);
            }

            EnsureProfile();

            if (sites.Count == 0)
            {
                GenerateSites();
            }

            foreach (StarGateSiteRecord site in sites.Where(candidate => candidate != null))
            {
                site.PostLoadNormalize(this);
            }

            generated = true;
        }

        public StarGateSiteRecord PrimaryGateSite()
        {
            EnsureGenerated();
            StarGateSiteRecord primary = sites.FirstOrDefault(site => site.siteType == "primary_gate");
            if (primary != null)
            {
                return primary;
            }

            primary = CreatePrimaryGateSite();
            sites.Insert(0, primary);
            return primary;
        }

        public bool HasMap(int mapId)
        {
            return sites != null && sites.Any(site => site.mapUniqueId == mapId);
        }

        public StarGateSiteRecord SiteForMap(int mapId)
        {
            return sites?.FirstOrDefault(site => site.mapUniqueId == mapId);
        }

        private void GenerateSites()
        {
            sites.Add(CreatePrimaryGateSite());

            Rand.PushState(generationSeed);
            try
            {
                int settlementCount = Rand.RangeInclusive(3, 7);
                for (int i = 0; i < settlementCount; i++)
                {
                    sites.Add(new StarGateSiteRecord
                    {
                        id = id + "_settlement_" + i,
                        displayName = displayName + " settlement " + (i + 1),
                        siteType = IsJaffaControlled && i % 3 == 0 ? "jaffa_outpost" : (i % 3 == 0 ? "ruin" : "settlement"),
                        seed = generationSeed + 101 + i * 37,
                        factionTag = IsJaffaControlled ? (i % 3 == 0 ? "jaffa" : "native") : (i % 2 == 0 ? "local" : "hostile"),
                        contentKind = IsJaffaControlled ? (i % 3 == 0 ? "outpost" : "settlement") : (i % 3 == 0 ? "ancient_ruins" : (i % 2 == 0 ? "settlement" : "outpost")),
                        mapSize = 150,
                        mapState = "uncreated",
                        generationVersion = StarGatePlanetSystem.CurrentGenerationVersion,
                        threatLevel = System.Math.Min(10, threatLevel + Rand.RangeInclusive(0, 3)),
                        known = false
                    });
                }
            }
            finally
            {
                Rand.PopState();
            }
        }

        private StarGateSiteRecord CreatePrimaryGateSite()
        {
            return new StarGateSiteRecord
            {
                id = id + "_primary_gate",
                displayName = displayName + " gate site",
                siteType = "primary_gate",
                address = address,
                seed = generationSeed == 0 ? StarGatePlanetSystem.StableSeed(address ?? id) : generationSeed,
                factionTag = "ancient",
                contentKind = PrimaryContentKind(),
                tile = tile,
                worldObjectId = worldObjectId,
                mapUniqueId = mapUniqueId,
                mapSize = 150,
                mapState = mapUniqueId >= 0 ? "generated" : "uncreated",
                generationVersion = StarGatePlanetSystem.CurrentGenerationVersion,
                known = true,
                threatLevel = threatLevel
            };
        }

        private void EnsureProfile()
        {
            if (!atmosphere.NullOrEmpty() && !civilizationLevel.NullOrEmpty() && threatLevel > 0 && resourceRichness > 0)
            {
                return;
            }

            StarGatePlanetProfile profile = StarGatePlanetProfile.ForSeed(generationSeed == 0 ? StarGatePlanetSystem.StableSeed(address ?? id ?? "stargate-planet") : generationSeed);
            if (atmosphere.NullOrEmpty())
            {
                atmosphere = profile.atmosphere;
            }

            if (civilizationLevel.NullOrEmpty())
            {
                civilizationLevel = profile.civilizationLevel;
            }

            if (threatLevel <= 0)
            {
                threatLevel = profile.threatLevel;
            }

            if (resourceRichness <= 0)
            {
                resourceRichness = profile.resourceRichness;
            }
        }

        private string PrimaryContentKind()
        {
            switch (civilizationLevel)
            {
                case "Scattered tribes":
                case "Hidden settlements":
                    return "settlement";
                case "Hostile outposts":
                case "Jaffa-controlled world":
                    return "outpost";
                case "Ruins":
                case "Ancient network":
                    return "ancient_ruins";
                default:
                    return "wilderness";
            }
        }

        public bool IsJaffaControlled => civilizationLevel == "Jaffa-controlled world";
    }

    public class StarGateSiteRecord : IExposable
    {
        public string id;
        public string displayName;
        public string siteType;
        public string address;
        public int seed;
        public string factionTag;
        public string contentKind;
        public string factionDefName;
        public int factionLoadId = -1;
        public string mapState = "uncreated";
        public int generationVersion = StarGatePlanetSystem.CurrentGenerationVersion;
        public int tile = -1;
        public int worldObjectId = -1;
        public int mapUniqueId = -1;
        public int planetLayerId = -1;
        public int planetTileId = -1;
        public int mapSize = 150;
        public bool contentGenerated;
        public bool visited;
        public bool known;
        public int threatLevel;
        public int visitCount;
        public int lastVisitTick = -1;

        public void ExposeData()
        {
            Scribe_Values.Look(ref id, "id");
            Scribe_Values.Look(ref displayName, "displayName");
            Scribe_Values.Look(ref siteType, "siteType");
            Scribe_Values.Look(ref address, "address");
            Scribe_Values.Look(ref seed, "seed", 0);
            Scribe_Values.Look(ref factionTag, "factionTag");
            Scribe_Values.Look(ref contentKind, "contentKind");
            Scribe_Values.Look(ref factionDefName, "factionDefName");
            Scribe_Values.Look(ref factionLoadId, "factionLoadId", -1);
            Scribe_Values.Look(ref mapState, "mapState", "uncreated");
            Scribe_Values.Look(ref generationVersion, "generationVersion", StarGatePlanetSystem.CurrentGenerationVersion);
            Scribe_Values.Look(ref tile, "tile", -1);
            Scribe_Values.Look(ref worldObjectId, "worldObjectId", -1);
            Scribe_Values.Look(ref mapUniqueId, "mapUniqueId", -1);
            Scribe_Values.Look(ref planetLayerId, "planetLayerId", -1);
            Scribe_Values.Look(ref planetTileId, "planetTileId", -1);
            Scribe_Values.Look(ref mapSize, "mapSize", 150);
            Scribe_Values.Look(ref contentGenerated, "contentGenerated", false);
            Scribe_Values.Look(ref visited, "visited", false);
            Scribe_Values.Look(ref known, "known", false);
            Scribe_Values.Look(ref threatLevel, "threatLevel", 0);
            Scribe_Values.Look(ref visitCount, "visitCount", 0);
            Scribe_Values.Look(ref lastVisitTick, "lastVisitTick", -1);
        }

        public void PostLoadNormalize(StarGatePlanetRecord planet)
        {
            if (mapSize <= 0)
            {
                mapSize = StarGatePlanetMapFactory.MapSize;
            }

            if (contentKind.NullOrEmpty())
            {
                if (siteType == "ruin")
                {
                    contentKind = "ancient_ruins";
                }
                else if (siteType == "settlement")
                {
                    contentKind = factionTag == "hostile" ? "outpost" : "settlement";
                }
                else
                {
                    switch (planet?.civilizationLevel)
                    {
                        case "Scattered tribes":
                        case "Hidden settlements":
                            contentKind = "settlement";
                            break;
                        case "Hostile outposts":
                            contentKind = "outpost";
                            break;
                        case "Ruins":
                        case "Ancient network":
                            contentKind = "ancient_ruins";
                            break;
                        default:
                            contentKind = "wilderness";
                            break;
                    }
                }
            }

            mapState = mapUniqueId >= 0 ? "generated" : "uncreated";
            generationVersion = generationVersion <= 0
                ? StarGatePlanetSystem.CurrentGenerationVersion
                : generationVersion;
        }
    }

    public struct StarGatePlanetProfile
    {
        public string atmosphere;
        public string civilizationLevel;
        public int threatLevel;
        public int resourceRichness;

        public static StarGatePlanetProfile ForSeed(int seed)
        {
            string[] atmospheres = { "Breathable", "Thin", "Humid", "Dust-heavy", "Irradiated", "Toxic traces" };
            string[] civilizations =
            {
                "Jaffa-controlled world", "Jaffa-controlled world", "Jaffa-controlled world", "Jaffa-controlled world",
                "Uninhabited", "Ruins", "Scattered tribes", "Hidden settlements", "Hostile outposts", "Ancient network"
            };

            Rand.PushState(seed ^ 0x51A7);
            try
            {
                return new StarGatePlanetProfile
                {
                    atmosphere = atmospheres[Rand.Range(0, atmospheres.Length)],
                    civilizationLevel = civilizations[Rand.Range(0, civilizations.Length)],
                    threatLevel = Rand.RangeInclusive(1, 10),
                    resourceRichness = Rand.RangeInclusive(1, 10)
                };
            }
            finally
            {
                Rand.PopState();
            }
        }
    }
}
