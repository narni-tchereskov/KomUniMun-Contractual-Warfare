using System;
using System.Collections.Generic;
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
            VerboseLogging.Log($"BDArmory: {(IsAvailable ? "DETECTED" : "NOT FOUND")}");
        }

        // Clears the cached reflection fields and methods.
        public static void ClearReflectionCache()
        {
            VerboseLogging.Log("Clearing BDArmory reflection cache.");
            BdaReflectionCache.Clear();
        }

        // Enforces the active combat state on BDArmory modules based on ID.
        public static void ForceCombatState(Vessel vessel, VesselTrack tracking)
        {
            if (!IsAvailable || vessel?.parts == null || !vessel.loaded || tracking == null)
                return;

            bool enableGuard = vessel.vesselName.IsGuardEnabled();
            bool enablePilot = vessel.vesselName.IsPilotEnabled();

            // If neither is required, abort the sweep.
            if (!enableGuard && !enablePilot)
                return;

            VerboseLogging.Log(
                $"Evaluating combat states for {vessel.vesselName} (Guard:{enableGuard}, Pilot:{enablePilot})"
            );

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
                    if (enableGuard && typeName == "MissileFire")
                    {
                        EnforceWeaponManagerState(module);
                    }
                    else if (
                        enablePilot
                        && (
                            typeName == "BDModulePilotAI"
                            || typeName == "BDModuleSurfaceAI"
                            || typeName == "BDModuleVTOLAI"
                            || typeName == "BDModuleOrbitalAI"
                            || typeName == "BDGenericAIBase"
                        )
                    )
                    {
                        EnforcePilotAiState(module);
                    }
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
                        catch (Exception ex)
                        {
                            Debug.LogWarning(
                                $"[KUM] Failed to invoke ToggleGuardMode: {ex.Message}."
                            );
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
            Type type = pilotAi.GetType();

            // Cache multiple AI types.
            if (
                !BdaReflectionCache.AiCache.TryGetValue(
                    type,
                    out BdaReflectionCache.AiReflectionData cache
                )
            )
            {
                cache = new BdaReflectionCache.AiReflectionData
                {
                    EnabledField = ReflectionUtils.FindFieldInHierarchy(type, "pilotEnabled"),
                    ActivateMethod =
                        ReflectionUtils.FindMethodInHierarchy(type, "ActivatePilot")
                        ?? ReflectionUtils.FindMethodInHierarchy(type, "EnablePilot"),
                };
                BdaReflectionCache.AiCache[type] = cache;
            }

            if (cache.EnabledField != null)
            {
                if (cache.EnabledField.GetValue(pilotAi) is false)
                {
                    VerboseLogging.Log($"Pilot AI ({type.Name}) activated.");
                    if (cache.ActivateMethod != null)
                    {
                        try
                        {
                            cache.ActivateMethod.Invoke(pilotAi, null);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning(
                                $"[KUM] Failed to invoke PilotAiActivate on {type.Name}: {ex.Message}."
                            );
                            cache.EnabledField.SetValue(pilotAi, true);
                        }
                    }
                    else
                    {
                        cache.EnabledField.SetValue(pilotAi, true);
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

            // Hold data per AI type.
            public class AiReflectionData
            {
                public FieldInfo EnabledField;
                public MethodInfo ActivateMethod;
            }

            public static readonly Dictionary<Type, AiReflectionData> AiCache =
                new Dictionary<Type, AiReflectionData>();

            // Clears all internal cached reflection states.
            public static void Clear()
            {
                MissileFireGuardModeField = null;
                MissileFireIsArmedField = null;
                MissileFireToggleGuardModeMethod = null;
                MissileFireFieldsResolved = false;

                AiCache.Clear();
            }
        }
    }
}
