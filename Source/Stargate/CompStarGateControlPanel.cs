using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimGateJaffaKree
{
    public class CompProperties_StarGateControlPanel : CompProperties
    {
        public string activeTexPath;
        public int interactionTicks = 180;
        public float linkedGateSearchRadius = 8f;

        public CompProperties_StarGateControlPanel()
        {
            compClass = typeof(CompStarGateControlPanel);
        }
    }

    public class CompStarGateControlPanel : ThingComp
    {
        private Graphic activeGraphic;

        private CompProperties_StarGateControlPanel Props => (CompProperties_StarGateControlPanel)props;
        public int InteractionTicks => Props.interactionTicks;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!Props.activeTexPath.NullOrEmpty())
            {
                activeGraphic = GraphicDatabase.Get<Graphic_Single>(
                    Props.activeTexPath,
                    ShaderDatabase.Cutout,
                    parent.Graphic.drawSize,
                    Color.white);
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            CompStarGate gate = LinkedGate();
            if (gate != null && (gate.IsOnline || gate.IsWarmingUp) && activeGraphic != null)
            {
                activeGraphic.Draw(parent.DrawPos, parent.Rotation, parent);
            }
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption option in base.CompFloatMenuOptions(selPawn))
            {
                yield return option;
            }

            if (selPawn == null || !selPawn.Spawned || selPawn.Map != parent.Map)
            {
                yield break;
            }

            CompStarGate gate = LinkedGate();
            if (gate == null)
            {
                yield return new FloatMenuOption(StarGateText.Get("StarGate_PanelDisconnected"), null);
                yield break;
            }

            if (gate.IsOnline)
            {
                yield return new FloatMenuOption(StarGateText.Get("StarGate_ConnectionActive"), null);
                yield break;
            }

            if (gate.IsWarmingUp)
            {
                yield return new FloatMenuOption(StarGateText.Get("StarGate_WarmingUp"), null);
                yield break;
            }

            if (!selPawn.CanReach(parent, PathEndMode.Touch, Danger.Some))
            {
                yield return new FloatMenuOption(StarGateText.Get("StarGate_CannotReachPanel"), null);
                yield break;
            }

            yield return new FloatMenuOption(StarGateText.Get("StarGate_Enable"), delegate
            {
                Job job = JobMaker.MakeJob(StarGateDefOf.UseStarGateControlPanel, parent);
                selPawn.jobs.TryTakeOrderedJob(job);
            });

            yield return new FloatMenuOption(StarGateText.Get("StarGate_OpenAddressBook"), delegate
            {
                Find.WindowStack.Add(new Dialog_StarGateAddressBook(this));
            });
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            yield return new Command_Action
            {
                defaultLabel = StarGateText.Get("StarGate_AddressBook"),
                defaultDesc = StarGateText.Get("StarGate_AddressBookDesc"),
                icon = BaseContent.WhiteTex,
                action = delegate
                {
                    Find.WindowStack.Add(new Dialog_StarGateAddressBook(this));
                }
            };

            yield return new Command_Action
            {
                defaultLabel = StarGateText.Get("StarGate_Galaxy"),
                defaultDesc = StarGateText.Get("StarGate_GalaxyDesc"),
                icon = BaseContent.WhiteTex,
                action = delegate
                {
                    Find.WindowStack.Add(new Dialog_StarGateGalaxyMap(this));
                }
            };

            yield return new Command_Action
            {
                defaultLabel = StarGateText.Get("StarGate_Database"),
                defaultDesc = StarGateText.Get("StarGate_DatabaseDesc"),
                icon = BaseContent.WhiteTex,
                action = delegate
                {
                    Find.WindowStack.Add(new Dialog_StarGateDebugDatabase());
                }
            };

            yield return new Command_Action
            {
                defaultLabel = StarGateText.Get("StarGate_DebugArrival"),
                defaultDesc = StarGateText.Get("StarGate_DebugArrivalDesc"),
                icon = BaseContent.WhiteTex,
                action = delegate
                {
                    Find.WindowStack.Add(new Dialog_StarGateIncidentDebug(parent.Map));
                }
            };
        }

        public CompStarGate LinkedGate()
        {
            if (parent?.Map == null)
            {
                return null;
            }

            ThingDef gateDef = ThingDef.Named("StarGate");
            return parent.Map.listerThings.ThingsOfDef(gateDef)
                .Where(thing => thing.Spawned)
                .OrderBy(thing => thing.Position.DistanceToSquared(parent.Position))
                .Where(thing => thing.Position.DistanceTo(parent.Position) <= Props.linkedGateSearchRadius)
                .Select(thing => thing.TryGetComp<CompStarGate>())
                .FirstOrDefault(comp => comp != null);
        }
    }
}
