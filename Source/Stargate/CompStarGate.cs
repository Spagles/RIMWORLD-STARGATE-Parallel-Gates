using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

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
        public int kawooshTicks = 24;
        public float kawooshLength = 2.5f;
        public float kawooshWidth = 2f;
        public bool drawPlaceholderKawoosh = false;

        public CompProperties_StarGate()
        {
            compClass = typeof(CompStarGate);
        }
    }

    public class CompStarGate : ThingComp
    {
        private int warmupTicksLeft;
        private int onlineTicksLeft;
        private int incomingOnlyTicksLeft;
        private int kawooshTicksLeft;
        private string dialedAddress;
        private string dialedSiteId;
        private List<Graphic> onlineGraphics;
        private static readonly Material KawooshMaterial = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.15f, 0.95f, 1f, 0.22f));

        private CompProperties_StarGate Props => (CompProperties_StarGate)props;
        public bool IsOnline => onlineTicksLeft > 0;
        public bool IsWarmingUp => warmupTicksLeft > 0;
        public bool IsIncomingOnly => IsOnline && incomingOnlyTicksLeft > 0;

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
            Scribe_Values.Look(ref incomingOnlyTicksLeft, "incomingOnlyTicksLeft", 0);
            Scribe_Values.Look(ref kawooshTicksLeft, "kawooshTicksLeft", 0);
            Scribe_Values.Look(ref dialedAddress, "dialedAddress");
            Scribe_Values.Look(ref dialedSiteId, "dialedSiteId");
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
                if (incomingOnlyTicksLeft > 0)
                {
                    incomingOnlyTicksLeft--;
                }
            }

            if (kawooshTicksLeft > 0)
            {
                kawooshTicksLeft--;
                parent.Map?.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Things);
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

            DrawKawoosh();
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

            string label = IsIncomingOnly ? "Vstoupit do prichozi brany (smrtelne)" : "Projit";
            yield return new FloatMenuOption(label, delegate
            {
                Job job = JobMaker.MakeJob(StarGateDefOf.EnterStarGate, parent, StarGateTravelUtility.EntryCellFor(parent, selPawn));
                selPawn.jobs.TryTakeOrderedJob(job);
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

        public void SetDialedAddress(string address)
        {
            dialedAddress = address;
            dialedSiteId = null;
        }

        public void SetDialedTarget(string address, string siteId)
        {
            dialedAddress = address;
            dialedSiteId = siteId;
        }

        public string DialedAddress => dialedAddress;
        public string DialedSiteId => dialedSiteId;

        public void BringOnline(int ticks, bool incomingOnly = false)
        {
            bool wasOnline = IsOnline;
            onlineTicksLeft = Mathf.Max(onlineTicksLeft, ticks);
            if (incomingOnly)
            {
                incomingOnlyTicksLeft = Mathf.Max(incomingOnlyTicksLeft, ticks);
            }

            warmupTicksLeft = 0;
            if (!wasOnline)
            {
                BeginKawoosh();
            }

            parent.Map?.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Things);
        }

        private void BeginKawoosh()
        {
            kawooshTicksLeft = Mathf.Max(1, Props.kawooshTicks);
            VaporizeKawooshArea();
        }

        private void DrawKawoosh()
        {
            if (!Props.drawPlaceholderKawoosh || kawooshTicksLeft <= 0 || parent?.Map == null)
            {
                return;
            }

            float duration = Mathf.Max(1f, Props.kawooshTicks);
            float progress = 1f - (kawooshTicksLeft / duration);
            float fade = Mathf.Sin(progress * Mathf.PI);
            float length = Mathf.Lerp(0.7f, Props.kawooshLength, progress);
            float width = Mathf.Lerp(0.5f, Props.kawooshWidth, fade);
            Vector3 drawPos = parent.DrawPos + new Vector3(0f, 0.22f, -length * 0.42f);
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Matrix4x4 matrix = Matrix4x4.TRS(
                drawPos,
                Quaternion.identity,
                new Vector3(width / 10f, 1f, length / 10f));

            Graphics.DrawMesh(MeshPool.plane10, matrix, KawooshMaterial, 0);
        }

        private void VaporizeKawooshArea()
        {
            if (parent?.Map == null)
            {
                return;
            }

            Map map = parent.Map;
            IntVec3 origin = parent.Position;
            float length = Mathf.Max(1f, Props.kawooshLength);
            float halfWidth = Mathf.Max(0.5f, Props.kawooshWidth * 0.5f);

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(origin, length + halfWidth + 2f, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                int forwardDistance = origin.z - cell.z;
                if (forwardDistance < 0 || forwardDistance > length)
                {
                    continue;
                }

                float widthAtDistance = Mathf.Lerp(0.8f, halfWidth, forwardDistance / length);
                if (Mathf.Abs(cell.x - origin.x) > widthAtDistance)
                {
                    continue;
                }

                VaporizeCell(map, cell);
                map.fogGrid.Unfog(cell);
            }
        }

        private void VaporizeCell(Map map, IntVec3 cell)
        {
            foreach (Thing thing in cell.GetThingList(map).ToList())
            {
                if (thing == parent || thing.Destroyed || thing.def.defName == "StarGate" || thing.def.defName == "StarGate_Control_Panel")
                {
                    continue;
                }

                Pawn pawn = thing as Pawn;
                if (pawn != null)
                {
                    pawn.Kill(null);
                    if (pawn.Corpse != null && !pawn.Corpse.Destroyed)
                    {
                        pawn.Corpse.Destroy(DestroyMode.Vanish);
                    }

                    continue;
                }

                thing.Destroy(DestroyMode.Vanish);
            }
        }

        public void ActivateLinkedGates()
        {
            BringOnline(Props.stayOnlineTicks);

            CompStarGate destination = FindDestination();
            if (destination != null)
            {
                destination.BringOnline(destination.Props.stayOnlineTicks, true);
                Messages.Message("StarGate spojeni je aktivni.", parent, MessageTypeDefOf.PositiveEvent, false);
            }
            else if (!dialedAddress.NullOrEmpty())
            {
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
