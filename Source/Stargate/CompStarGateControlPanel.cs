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
                yield return new FloatMenuOption("Zapnout StarGate: panel neni pripojen k brane", null);
                yield break;
            }

            if (gate.IsOnline)
            {
                yield return new FloatMenuOption("StarGate spojeni je aktivni", null);
                yield break;
            }

            if (gate.IsWarmingUp)
            {
                yield return new FloatMenuOption("StarGate se spousti", null);
                yield break;
            }

            if (!selPawn.CanReach(parent, PathEndMode.Touch, Danger.Some))
            {
                yield return new FloatMenuOption("Zapnout StarGate: nelze dojit k panelu", null);
                yield break;
            }

            yield return new FloatMenuOption("Zapnout StarGate", delegate
            {
                Job job = JobMaker.MakeJob(StarGateDefOf.UseStarGateControlPanel, parent);
                selPawn.jobs.TryTakeOrderedJob(job);
            });

            yield return new FloatMenuOption("Otevrit StarGate adresar", delegate
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
                defaultLabel = "Adresar",
                defaultDesc = "Otevre posledni a domovske StarGate adresy.",
                icon = BaseContent.WhiteTex,
                action = delegate
                {
                    Find.WindowStack.Add(new Dialog_StarGateAddressBook(this));
                }
            };

            yield return new Command_Action
            {
                defaultLabel = "Galaxy",
                defaultDesc = "Otevre StarGate seznam domovske planety a objevenych planet.",
                icon = BaseContent.WhiteTex,
                action = delegate
                {
                    Find.WindowStack.Add(new Dialog_StarGateGalaxyMap(this));
                }
            };

            yield return new Command_Action
            {
                defaultLabel = "StarGate DB",
                defaultDesc = "Zobrazi ulozene StarGate planety, adresy a mapy.",
                icon = BaseContent.WhiteTex,
                action = delegate
                {
                    Find.WindowStack.Add(new Dialog_StarGateDebugDatabase());
                }
            };

            yield return new Command_Action
            {
                defaultLabel = "Debug prichod",
                defaultDesc = "Otevre debug volby pro raid, obchodniky a spojence pres StarGate.",
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
