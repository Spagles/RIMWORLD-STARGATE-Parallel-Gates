using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimGateJaffaKree
{
    public class Dialog_StarGateDebugDatabase : Window
    {
        private Vector2 scrollPosition;

        public override Vector2 InitialSize => new Vector2(900f, 620f);

        public Dialog_StarGateDebugDatabase()
        {
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
            doCloseX = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            StarGatePlanetSystem planetSystem = Current.Game.GetComponent<StarGatePlanetSystem>();
            planetSystem?.EnsureInitialized();

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 34f), "StarGate database");
            Text.Font = GameFont.Small;

            if (planetSystem == null)
            {
                Widgets.Label(new Rect(inRect.x, inRect.y + 46f, inRect.width, 28f), "StarGate database is not available.");
                return;
            }

            Rect scrollOut = new Rect(inRect.x, inRect.y + 44f, inRect.width, inRect.height - 44f);
            List<StarGatePlanetRecord> planets = planetSystem.Planets.ToList();
            float viewHeight = 150f + planets.Sum(planet => 80f + ((planet.sites == null ? 0 : planet.sites.Count) * 26f));
            Rect scrollView = new Rect(0f, 0f, scrollOut.width - 20f, Mathf.Max(viewHeight, scrollOut.height));

            Widgets.BeginScrollView(scrollOut, ref scrollPosition, scrollView);
            float y = 0f;

            DrawLine(ref y, "Home address", planetSystem.HomeAddress);
            DrawLine(ref y, "Home map", "mapId=" + planetSystem.HomeMapUniqueId + " tile=" + planetSystem.HomeTile);
            DrawLine(ref y, "Known planets", planets.Count.ToString());
            y += 10f;

            foreach (StarGatePlanetRecord planet in planets)
            {
                DrawPlanet(ref y, planet);
            }

            Widgets.EndScrollView();
        }

        private static void DrawPlanet(ref float y, StarGatePlanetRecord planet)
        {
            Rect headerRect = new Rect(0f, y, 840f, 30f);
            Widgets.DrawBox(headerRect);
            Widgets.Label(new Rect(headerRect.x + 8f, headerRect.y + 6f, headerRect.width - 16f, 22f),
                LabelOrFallback(planet.displayName, planet.id) + " | seed=" + planet.generationSeed + " | " + LabelOrFallback(planet.planetType, "unknown") + " | " + LabelOrFallback(planet.atmosphere, "unknown") + " | civ=" + LabelOrFallback(planet.civilizationLevel, "unknown") + " | T" + planet.threatLevel + " R" + planet.resourceRichness + " | discovered=" + planet.discovered + " | visits=" + planet.visitCount + " | scans=" + planet.scanCount + " | last=" + planet.lastVisitTick + " | " + planet.address);
            y += 34f;

            DrawLine(ref y, "Main map", "mapId=" + planet.mapUniqueId + " worldObjectId=" + planet.worldObjectId + " tile=" + planet.tile);

            if (planet.sites == null || planet.sites.Count == 0)
            {
                DrawLine(ref y, "Sites", "none");
                y += 8f;
                return;
            }

            for (int i = 0; i < planet.sites.Count; i++)
            {
                StarGateSiteRecord site = planet.sites[i];
                DrawLine(ref y, "Site " + (i + 1), LabelOrFallback(site.displayName, site.id) + " | " + site.siteType + " | known=" + site.known + " | T" + site.threatLevel + " | visited=" + site.visited + " | visits=" + site.visitCount + " | content=" + site.contentGenerated + " | mapId=" + site.mapUniqueId + " worldObjectId=" + site.worldObjectId + " tile=" + site.tile);
            }

            y += 10f;
        }

        private static void DrawLine(ref float y, string label, string value)
        {
            Rect rect = new Rect(0f, y, 840f, 24f);
            Widgets.Label(new Rect(rect.x, rect.y, 150f, rect.height), label);
            Widgets.Label(new Rect(rect.x + 160f, rect.y, rect.width - 160f, rect.height), value ?? "-");
            y += 26f;
        }

        private static string LabelOrFallback(string value, string fallback)
        {
            return value.NullOrEmpty() ? fallback : value;
        }
    }
}
