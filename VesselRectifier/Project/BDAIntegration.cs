using System;
using System.Reflection;
using UnityEngine;

namespace KomUniMunVesselRectifier
{
    internal static class BdaIntegration
    {
        public static bool IsAvailable;

        // Detects if BDArmory is installed and loaded.
        public static void DetectPresence()
        {
            IsAvailable = false;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "BDArmory")
                {
                    IsAvailable = true;
                    break;
                }
            }
            VerboseLogging.Log($"[KUM] BDArmory: {(IsAvailable ? "DETECTED" : "NOT FOUND")}");
        }

        // Clears the cached reflection fields and methods.
        public static void ClearReflectionCache()
        {
            VerboseLogging.Log("Clearing BDArmory reflection cache.");
            BdaReflectionCache.Clear();
        }

        // Enforces the active combat state on BDArmory modules.
        public static void ForceCombatState(Vessel vessel, VesselTrack tracking)
        {
            if (!IsAvailable || vessel?.parts == null || !vessel.loaded || tracking == null)
                return;

            VerboseLogging.Log($"Evaluating combat states for {vessel.vesselName}");

            for (int i = vessel.parts.Count - 1; i >= 0; i--)
            {
                Part part = vessel.parts[i];

                if (part?.Modules == null)
                    continue;

                for (int m = 0; m < part.Modules.Count; m++)
                {
                    PartModule module = part.Modules[m];

                    if (module == null)
                        continue;

                    string typeName = module.GetType().Name;

                    // Check or die.
                    if (typeName == "MissileFire")
                        EnforceWeaponManagerState(module);
                    else if (
                        typeName == "BDModulePilotAI"
                        || typeName == "BDModuleSurfaceAI"
                        || typeName == "BDModuleVTOLAI"
                        || typeName == "BDModuleOrbitalAI"
                        || typeName == "BDGenericAIBase"
                    )
                        EnforcePilotAiState(module);
                }
            }
        }

        // Forces the BDArmory Weapon Manager module into armed and guard mode.
        private static void EnforceWeaponManagerState(PartModule weaponManager)
        {
            if (!BdaReflectionCache.MissileFireFieldsResolved)
            {
                Type type = weaponManager.GetType();
                BdaReflectionCache.MissileFireGuardModeField = ReflectionUtils.FindFieldInHierarchy(
                    type,
                    "guardMode"
                );
                BdaReflectionCache.MissileFireIsArmedField = ReflectionUtils.FindFieldInHierarchy(
                    type,
                    "isArmed"
                );
                BdaReflectionCache.MissileFireToggleGuardModeMethod =
                    ReflectionUtils.FindMethodInHierarchy(type, "ToggleGuardMode");
                BdaReflectionCache.MissileFireFieldsResolved = true;
            }

            if (BdaReflectionCache.MissileFireIsArmedField != null)
            {
                if (BdaReflectionCache.MissileFireIsArmedField.GetValue(weaponManager) is false)
                {
                    VerboseLogging.Log("Weapon Manager Armed.");
                    BdaReflectionCache.MissileFireIsArmedField.SetValue(weaponManager, true);
                }
            }

            if (BdaReflectionCache.MissileFireGuardModeField != null)
            {
                if (BdaReflectionCache.MissileFireGuardModeField.GetValue(weaponManager) is false)
                {
                    VerboseLogging.Log("Guard Mode activated.");
                    if (BdaReflectionCache.MissileFireToggleGuardModeMethod != null)
                    {
                        try
                        {
                            BdaReflectionCache.MissileFireToggleGuardModeMethod.Invoke(
                                weaponManager,
                                null
                            );
                        }
                        catch
                        {
                            BdaReflectionCache.MissileFireGuardModeField.SetValue(
                                weaponManager,
                                true
                            );
                        }
                    }
                    else
                    {
                        BdaReflectionCache.MissileFireGuardModeField.SetValue(weaponManager, true);
                    }
                }
            }
        }

        // Forces the BDArmory Pilot AI module to activate.
        private static void EnforcePilotAiState(PartModule pilotAi)
        {
            if (!BdaReflectionCache.PilotAiFieldsResolved)
            {
                Type type = pilotAi.GetType();
                BdaReflectionCache.PilotAiEnabledField = ReflectionUtils.FindFieldInHierarchy(
                    type,
                    "pilotEnabled"
                );
                BdaReflectionCache.PilotAiActivatePilotMethod =
                    ReflectionUtils.FindMethodInHierarchy(type, "ActivatePilot")
                    ?? ReflectionUtils.FindMethodInHierarchy(type, "EnablePilot");
                BdaReflectionCache.PilotAiFieldsResolved = true;
            }

            if (BdaReflectionCache.PilotAiEnabledField != null)
            {
                if (BdaReflectionCache.PilotAiEnabledField.GetValue(pilotAi) is false)
                {
                    VerboseLogging.Log("Pilot AI activated.");
                    if (BdaReflectionCache.PilotAiActivatePilotMethod != null)
                    {
                        try
                        {
                            BdaReflectionCache.PilotAiActivatePilotMethod.Invoke(pilotAi, null);
                        }
                        catch
                        {
                            BdaReflectionCache.PilotAiEnabledField.SetValue(pilotAi, true);
                        }
                    }
                    else
                    {
                        BdaReflectionCache.PilotAiEnabledField.SetValue(pilotAi, true);
                    }
                }
            }
        }

        // Static cache for resolved BDArmory reflection targets.
        private static class BdaReflectionCache
        {
            public static FieldInfo MissileFireGuardModeField;
            public static FieldInfo MissileFireIsArmedField;
            public static MethodInfo MissileFireToggleGuardModeMethod;
            public static bool MissileFireFieldsResolved;

            public static FieldInfo PilotAiEnabledField;
            public static MethodInfo PilotAiActivatePilotMethod;
            public static bool PilotAiFieldsResolved;

            // Clears all internal cached reflection states.
            public static void Clear()
            {
                MissileFireGuardModeField = null;
                MissileFireIsArmedField = null;
                MissileFireToggleGuardModeMethod = null;
                MissileFireFieldsResolved = false;

                PilotAiEnabledField = null;
                PilotAiActivatePilotMethod = null;
                PilotAiFieldsResolved = false;
            }
        }
    }
}
