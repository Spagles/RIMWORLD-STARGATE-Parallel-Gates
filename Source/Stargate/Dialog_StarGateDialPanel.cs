using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimGateJaffaKree
{
    public class Dialog_StarGateDialPanel : Window
    {
        private const int SymbolsPerRing = 20;
        private const int AddressLength = 7;
        private static readonly Texture2D PanelTexture = ContentFinder<Texture2D>.Get("UI/Stargate/DialPanel");
        private static readonly Color SelectedSymbolColor = new Color(0.55f, 0.9f, 1f, 0.38f);

        private readonly CompStarGateControlPanel panel;
        private readonly List<string> selectedSymbols = new List<string>();

        public override Vector2 InitialSize => new Vector2(900f, 700f);

        public Dialog_StarGateDialPanel(CompStarGateControlPanel panel)
        {
            this.panel = panel;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
            doCloseX = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f), "StarGate dialing address");

            Text.Font = GameFont.Small;
            Rect addressRect = new Rect(inRect.x, inRect.y + 36f, inRect.width, 28f);
            Widgets.Label(addressRect, "Address: " + CurrentAddressText());

            float panelSize = Mathf.Min(inRect.width - 40f, inRect.height - 100f);
            Rect panelRect = new Rect(inRect.center.x - panelSize / 2f, inRect.y + 76f, panelSize, panelSize * 1696f / 2516f);
            Widgets.DrawTextureFitted(panelRect, PanelTexture, 1f);
            DrawSelectedSymbols(panelRect);

            HandlePanelClick(panelRect);

            Rect buttonsRect = new Rect(inRect.x, inRect.yMax - 72f, inRect.width, 32f);
            DrawControlButtons(buttonsRect);

            Rect hintRect = new Rect(inRect.x, inRect.yMax - 34f, inRect.width, 30f);
            Widgets.Label(hintRect, "Click 7 symbols on any ring, then click the red crystal to activate.");
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

            CompStarGate gate = panel.LinkedGate();
            if (gate == null)
            {
                Messages.Message("Panel neni pripojen k brane.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            gate.SetDialedAddress(address);
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
