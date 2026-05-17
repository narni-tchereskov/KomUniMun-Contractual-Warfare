using System;
using UnityEngine;

namespace KomUniMunVesselRectifier
{
    internal static class VerboseLogging
    {
        // Logs only if verbose logging is enabled.
        internal static void Log(string message)
        {
            if (Settings.VerboseLogs)
                Debug.Log($"[KUM] {message}");
        }
    }
}
