using RimWorld.Planet;
using Verse;

namespace RimGateJaffaKree
{
    public class StarGatePlanetMapParent : PocketMapParent
    {
        public const int CurrentGenerationVersion = 1;

        public string address;
        public string siteId;
        public int generationSeed;
        public int generationVersion = CurrentGenerationVersion;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref address, "starGateAddress");
            Scribe_Values.Look(ref siteId, "starGateSiteId");
            Scribe_Values.Look(ref generationSeed, "starGateGenerationSeed", 0);
            Scribe_Values.Look(ref generationVersion, "starGateGenerationVersion", CurrentGenerationVersion);
        }

        public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
        {
            alsoRemoveWorldObject = false;
            return false;
        }
    }
}
