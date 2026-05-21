using System;

namespace KomUniMunVesselRectifier
{
    internal class VesselTrack
    {
        public VesselFlags Flags { get; set; }

        public float UnpackedAtTime { get; set; } = -1f;
    }
}
