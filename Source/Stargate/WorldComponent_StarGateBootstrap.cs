using System.Linq;
using RimWorld;
using RimWorld.Planet;
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
            if (!ensuredWorldGate)
            {
                EnsureWorldGate();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ensuredWorldGate, "ensuredWorldGate", false);
        }

        private void EnsureWorldGate()
        {
            if (Find.World == null || Find.WorldGrid == null || Find.WorldObjects == null)
            {
                return;
            }

            WorldObjectDef def = DefDatabase<WorldObjectDef>.GetNamedSilentFail("StarGateWorldSite");
            if (def == null)
            {
                return;
            }

            if (Find.WorldObjects.AllWorldObjects.Any(obj => obj.def == def))
            {
                ensuredWorldGate = true;
                return;
            }

            Settlement playerSettlement = Find.WorldObjects.Settlements.FirstOrDefault(settlement => settlement.Faction == Faction.OfPlayer);
            if (playerSettlement == null)
            {
                return;
            }

            if (!TryFindTile(playerSettlement.Tile, out int tile))
            {
                return;
            }

            WorldObject gate = WorldObjectMaker.MakeWorldObject(def);
            gate.Tile = tile;
            Find.WorldObjects.Add(gate);
            ensuredWorldGate = true;
        }

        private static bool TryFindTile(int playerTile, out int tile)
        {
            for (int i = 0; i < 2000; i++)
            {
                int candidate = Rand.Range(0, Find.WorldGrid.TilesCount);
                if (Find.WorldGrid.ApproxDistanceInTiles(playerTile, candidate) < 35)
                {
                    continue;
                }

                Tile worldTile = Find.WorldGrid[candidate];
                if (worldTile == null || worldTile.PrimaryBiome == null || worldTile.PrimaryBiome.impassable)
                {
                    continue;
                }

                if (worldTile.hilliness == Hilliness.Impassable)
                {
                    continue;
                }

                if (Find.WorldObjects.ObjectsAt(candidate).Any())
                {
                    continue;
                }

                tile = candidate;
                return true;
            }

            tile = -1;
            return false;
        }
    }
}
