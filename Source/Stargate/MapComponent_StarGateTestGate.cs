using RimWorld;
using System.Linq;
using Verse;

namespace RimGateJaffaKree
{
    public class MapComponent_StarGateTestGate : MapComponent
    {
        private int nextPanelEnsureTick;

        public MapComponent_StarGateTestGate(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (Find.TickManager.TicksGame >= nextPanelEnsureTick)
            {
                nextPanelEnsureTick = Find.TickManager.TicksGame + 120;
                EnsurePanels();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextPanelEnsureTick, "nextPanelEnsureTick", 0);
        }

        private void EnsurePanels()
        {
            if (map == null)
            {
                return;
            }

            ThingDef gateDef = ThingDef.Named("StarGate");
            ThingDef panelDef = ThingDef.Named("StarGate_Control_Panel");

            foreach (Thing gate in map.listerThings.ThingsOfDef(gateDef).Where(thing => thing.Spawned).ToList())
            {
                if (HasNearbyPanel(gate, panelDef))
                {
                    continue;
                }

                if (TryFindPanelCell(gate, panelDef, out IntVec3 panelCell))
                {
                    GenSpawn.Spawn(panelDef, panelCell, map);
                }
            }
        }

        private bool HasNearbyPanel(Thing gate, ThingDef panelDef)
        {
            return map.listerThings.ThingsOfDef(panelDef)
                .Any(panel => panel.Spawned && panel.Position.DistanceTo(gate.Position) <= 8f);
        }

        private bool TryFindPanelCell(Thing gate, ThingDef panelDef, out IntVec3 cell)
        {
            IntVec3[] preferredCells =
            {
                gate.Position + new IntVec3(-2, 0, -3),
                gate.Position + new IntVec3(2, 0, -3),
                gate.Position + new IntVec3(-3, 0, -2),
                gate.Position + new IntVec3(3, 0, -2),
                gate.Position + new IntVec3(-2, 0, 3),
                gate.Position + new IntVec3(2, 0, 3)
            };

            foreach (IntVec3 candidate in preferredCells)
            {
                if (CanPlacePanel(panelDef, candidate))
                {
                    cell = candidate;
                    return true;
                }
            }

            for (int i = 0; i < 80; i++)
            {
                IntVec3 candidate = CellFinder.RandomClosewalkCellNear(gate.Position, map, 5);
                if (CanPlacePanel(panelDef, candidate))
                {
                    cell = candidate;
                    return true;
                }
            }

            cell = IntVec3.Invalid;
            return false;
        }

        private bool CanPlacePanel(ThingDef panelDef, IntVec3 candidate)
        {
            return candidate.InBounds(map)
                && candidate.Standable(map)
                && !candidate.Fogged(map)
                && GenConstruct.CanPlaceBlueprintAt(panelDef, candidate, Rot4.North, map).Accepted;
        }
    }
}
