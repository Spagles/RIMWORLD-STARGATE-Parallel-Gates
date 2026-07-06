using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimGateJaffaKree
{
    [StaticConstructorOnStartup]
    public class Dialog_StarGateDialPanel : Window
    {
        private const int SymbolsPerRing = 20;
        private const int AddressLength = 7;
        private static readonly Texture2D PanelTexture = ContentFinder<Texture2D>.Get("UI/Stargate/DialPanel");
        private static readonly Color SelectedSymbolColor = new Color(0.55f, 0.9f, 1f, 0.38f);
        private static readonly Color GuidedSymbolColor = new Color(1f, 0.85f, 0.18f, 0.28f);
        private static readonly Color GuidedNextSymbolColor = new Color(0.2f, 1f, 0.85f, 0.42f);
        private static readonly Color GuidedDoneSymbolColor = new Color(0.2f, 1f, 0.25f, 0.30f);

        private readonly CompStarGateControlPanel panel;
        private readonly List<string> guidedSymbols = new List<string>();
        private readonly List<string> selectedSymbols = new List<string>();
        private string guidedAddress;
        private string guidedSiteId;

        public override Vector2 InitialSize => new Vector2(900f, 700f);

        public Dialog_StarGateDialPanel(CompStarGateControlPanel panel)
        {
            this.panel = panel;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
            doCloseX = true;
        }

        public Dialog_StarGateDialPanel(CompStarGateControlPanel panel, string guidedAddress) : this(panel)
        {
            SetGuidedAddress(guidedAddress, false);
        }

        public Dialog_StarGateDialPanel(CompStarGateControlPanel panel, string guidedAddress, string guidedSiteId) : this(panel, guidedAddress)
        {
            this.guidedSiteId = guidedSiteId;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f), "StarGate dialing address");

            Text.Font = GameFont.Small;
            Rect addressRect = new Rect(inRect.x, inRect.y + 36f, inRect.width, 28f);
            Widgets.Label(addressRect, "Address: " + CurrentAddressText());

            StarGatePlanetSystem planetSystem = Current.Game.GetComponent<StarGatePlanetSystem>();
            string homeAddress = planetSystem?.HomeAddress ?? "(unknown)";
            Rect homeAddressRect = new Rect(inRect.x, inRect.y + 60f, inRect.width - 170f, 24f);
            Widgets.Label(homeAddressRect, GuidedAddressText(homeAddress));
            Rect homeButtonRect = new Rect(inRect.xMax - 160f, inRect.y + 56f, 150f, 30f);
            if (Widgets.ButtonText(homeButtonRect, "Domovska planeta"))
            {
                SetGuidedAddress(homeAddress, true);
            }

            float panelSize = Mathf.Min(inRect.width - 40f, inRect.height - 100f);
            Rect panelRect = new Rect(inRect.center.x - panelSize / 2f, inRect.y + 92f, panelSize, panelSize * 1696f / 2516f);
            Widgets.DrawTextureFitted(panelRect, PanelTexture, 1f);
            DrawGuidedSymbols(panelRect);
            DrawSelectedSymbols(panelRect);

            HandlePanelClick(panelRect);

            Rect buttonsRect = new Rect(inRect.x, inRect.yMax - 72f, inRect.width, 32f);
            DrawControlButtons(buttonsRect);

            Rect hintRect = new Rect(inRect.x, inRect.yMax - 34f, inRect.width, 30f);
            Widgets.Label(hintRect, "Click 7 symbols on any ring, then click the red crystal to activate.");
        }

        private string GuidedAddressText(string homeAddress)
        {
            if (guidedSymbols.Count == AddressLength)
            {
                return "Guided address: " + string.Join("-", guidedSymbols.ToArray());
            }

            return "Home address: " + homeAddress;
        }

        private void SetGuidedAddress(string address, bool clearCurrentInput)
        {
            guidedSymbols.Clear();
            guidedAddress = address;
            if (!address.NullOrEmpty())
            {
                guidedSymbols.AddRange(address.Split('-'));
            }

            if (clearCurrentInput)
            {
                selectedSymbols.Clear();
                guidedSiteId = null;
            }
        }

        private void DrawControlButtons(Rect rect)
        {
            float width = 110f;
            Rect backRect = new Rect(rect.x, rect.y, width, rect.height);
            Rect clearRect = new Rect(backRect.xMax + 8f, rect.y, width, rect.height);
            Rect cancelRect = new Rect(rect.xMax - width, rect.y, width, rect.height);

            if (Widgets.ButtonText(backRect, "Zpet") && selectedSymbols.Count > 0)
            {
                selectedSymbols.RemoveAt(selectedSymbols.Count - 1);
            }

            if (Widgets.ButtonText(clearRect, "Vymazat"))
            {
                selectedSymbols.Clear();
            }

            if (Widgets.ButtonText(cancelRect, "Zrusit"))
            {
                Close();
            }
        }

        private void DrawSelectedSymbols(Rect panelRect)
        {
            for (int i = 0; i < selectedSymbols.Count; i++)
            {
                if (!TrySymbolPosition(panelRect, selectedSymbols[i], out Vector2 center))
                {
                    continue;
                }

                Rect markerRect = new Rect(center.x - 15f, center.y - 15f, 30f, 30f);
                Widgets.DrawBoxSolid(markerRect, SelectedSymbolColor);
                Widgets.DrawBox(markerRect, 2);

                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Tiny;
                Widgets.Label(markerRect, (i + 1).ToString());
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
            }
        }

        private void DrawGuidedSymbols(Rect panelRect)
        {
            for (int i = 0; i < guidedSymbols.Count; i++)
            {
                if (!TrySymbolPosition(panelRect, guidedSymbols[i], out Vector2 center))
                {
                    continue;
                }

                Color color = GuidedSymbolColor;
                if (selectedSymbols.Count > i && selectedSymbols[i] == guidedSymbols[i])
                {
                    color = GuidedDoneSymbolColor;
                }
                else if (selectedSymbols.Count == i)
                {
                    color = GuidedNextSymbolColor;
                }

                Rect markerRect = new Rect(center.x - 22f, center.y - 22f, 44f, 44f);
                Widgets.DrawBoxSolid(markerRect, color);
                Widgets.DrawBox(markerRect, 1);

                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Tiny;
                Widgets.Label(markerRect, (i + 1).ToString());
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
            }
        }

        private void HandlePanelClick(Rect panelRect)
        {
            if (!Widgets.ButtonInvisible(panelRect))
            {
                return;
            }

            Vector2 mouse = Event.current.mousePosition;
            Vector2 center = panelRect.center;
            Vector2 delta = mouse - center;
            float normalizedRadius = delta.magnitude / (Mathf.Min(panelRect.width, panelRect.height) / 2f);

            if (normalizedRadius < 0.16f)
            {
                TryConfirmAddress();
                return;
            }

            string ringPrefix = RingPrefixForRadius(normalizedRadius);
            if (ringPrefix == null)
            {
                return;
            }

            if (selectedSymbols.Count >= AddressLength)
            {
                Messages.Message("Adresa uz ma sedm symbolu.", MessageTypeDefOf.NeutralEvent, false);
                return;
            }

            float angle = Mathf.Atan2(delta.x, -delta.y) * Mathf.Rad2Deg;
            angle = (angle + 360f) % 360f;
            int symbol = Mathf.FloorToInt(angle / (360f / SymbolsPerRing)) + 1;
            selectedSymbols.Add(ringPrefix + symbol.ToString("00"));
        }

        private bool TrySymbolPosition(Rect panelRect, string symbolCode, out Vector2 position)
        {
            position = Vector2.zero;
            if (symbolCode.NullOrEmpty() || symbolCode.Length < 3)
            {
                return false;
            }

            string ringPrefix = symbolCode.Substring(0, 1);
            if (!int.TryParse(symbolCode.Substring(1), out int symbol) || symbol < 1 || symbol > SymbolsPerRing)
            {
                return false;
            }

            float radius = RadiusForRing(ringPrefix);
            if (radius <= 0f)
            {
                return false;
            }

            float angle = (symbol - 0.5f) * (360f / SymbolsPerRing) * Mathf.Deg2Rad;
            float pixelRadius = Mathf.Min(panelRect.width, panelRect.height) / 2f * radius;
            Vector2 center = panelRect.center;
            position = new Vector2(
                center.x + Mathf.Sin(angle) * pixelRadius,
                center.y - Mathf.Cos(angle) * pixelRadius);
            return true;
        }

        private float RadiusForRing(string ringPrefix)
        {
            switch (ringPrefix)
            {
                case "O":
                    return 0.83f;
                case "M":
                    return 0.55f;
                case "I":
                    return 0.32f;
                default:
                    return -1f;
            }
        }

        private string RingPrefixForRadius(float normalizedRadius)
        {
            if (normalizedRadius >= 0.68f && normalizedRadius <= 0.98f)
            {
                return "O";
            }

            if (normalizedRadius >= 0.43f && normalizedRadius < 0.68f)
            {
                return "M";
            }

            if (normalizedRadius >= 0.22f && normalizedRadius < 0.43f)
            {
                return "I";
            }

            return null;
        }

        private void TryConfirmAddress()
        {
            if (selectedSymbols.Count != AddressLength)
            {
                Messages.Message("StarGate adresa musi mit sedm symbolu.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            string address = CurrentAddress();
            StarGatePlanetSystem planetSystem = Current.Game.GetComponent<StarGatePlanetSystem>();
            planetSystem?.EnsurePlanetForAddress(address);
            planetSystem?.RegisterUsedAddress(address);

            CompStarGate gate = panel.LinkedGate();
            if (gate == null)
            {
                Messages.Message("Panel neni pripojen k brane.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (!guidedSiteId.NullOrEmpty() && address == guidedAddress)
            {
                gate.SetDialedTarget(address, guidedSiteId);
            }
            else
            {
                gate.SetDialedAddress(address);
            }

            gate.StartWarmup();
            Close();
        }

        private string CurrentAddressText()
        {
            if (selectedSymbols.Count == 0)
            {
                return "(empty)";
            }

            return string.Join("-", selectedSymbols.ToArray());
        }

        private string CurrentAddress()
        {
            return string.Join("-", selectedSymbols.ToArray());
        }
    }
}
