using System;
using UnityEngine;

namespace KomUniMunVesselRectifier
{
    internal static class VesselClassification
    {
        // Matches any managed vessel.
        internal static bool IsContractVessel(this string vesselName)
        {
            if (string.IsNullOrEmpty(vesselName))
                return false;

            return vesselName.IndexOf("ID:", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // Identifies units requiring altitude reposition).
        internal static bool IsContractAircraft(this string vesselName)
        {
            if (string.IsNullOrEmpty(vesselName))
                return false;

            return vesselName.IndexOf("AIR ID:", StringComparison.OrdinalIgnoreCase) >= 0
                || vesselName.IndexOf("ATGT ID:", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // Identifies units requiring altitude reposition with vertical velocity axis.
        internal static bool IsContractHelicopter(this string vesselName)
        {
            if (string.IsNullOrEmpty(vesselName))
                return false;

            return vesselName.IndexOf("HLI ID:", StringComparison.OrdinalIgnoreCase) >= 0
                || vesselName.IndexOf("HTGT ID:", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // Checks if Guard Mode should be enabled.
        internal static bool IsGuardEnabled(this string vesselName)
        {
            if (string.IsNullOrEmpty(vesselName))
                return false;

            return vesselName.IndexOf("SPC ID:", StringComparison.OrdinalIgnoreCase) >= 0
                || vesselName.IndexOf("AIR ID:", StringComparison.OrdinalIgnoreCase) >= 0
                || vesselName.IndexOf("GND ID:", StringComparison.OrdinalIgnoreCase) >= 0
                || vesselName.IndexOf("HLI ID:", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // Checks if Pilot AI should be enabled (AIR and ATGT).
        internal static bool IsPilotEnabled(this string vesselName)
        {
            if (string.IsNullOrEmpty(vesselName))
                return false;

            return vesselName.IndexOf("SPC ID:", StringComparison.OrdinalIgnoreCase) >= 0
                || vesselName.IndexOf("AIR ID:", StringComparison.OrdinalIgnoreCase) >= 0
                || vesselName.IndexOf("GND ID:", StringComparison.OrdinalIgnoreCase) >= 0
                || vesselName.IndexOf("HLI ID:", StringComparison.OrdinalIgnoreCase) >= 0
                || vesselName.IndexOf("ATGT ID:", StringComparison.OrdinalIgnoreCase) >= 0
                || vesselName.IndexOf("HTGT ID:", StringComparison.OrdinalIgnoreCase) >= 0;
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
