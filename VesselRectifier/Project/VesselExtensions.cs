using UnityEngine;

namespace KomUniMunVesselRectifier
{
    internal static class VesselExtensions
    {
        public static void SafeIgnoreGForces(this Vessel vessel, int frames)
        {
            if (vessel == null)
                return;

            try
            {
                vessel.IgnoreGForces(frames);
            }
            catch { }
        }
    }
}
