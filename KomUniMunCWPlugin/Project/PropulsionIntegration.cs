using System;
using UnityEngine;

namespace KomUniMunVesselRectifier
{
    internal static class PropulsionIntegration
    {
        // Forces all valid engines to ignite and activates their gimbal (without disabling lock).
        public static void ForceIgnition(Vessel vessel, VesselTrack tracking)
        {
            if (vessel?.parts == null || !vessel.loaded || tracking == null)
                return;

            VerboseLogging.Log($"Activating engines for {vessel.vesselName}");

            // Zero the vessel throttle just in case.
            vessel.ctrlState.mainThrottle = 0f;

            for (int i = vessel.parts.Count - 1; i >= 0; i--)
            {
                Part part = vessel.parts[i];

                if (part?.Modules == null)
                    continue;

                // Check if the engine is multi-mode.
                MultiModeEngine multiMode = part.FindModuleImplementing<MultiModeEngine>();
                string activeEngineId = null;

                if (multiMode != null)
                {
                    activeEngineId = multiMode.runningPrimary
                        ? multiMode.primaryEngineID
                        : multiMode.secondaryEngineID;
                }

                for (int m = 0; m < part.Modules.Count; m++)
                {
                    PartModule module = part.Modules[m];

                    if (module == null)
                        continue;

                    // If it's already active we don't care, do it AGAIN! (with discretion)
                    if (module is ModuleEngines engine)
                    {
                        if (
                            multiMode != null
                            && !string.IsNullOrEmpty(activeEngineId)
                            && !string.Equals(
                                engine.engineID,
                                activeEngineId,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        {
                            engine.Shutdown();
                            engine.EngineIgnited = false;
                            continue;
                        }

                        engine.Activate();
                        engine.staged = true;
                        engine.EngineIgnited = true;
                        engine.currentThrottle = 0f;
                    }
                    else if (module is ModuleGimbal gimbal)
                    {
                        gimbal.isEnabled = true;
                        gimbal.gimbalActive = true;
                    }
                }
            }
        }
    }
}
