using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimGateJaffaKree
{
    public class Dialog_StarGateAddressBook : Window
    {
        private readonly CompStarGateControlPanel panel;
        private Vector2 recentScroll;
        private Vector2 planetScroll;

        public override Vector2 InitialSize => new Vector2(980f, 640f);

        public Dialog_StarGateAddressBook(CompStarGateControlPanel panel)
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
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 34f), "StarGate address book");

            Text.Font = GameFont.Small;
            float y = inRect.y + 46f;
            if (planetSystem == null)
            {
                Widgets.Label(new Rect(inRect.x, y, inRect.width, 28f), "Address database is not available.");
                return;
            }

            DrawCurrentLocation(new Rect(inRect.x, y, inRect.width, 64f), planetSystem);
            y += 74f;

            Rect topActionRect = new Rect(inRect.x, y, inRect.width, 44f);
            Widgets.DrawMenuSection(topActionRect);
            Widgets.Label(new Rect(topActionRect.x + 12f, topActionRect.y + 11f, 300f, 24f), "Navigation");
            if (Widgets.ButtonText(new Rect(topActionRect.xMax - 332f, topActionRect.y + 8f, 156f, 28f), "Galaxy"))
            {
                Find.WindowStack.Add(new Dialog_StarGateGalaxyMap(panel));
            }

            if (Widgets.ButtonText(new Rect(topActionRect.xMax - 168f, topActionRect.y + 8f, 156f, 28f), "New address"))
            {
                SelectAddress(planetSystem.GenerateUnknownAddress());
            }

            y += 56f;

            float gap = 12f;
            float leftWidth = (inRect.width - gap) * 0.42f;
            Rect leftRect = new Rect(inRect.x, y, leftWidth, inRect.yMax - y);
            Rect rightRect = new Rect(leftRect.xMax + gap, y, inRect.xMax - leftRect.xMax - gap, inRect.yMax - y);

            DrawLeftColumn(leftRect, planetSystem);
            DrawKnownPlanets(rightRect, planetSystem);
        }

        private void DrawCurrentLocation(Rect rect, StarGatePlanetSystem planetSystem)
        {
            Widgets.DrawMenuSection(rect);
            Map map = panel?.parent?.Map;
            StarGatePlanetRecord planet = planetSystem.PlanetForMap(map);
            StarGateSiteRecord site = planetSystem.SiteForMap(map);
            string location = planet == null ? "Home planet" : (planet.displayName + "   " + planet.planetType);
            string addressText = planet == null ? planetSystem.HomeAddress : planet.address;
            string siteText = site == null ? "Primary gate" : site.displayName;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 28f), location);
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(rect.x + 12f, rect.y + 36f, rect.width - 24f, 22f), "Address: " + addressText + "   Site: " + siteText);
        }

        private void DrawLeftColumn(Rect rect, StarGatePlanetSystem planetSystem)
        {
            Widgets.DrawMenuSection(rect);
            float y = rect.y + 10f;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x + 10f, y, rect.width - 20f, 28f), "Home");
            Text.Font = GameFont.Small;
            y += 34f;
            DrawCompactAddressRow(new Rect(rect.x + 10f, y, rect.width - 20f, 54f), "Home planet", planetSystem.HomeAddress, "Guide", planetSystem.HomeAddress);
            y += 64f;

            Widgets.Label(new Rect(rect.x + 10f, y, rect.width - 20f, 24f), "Recent");
            y += 28f;

            Rect scrollOut = new Rect(rect.x + 10f, y, rect.width - 20f, rect.height - (y - rect.y) - 10f);
            List<string> recent = planetSystem.RecentAddresses.ToList();
            if (recent.Count == 0)
            {
                Widgets.Label(new Rect(scrollOut.x, scrollOut.y, scrollOut.width, 28f), "No recent addresses yet.");
                return;
            }

            Rect view = new Rect(0f, 0f, scrollOut.width - 16f, recent.Count * 62f);
            Widgets.BeginScrollView(scrollOut, ref recentScroll, view);
            float rowY = 0f;
            for (int i = 0; i < recent.Count; i++)
            {
                DrawCompactAddressRow(new Rect(0f, rowY, view.width, 54f), "Recent " + (i + 1), recent[i], "Guide", recent[i]);
                rowY += 60f;
            }
            Widgets.EndScrollView();
        }

        private void DrawKnownPlanets(Rect rect, StarGatePlanetSystem planetSystem)
        {
            Widgets.DrawMenuSection(rect);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x + 10f, rect.y + 10f, rect.width - 20f, 28f), "Known planets");
            Text.Font = GameFont.Small;

            List<StarGatePlanetRecord> knownPlanets = planetSystem.KnownPlanets
                .OrderByDescending(planet => planet.visitCount)
                .ThenBy(planet => planet.displayName)
                .ToList();

            Rect scrollOut = new Rect(rect.x + 10f, rect.y + 44f, rect.width - 20f, rect.height - 54f);
            if (knownPlanets.Count == 0)
            {
                Widgets.Label(new Rect(scrollOut.x, scrollOut.y, scrollOut.width, 28f), "No discovered planets yet.");
                return;
            }

            Rect view = new Rect(0f, 0f, scrollOut.width - 16f, knownPlanets.Count * 82f);
            Widgets.BeginScrollView(scrollOut, ref planetScroll, view);
            float rowY = 0f;
            foreach (StarGatePlanetRecord planet in knownPlanets)
            {
                DrawPlanetCard(new Rect(0f, rowY, view.width, 74f), planet);
                rowY += 80f;
            }
            Widgets.EndScrollView();
        }

        private void DrawCompactAddressRow(Rect rect, string title, string address, string buttonLabel, string selectedAddress)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.18f, 0.18f, 0.18f, 0.95f));
            Widgets.DrawHighlightIfMouseover(rect);
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(rect.x + 8f, rect.y + 6f, rect.width - 108f, 20f), title);
            GUI.color = Color.gray;
            Widgets.Label(new Rect(rect.x + 8f, rect.y + 28f, rect.width - 108f, 20f), address);
            GUI.color = Color.white;
            if (Widgets.ButtonText(new Rect(rect.xMax - 92f, rect.y + 13f, 84f, 28f), buttonLabel))
            {
                SelectAddress(selectedAddress);
            }
        }

        private void DrawPlanetCard(Rect rect, StarGatePlanetRecord planet)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.16f, 0.16f, 0.16f, 0.96f));
            Widgets.DrawHighlightIfMouseover(rect);

            string title = planet.displayName.NullOrEmpty() ? "Planet" : planet.displayName;
            string line1 = planet.address;
            string line2 = planet.planetType + "   Visits: " + planet.visitCount + "   Sites: " + (planet.sites?.Count ?? 0);

            Widgets.Label(new Rect(rect.x + 8f, rect.y + 6f, rect.width - 104f, 20f), title);
            GUI.color = Color.gray;
            Widgets.Label(new Rect(rect.x + 8f, rect.y + 28f, rect.width - 104f, 20f), line1);
            GUI.color = new Color(0.82f, 0.82f, 0.82f);
            Widgets.Label(new Rect(rect.x + 8f, rect.y + 48f, rect.width - 104f, 20f), line2);
            GUI.color = Color.white;

            if (Widgets.ButtonText(new Rect(rect.xMax - 92f, rect.y + 22f, 84f, 28f), "Guide"))
            {
                SelectAddress(planet.address);
            }
        }

        private void SelectAddress(string address)
        {
            if (!StarGatePlanetSystem.IsValidAddress(address))
            {
                Messages.Message("StarGate adresa neni platna.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            CompStarGate gate = panel.LinkedGate();
            if (gate == null)
            {
                Messages.Message("Panel neni pripojen k brane.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            Close();
            Find.WindowStack.Add(new Dialog_StarGateDialPanel(panel, address));
        }
    }
}
