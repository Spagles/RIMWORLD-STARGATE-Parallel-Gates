using RimWorld.Planet;
using System.Linq;
using Verse;

namespace RimGateJaffaKree
{
    public class WorldComponent_StarGateBootstrap : WorldComponent
    {
        private bool ensuredWorldGate;

        public WorldComponent_StarGateBootstrap(World world) : base(world)
        {
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (ensuredWorldGate || Current.Game == null || Current.ProgramState != ProgramState.Playing)
            {
                return;
            }

            Map homeMap = Current.Game.Maps?.FirstOrDefault(map => map != null && map.IsPlayerHome);
            if (homeMap == null)
            {
                return;
            }

            CompStarGate gate = StarGateTravelUtility.EnsureGateOnMap(homeMap);
            if (gate == null)
            {
                return;
            }

            ensuredWorldGate = true;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ensuredWorldGate, "ensuredWorldGate", false);
        }
    }
}
