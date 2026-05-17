using System;
using UnityEngine;

namespace KomUniMunVesselRectifier
{
    internal static class VesselClassification
    {
        // Checks if it is a contract NON aircraft vessel.
        internal static bool IsContractVessel(this string vesselName)
        {
            if (string.IsNullOrEmpty(vesselName))
                return false;

            return vesselName.IndexOf("ID:", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // Checks if it is a contract AIRCRAFT that flies over terrain without orbits.
        internal static bool IsContractAircraft(this string vesselName)
        {
            if (string.IsNullOrEmpty(vesselName))
                return false;

            return vesselName.IndexOf("AIR ID:", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // Validates if a vessel is initialized by its physics parameters.
        internal static bool IsFullyInitialized(this Vessel vessel)
        {
            if (vessel == null || !vessel.loaded)
                return false;

            if (vessel.orbit == null || vessel.orbit.referenceBody == null)
                return false;

            if (vessel.mainBody == null || vessel.mainBody.bodyTransform == null)
                return false;

            if (
                double.IsNaN(vessel.orbit.pos.x)
                || double.IsNaN(vessel.orbit.pos.y)
                || double.IsNaN(vessel.orbit.pos.z)
            )
                return false;

            return true;
        }
    }
}
