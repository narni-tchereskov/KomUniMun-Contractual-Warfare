using System;
using System.Collections.Generic;
using UnityEngine;

namespace KomUniMunVesselRectifier
{
    internal static class VesselTracking
    {
        private static readonly Dictionary<Guid, VesselTrack> _trackedVessels =
            new Dictionary<Guid, VesselTrack>();

        // Keeps track of managed vessels by Guid.
        internal static bool IsVesselManaged(Guid vesselId)
        {
            if (
                !_trackedVessels.TryGetValue(vesselId, out VesselTrack tracking)
                || tracking == null
            )
                return false;

            return (tracking.Flags & VesselFlags.Managed) != 0;
        }

        // Checks if a tracked vessel has an operation flag.
        internal static bool VesselHasFlag(Guid vesselId, VesselFlags flag)
        {
            if (
                !_trackedVessels.TryGetValue(vesselId, out VesselTrack tracking)
                || tracking == null
            )
                return false;

            return (tracking.Flags & flag) == flag;
        }

        // Assigns a specific operation flag to a tracked vessel.
        internal static void SetVesselFlag(Guid vesselId, VesselFlags flag)
        {
            if (!_trackedVessels.TryGetValue(vesselId, out VesselTrack tracking))
            {
                tracking = new VesselTrack();
                _trackedVessels[vesselId] = tracking;
            }

            tracking.Flags |= flag;
            VerboseLogging.Log($"Flag {flag} assigned to vessel {vesselId}.");
        }

        // Returns the vessel tracking data for a specific vessel.
        internal static VesselTrack GetVesselTracking(Guid vesselId)
        {
            _trackedVessels.TryGetValue(vesselId, out VesselTrack tracking);
            return tracking;
        }

        // Registers a vessel in the tracking system.
        internal static void TryRegisterVessel(Vessel vessel)
        {
            if (vessel == null)
                return;

            if (vessel.vesselType == VesselType.Debris)
                return;

            if (!vessel.vesselName.IsContractVessel())
                return;

            if (IsVesselManaged(vessel.id))
                return;

            SetVesselFlag(vessel.id, VesselFlags.Managed);
            VerboseLogging.Log($"Managing new vessel: {vessel.vesselName}");
        }

        // Clears a specific operation flag from a tracked vessel.
        internal static void ClearVesselFlag(Guid vesselId, VesselFlags flag)
        {
            if (
                !_trackedVessels.TryGetValue(vesselId, out VesselTrack tracking)
                || tracking == null
            )
                return;

            tracking.Flags &= ~flag;
            VerboseLogging.Log($"Flag {flag} cleared from vessel {vesselId}.");
        }

        // Stops tracking for a specific vessel.
        internal static void StopTracking(Guid vesselId)
        {
            if (_trackedVessels.Remove(vesselId))
                VerboseLogging.Log($"Stopped tracking vessel: {vesselId}");
        }

        // Stops tracking for all vessels.
        internal static void ClearAll()
        {
            _trackedVessels.Clear();
            VerboseLogging.Log("Cleared all tracked vessels.");
        }
    }
}
