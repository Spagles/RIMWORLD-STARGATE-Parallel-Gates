using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimGateJaffaKree
{
    public class CompProperties_StarGate : CompProperties
    {
        public string onlineTexPath;
        public int warmupTicks = 120;
        public int stayOnlineTicks = 600;
        public int postTravelOnlineTicks = 180;

        public CompProperties_StarGate()
        {
            compClass = typeof(CompStarGate);
        }
    }

    public class CompStarGate : ThingComp
    {
        private int warmupTicksLeft;
        private int onlineTicksLeft;
        private Graphic onlineGraphic;

        private CompProperties_StarGate Props => (CompProperties_StarGate)props;
        private bool IsOnline => onlineTicksLeft > 0;
        private bool IsWarmingUp => warmupTicksLeft > 0;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!Props.onlineTexPath.NullOrEmpty())
            {
                onlineGraphic = GraphicDatabase.Get<Graphic_Single>(
                    Props.onlineTexPath,
                    ShaderDatabase.Cutout,
                    parent.Graphic.drawSize,
                    Color.white);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref warmupTicksLeft, "warmupTicksLeft", 0);
            Scribe_Values.Look(ref onlineTicksLeft, "onlineTicksLeft", 0);
        }

        public override void CompTick()
        {
            base.CompTick();

            if (warmupTicksLeft > 0)
            {
                warmupTicksLeft--;
                if (warmupTicksLeft == 0)
                {
                    BringOnline(Props.stayOnlineTicks);
                }
            }
            else if (onlineTicksLeft > 0)
            {
                onlineTicksLeft--;
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            if (IsOnline && onlineGraphic != null)
            {
                onlineGraphic.Draw(parent.DrawPos, parent.Rotation, parent);
            }
        }

        public override string TransformLabel(string label)
        {
            if (IsOnline)
            {
                return label + " (online)";
            }

            if (IsWarmingUp)
            {
                return label + " (spousti se)";
            }

            return label;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            Command_Action command = new Command_Action
            {
                defaultLabel = IsOnline ? "StarGate online" : IsWarmingUp ? "StarGate se spousti" : "Zapnout StarGate",
                defaultDesc = "Spusti branu. Po kratkem nabehu prejde do online stavu.",
                icon = ContentFinder<Texture2D>.Get("Things/Building/Stargate/StarGate", false),
                action = delegate
                {
                    StartWarmup();
                }
            };

            if (IsOnline || IsWarmingUp)
            {
                command.Disable(IsOnline ? "Brana uz je online." : "Brana se prave spousti.");
            }

            yield return command;
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

            if (IsWarmingUp)
            {
                yield return new FloatMenuOption("StarGate se spousti", null);
                yield break;
            }

            if (!IsOnline)
            {
                yield return new FloatMenuOption("Zapnout StarGate", delegate
                {
                    StartWarmup();
                });
                yield break;
            }

            CompStarGate destination = FindDestination();
            if (destination == null)
            {
                yield return new FloatMenuOption("Projit: neni dostupna druha StarGate", null);
                yield break;
            }

            yield return new FloatMenuOption("Projit", delegate
            {
                Travel(selPawn, destination);
            });
        }

        private void StartWarmup()
        {
            if (IsOnline || IsWarmingUp)
            {
                return;
            }

            warmupTicksLeft = Props.warmupTicks;
            Messages.Message("StarGate se spousti.", parent, MessageTypeDefOf.NeutralEvent, false);
        }

        private void BringOnline(int ticks)
        {
            onlineTicksLeft = Mathf.Max(onlineTicksLeft, ticks);
            warmupTicksLeft = 0;
            parent.Map?.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Things);
            Messages.Message("StarGate je online.", parent, MessageTypeDefOf.PositiveEvent, false);
        }

        private void Travel(Pawn pawn, CompStarGate destination)
        {
            if (pawn == null || destination == null || !pawn.Spawned)
            {
                return;
            }

            IntVec3 targetCell = CellFinder.StandableCellNear(destination.parent.Position, destination.parent.Map, 4f);
            if (!targetCell.IsValid)
            {
                Messages.Message("U cilove StarGate neni volne misto.", destination.parent, MessageTypeDefOf.RejectInput, false);
                return;
            }

            BringOnline(Props.postTravelOnlineTicks);
            destination.BringOnline(destination.Props.postTravelOnlineTicks);

            Map oldMap = pawn.Map;
            pawn.DeSpawn(DestroyMode.Vanish);
            GenSpawn.Spawn(pawn, targetCell, destination.parent.Map);
            pawn.Notify_Teleported(false, true);
            CameraJumper.TryJump(pawn);

            if (oldMap == destination.parent.Map)
            {
                Messages.Message("Kolonista prosel StarGate.", pawn, MessageTypeDefOf.PositiveEvent, false);
            }
            else
            {
                Messages.Message("Kolonista prosel StarGate na jinou mapu.", pawn, MessageTypeDefOf.PositiveEvent, false);
            }
        }

        private CompStarGate FindDestination()
        {
            if (Current.Game == null || Current.Game.Maps == null)
            {
                return null;
            }

            return Current.Game.Maps
                .Where(map => map != null)
                .SelectMany(map => map.listerThings.ThingsOfDef(parent.def))
                .Where(thing => thing != parent && thing.Spawned)
                .Select(thing => thing.TryGetComp<CompStarGate>())
                .FirstOrDefault(comp => comp != null);
        }
    }
}
