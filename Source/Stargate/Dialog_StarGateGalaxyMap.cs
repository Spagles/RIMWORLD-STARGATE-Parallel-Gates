using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimGateJaffaKree
{
    public class Dialog_StarGateGalaxyMap : Window
    {
        private readonly CompStarGateControlPanel panel;
        private Vector2 scrollPosition;
        private string selectedPlanetId;

        public override Vector2 InitialSize => new Vector2(980f, 620f);

        public Dialog_StarGateGalaxyMap(CompStarGateControlPanel panel)
        {
            this.panel = panel;
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
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 34f), "StarGate galaxy");
            Text.Font = GameFont.Small;

            if (planetSystem == null)
            {
                Widgets.Label(new Rect(inRect.x, inRect.y + 46f, inRect.width, 28f), "StarGate galaxy database is not available.");
                return;
            }

            float y = inRect.y + 46f;
            DrawCurrentLocation(new Rect(inRect.x, y, inRect.width, 64f), planetSystem);
            y += 74f;

            DrawHomeRow(new Rect(inRect.x, y, inRect.width, 44f), planetSystem.HomeAddress);
            y += 54f;

            Rect unknownRect = new Rect(inRect.x, y, inRect.width, 44f);
            Widgets.DrawMenuSection(unknownRect);
            Widgets.Label(new Rect(unknownRect.x + 12f, unknownRect.y + 11f, unknownRect.width - 180f, 24f), "Unknown destination");
            if (Widgets.ButtonText(new Rect(unknownRect.xMax - 164f, unknownRect.y + 8f, 156f, 28f), "New address"))
            {
                OpenDialGuide(planetSystem.GenerateUnknownAddress());
            }

            y += 52f;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 24f), "Discovered planets");
            y += 30f;

            List<StarGatePlanetRecord> planets = planetSystem.KnownPlanets
                .OrderByDescending(planet => planet.lastVisitTick)
                .ThenBy(planet => planet.displayName)
                .ToList();

            if (planets.Count == 0)
            {
                Widgets.Label(new Rect(inRect.x, y, inRect.width, 30f), "No discovered planets yet. Dial a new unknown address to discover the first one.");
                return;
            }

            Rect listRect = new Rect(inRect.x, y, inRect.width * 0.56f, inRect.yMax - y);
            Rect detailRect = new Rect(listRect.xMax + 12f, y, inRect.xMax - listRect.xMax - 12f, inRect.yMax - y);
            Widgets.DrawMenuSection(listRect);
            Widgets.DrawMenuSection(detailRect);

            Rect scrollOut = new Rect(listRect.x + 8f, listRect.y + 8f, listRect.width - 16f, listRect.height - 16f);
            Rect scrollView = new Rect(0f, 0f, scrollOut.width - 20f, planets.Count * 74f + 8f);
            Widgets.BeginScrollView(scrollOut, ref scrollPosition, scrollView);

            float rowY = 0f;
            foreach (StarGatePlanetRecord planet in planets)
            {
                DrawPlanetRow(new Rect(0f, rowY, scrollView.width, 68f), planet);
                rowY += 74f;
            }

            Widgets.EndScrollView();

            StarGatePlanetRecord selected = planets.FirstOrDefault(planet => planet.id == selectedPlanetId) ?? planets.FirstOrDefault();
            DrawPlanetDetails(detailRect, selected);
        }

        private void DrawCurrentLocation(Rect rect, StarGatePlanetSystem planetSystem)
        {
            Widgets.DrawMenuSection(rect);
            Map map = panel?.parent?.Map;
            StarGatePlanetRecord planet = planetSystem.PlanetForMap(map);
            StarGateSiteRecord site = planetSystem.SiteForMap(map);

            string location = planet == null
                ? "Current location: Home planet"
                : "Current location: " + LabelOrFallback(planet.displayName, planet.id) + " | " + LabelOrFallback(planet.planetType, "unknown");

            string address = planet == null
                ? "Address: " + planetSystem.HomeAddress
                : "Address: " + planet.address + " | Site: " + LabelOrFallback(site?.displayName, "primary gate site");

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 160f, 24f), location);
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(rect.x + 12f, rect.y + 36f, rect.width - 160f, 20f), address);

            if (planet != null && Widgets.ButtonText(new Rect(rect.xMax - 132f, rect.y + 18f, 124f, 28f), "Scan planet"))
            {
                StarGateSiteRecord revealed = planetSystem.RevealNextSiteOnMap(map);
                if (revealed == null)
                {
                    Messages.Message("StarGate scan nenasel zadne dalsi skryte signaly.", MessageTypeDefOf.NeutralEvent, false);
                    return;
                }

                selectedPlanetId = planet.id;
                string title = "StarGate site discovered";
                string text = "The StarGate scan revealed a new location on " + LabelOrFallback(planet.displayName, planet.id) + ".\n\n"
                    + "Site: " + LabelOrFallback(revealed.displayName, revealed.id) + "\n"
                    + "Type: " + LabelOrFallback(revealed.siteType, "unknown") + "\n"
                    + "Threat level: " + revealed.threatLevel + "/10";
                Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.PositiveEvent, panel.parent);
            }
        }

        private void DrawHomeRow(Rect rect, string homeAddress)
        {
            Widgets.DrawMenuSection(rect);
            Widgets.Label(new Rect(rect.x + 12f, rect.y + 11f, rect.width - 180f, 24f), "Home planet   " + homeAddress);
            if (Widgets.ButtonText(new Rect(rect.xMax - 164f, rect.y + 8f, 156f, 28f), "Guide home"))
            {
                OpenDialGuide(homeAddress);
            }
        }

        private void DrawPlanetRow(Rect rect, StarGatePlanetRecord planet)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.16f, 0.16f, 0.16f, 0.96f));
            Widgets.DrawHighlightIfMouseover(rect);

            string name = LabelOrFallback(planet.displayName, planet.id);
            string detail = LabelOrFallback(planet.planetType, "unknown") + "   Threat " + planet.threatLevel + "/10   Resources " + planet.resourceRichness + "/10";
            string address = planet.address + "   Visits: " + planet.visitCount;

            Widgets.Label(new Rect(rect.x + 8f, rect.y + 6f, rect.width - 180f, 20f), name);
            GUI.color = new Color(0.82f, 0.82f, 0.82f);
            Widgets.Label(new Rect(rect.x + 8f, rect.y + 26f, rect.width - 180f, 20f), detail);
            GUI.color = Color.gray;
            Widgets.Label(new Rect(rect.x + 8f, rect.y + 46f, rect.width - 180f, 18f), address);
            GUI.color = Color.white;

            if (Widgets.ButtonText(new Rect(rect.xMax - 328f, rect.y + 20f, 156f, 28f), "Details"))
            {
                selectedPlanetId = planet.id;
            }

            if (Widgets.ButtonText(new Rect(rect.xMax - 164f, rect.y + 20f, 156f, 28f), "Guide address"))
            {
                OpenDialGuide(planet.address);
            }
        }

        private void DrawPlanetDetails(Rect rect, StarGatePlanetRecord planet)
        {
            if (planet == null)
            {
                Widgets.Label(new Rect(rect.x + 12f, rect.y + 12f, rect.width - 24f, 24f), "No planet selected.");
                return;
            }

            float y = rect.y + 12f;
            string name = LabelOrFallback(planet.displayName, planet.id);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x + 12f, y, rect.width - 24f, 30f), name);
            Text.Font = GameFont.Small;
            y += 34f;

            DrawDetailLine(rect, ref y, "Address", planet.address);
            DrawDetailLine(rect, ref y, "Planet seed", planet.generationSeed.ToString());
            DrawDetailLine(rect, ref y, "Type", LabelOrFallback(planet.planetType, "unknown"));
            DrawDetailLine(rect, ref y, "Atmosphere", LabelOrFallback(planet.atmosphere, "unknown"));
            DrawDetailLine(rect, ref y, "Civilization", LabelOrFallback(planet.civilizationLevel, "unknown"));
            DrawDetailLine(rect, ref y, "Threat", planet.threatLevel + "/10");
            DrawDetailLine(rect, ref y, "Resources", planet.resourceRichness + "/10");
            DrawDetailLine(rect, ref y, "Visits", planet.visitCount.ToString());
            DrawDetailLine(rect, ref y, "Scans", planet.scanCount.ToString());

            y += 8f;
            Widgets.Label(new Rect(rect.x + 12f, y, rect.width - 24f, 24f), "Planet sites");
            y += 28f;

            if (planet.sites == null || planet.sites.Count == 0)
            {
                Widgets.Label(new Rect(rect.x + 12f, y, rect.width - 24f, 24f), "No site records.");
                return;
            }

            foreach (StarGateSiteRecord site in planet.sites.Take(8))
            {
                Rect row = new Rect(rect.x + 12f, y, rect.width - 24f, 52f);
                Widgets.DrawBoxSolid(row, new Color(0.17f, 0.17f, 0.17f, 0.96f));
                bool visible = site.known || site.visited;
                string siteName = visible ? LabelOrFallback(site.displayName, site.id) : "Unidentified signal";
                string siteState = visible ? LabelOrFallback(site.siteType, "unknown") : "scan required";
                string threat = visible ? "T" + site.threatLevel : "T?";
                Widgets.Label(new Rect(row.x + 8f, row.y + 6f, row.width - 104f, 20f), siteName);
                GUI.color = new Color(0.82f, 0.82f, 0.82f);
                Widgets.Label(new Rect(row.x + 8f, row.y + 26f, row.width - 104f, 20f), siteState + "   " + threat + "   Visits: " + site.visitCount);
                GUI.color = Color.white;
                if (visible && Widgets.ButtonText(new Rect(row.xMax - 94f, row.y + 13f, 86f, 26f), "Dial site"))
                {
                    OpenDialGuide(planet.address, site.id);
                }

                y += 58f;
            }
        }

        private static void DrawDetailLine(Rect parentRect, ref float y, string label, string value)
        {
            Widgets.Label(new Rect(parentRect.x + 8f, y, 104f, 22f), label);
            Widgets.Label(new Rect(parentRect.x + 118f, y, parentRect.width - 126f, 22f), value ?? "-");
            y += 24f;
        }

        private void OpenDialGuide(string address)
        {
            OpenDialGuide(address, null);
        }

        private void OpenDialGuide(string address, string siteId)
        {
            if (!StarGatePlanetSystem.IsValidAddress(address))
            {
                Messages.Message("StarGate adresa neni platna.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (panel?.LinkedGate() == null)
            {
                Messages.Message("Panel neni pripojen k brane.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            Close();
            Find.WindowStack.Add(siteId.NullOrEmpty()
                ? new Dialog_StarGateDialPanel(panel, address)
                : new Dialog_StarGateDialPanel(panel, address, siteId));
        }

        private static string LabelOrFallback(string value, string fallback)
        {
            return value.NullOrEmpty() ? fallback : value;
        }
    }
}
