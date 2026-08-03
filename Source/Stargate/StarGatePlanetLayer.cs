using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RimGateJaffaKree
{
    public class StarGatePlanetLayer : SurfaceLayer
    {
        public string stargateAddress;
        public int generationSeed;
        public int generationVersion = StarGatePlanetSystem.CurrentGenerationVersion;

        public StarGatePlanetLayer()
        {
        }

        public StarGatePlanetLayer(int layerId, PlanetLayerDef def, float radius, Vector3 origin, Vector3 viewCenter,
            float viewAngle, int subdivisions, float extraCameraAltitude, float backgroundWorldCameraOffset,
            float backgroundWorldCameraParallaxDistancePer100Cells)
            : base(layerId, def, radius, origin, viewCenter, viewAngle, subdivisions, extraCameraAltitude,
                backgroundWorldCameraOffset, backgroundWorldCameraParallaxDistancePer100Cells)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref stargateAddress, "stargateAddress");
            Scribe_Values.Look(ref generationSeed, "generationSeed", 0);
            Scribe_Values.Look(ref generationVersion, "generationVersion", StarGatePlanetSystem.CurrentGenerationVersion);
        }
    }
}
