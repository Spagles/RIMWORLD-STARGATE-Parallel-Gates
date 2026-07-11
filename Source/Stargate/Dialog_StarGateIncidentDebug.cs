using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimGateJaffaKree
{
    public class Dialog_StarGateIncidentDebug : Window
    {
        private readonly Map map;

        public override Vector2 InitialSize => new Vector2(420f, 360f);

        public Dialog_StarGateIncidentDebug(Map map)
        {
            this.map = map;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
            doCloseX = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 34f), "StarGate - Parallel Gates");
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(inRect.x, inRect.y + 40f, inRect.width, 24f), "Incidents");

            float y = inRect.y + 72f;
            DrawButtonRow(inRect, ref y, "Execute raid with points (weak)", "raid", 300f);
            DrawButtonRow(inRect, ref y, "Execute raid with points (medium)", "raid", 700f);
            DrawButtonRow(inRect, ref y, "Execute raid with points (strong)", "raid", 1400f);
            DrawButtonRow(inRect, ref y, "Do trade caravan arrival", "trader", 350f);
            DrawButtonRow(inRect, ref y, "Do ally / visitor arrival", "ally", 350f);
            DrawButtonRow(inRect, ref y, "Random Stargate arrival", "random", -1f);
        }

        private void DrawButtonRow(Rect inRect, ref float y, string label, string mode, float points)
        {
            Rect row = new Rect(inRect.x, y, inRect.width, 34f);
            Widgets.DrawBoxSolid(row, new Color(0.16f, 0.16f, 0.16f, 0.96f));
            if (Widgets.ButtonText(new Rect(row.x + 8f, row.y + 4f, row.width - 16f, 26f), label))
            {
                bool triggered = mode == "random"
                    ? (Current.Game.GetComponent<GameComponent_StarGateIncidents>()?.DebugTriggerOnMap(map) ?? false)
                    : (Current.Game.GetComponent<GameComponent_StarGateIncidents>()?.DebugTriggerOnMap(map, mode, points) ?? false);

                if (!triggered)
                {
                    Messages.Message(StarGateText.Get("StarGate_DebugIncidentFailed"), MessageTypeDefOf.RejectInput, false);
                }
                else
                {
                    Close();
                }
            }

            y += 40f;
        }
    }
}
