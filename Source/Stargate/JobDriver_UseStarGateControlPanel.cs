using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimGateJaffaKree
{
    [DefOf]
    public static class StarGateDefOf
    {
        public static JobDef UseStarGateControlPanel;
    }

    public class JobDriver_UseStarGateControlPanel : JobDriver
    {
        private Thing Panel => job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Panel, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => Panel.TryGetComp<CompStarGateControlPanel>()?.LinkedGate() == null);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            Toil usePanel = Toils_General.Wait(Panel.TryGetComp<CompStarGateControlPanel>()?.InteractionTicks ?? 180);
            usePanel.WithProgressBarToilDelay(TargetIndex.A);
            usePanel.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            yield return usePanel;

            Toil activate = new Toil
            {
                initAction = delegate
                {
                    CompStarGateControlPanel panelComp = Panel.TryGetComp<CompStarGateControlPanel>();
                    if (panelComp != null)
                    {
                        Find.WindowStack.Add(new Dialog_StarGateDialPanel(panelComp));
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return activate;
        }
    }
}
