using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimGateJaffaKree
{
    public class JobDriver_EnterStarGate : JobDriver
    {
        private Thing Gate => job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Gate, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => Gate.TryGetComp<CompStarGate>() == null);
            this.FailOn(() => !Gate.TryGetComp<CompStarGate>().IsOnline);

            Toil chooseEntryCell = new Toil
            {
                initAction = delegate
                {
                    job.targetB = StarGateTravelUtility.EntryCellFor(Gate, pawn);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return chooseEntryCell;

            yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);

            Toil enter = Toils_General.Wait(20);
            enter.WithProgressBarToilDelay(TargetIndex.A);
            yield return enter;

            Toil travel = new Toil
            {
                initAction = delegate
                {
                    StarGateTravelUtility.EnterGate(Gate.TryGetComp<CompStarGate>(), pawn);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return travel;
        }
    }
}
