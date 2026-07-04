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
        public List<string> onlineTexPaths;
        public int onlineAnimationTicks = 15;
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
        private List<Graphic> onlineGraphics;

        private CompProperties_StarGate Props => (CompProperties_StarGate)props;
        public bool IsOnline => onlineTicksLeft > 0;
        public bool IsWarmingUp => warmupTicksLeft > 0;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            List<string> texPaths = Props.onlineTexPaths;
            if ((texPaths == null || texPaths.Count == 0) && !Props.onlineTexPath.NullOrEmpty())
            {
                texPaths = new List<string> { Props.onlineTexPath };
            }

            if (texPaths != null && texPaths.Count > 0)
            {
                onlineGraphics = texPaths
                    .Where(path => !path.NullOrEmpty())
                    .Select(path => GraphicDatabase.Get<Graphic_Single>(
                        path,
                        ShaderDatabase.Cutout,
                        parent.Graphic.drawSize,
                        Color.white))
                    .ToList();
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
                    ActivateLinkedGates();
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
            if (IsOnline && onlineGraphics != null && onlineGraphics.Count > 0)
            {
                int frameTicks = Mathf.Max(1, Props.onlineAnimationTicks);
                int frame = (Find.TickManager.TicksGame / frameTicks) % onlineGraphics.Count;
                onlineGraphics[frame].Draw(parent.DrawPos, parent.Rotation, parent);
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
                yield return new FloatMenuOption("StarGate se ovlada pouze panelem", null);
                yield break;
            }

            yield return new FloatMenuOption("Projit", delegate
            {
                StarGateTravelUtility.TravelThrough(this, selPawn);
            });
        }

        public void StartWarmup()
        {
            if (IsOnline || IsWarmingUp)
            {
                return;
            }

            warmupTicksLeft = Props.warmupTicks;
            Messages.Message("StarGate se spousti.", parent, MessageTypeDefOf.NeutralEvent, false);
        }

        public void BringOnline(int ticks)
        {
            onlineTicksLeft = Mathf.Max(onlineTicksLeft, ticks);
            warmupTicksLeft = 0;
            parent.Map?.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Things);
        }

        public void ActivateLinkedGates()
        {
            BringOnline(Props.stayOnlineTicks);

            CompStarGate destination = FindDestination();
            if (destination != null)
            {
                destination.BringOnline(destination.Props.stayOnlineTicks);
                Messages.Message("StarGate spojeni je aktivni.", parent, MessageTypeDefOf.PositiveEvent, false);
            }
            else
            {
                Messages.Message("StarGate je online, ale neni dostupna druha brana.", parent, MessageTypeDefOf.NeutralEvent, false);
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
