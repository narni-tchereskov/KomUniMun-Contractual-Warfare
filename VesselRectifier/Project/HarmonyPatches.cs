using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace KomUniMunVesselRectifier
{
    [HarmonyPatch(typeof(Vessel), nameof(Vessel.GoOffRails))]
    internal static class GoOffRailsPatch
    {
        // Intercepts unrailing to apply safe spawn position.
        [HarmonyPrefix]
        private static void Prefix(Vessel __instance)
        {
            if (__instance == null)
                return;

            if (!VesselTracking.IsVesselManaged(__instance.id))
                return;

            if (__instance.vesselType == VesselType.Debris)
                return;

            if (!__instance.vesselName.IsContractAircraft())
                return;

            if (!__instance.IsFullyInitialized())
                return;

            Positioning.MarkVesselAsFlying(__instance);

            VesselPositioned scenario = VesselPositioned.Instance;
            bool needsPositioning = scenario == null || !scenario.HasPositioned(__instance.id);

            if (needsPositioning)
            {
                VerboseLogging.Log($"Intercepted GoOffRails for {__instance.vesselName}.");

                // Only mark positioned if successful.
                if (Positioning.ApplySafeSpawnPosition(__instance))
                {
                    __instance.SafeIgnoreGForces(Settings.GHardeningDuration);
                    scenario?.AddPositioned(__instance.id);
                }
                else
                {
                    VerboseLogging.Log($"Positioning aborted for {__instance.vesselName}");
                }
            }
        }

        // Applies combat states and forces part unpacking.
        [HarmonyPostfix]
        private static void Postfix(Vessel __instance)
        {
            if (__instance == null)
                return;

            if (!VesselTracking.IsVesselManaged(__instance.id))
                return;

            if (__instance.vesselType == VesselType.Debris)
                return;

            if (!__instance.loaded)
                return;

            if (__instance.packed)
            {
                VerboseLogging.Log($"Postfix forcing unpack on {__instance.vesselName}.");
                Positioning.ForceUnpackAllParts(__instance);
            }
        }
    }

    // Keeps relevant vessels from changing physics states.
    [HarmonyPatch(typeof(Vessel), nameof(Vessel.GoOnRails))]
    internal static class GoOnRailsPatch
    {
        // Blocks managed and non-debris vessels from going on-rails.
        [HarmonyPrefix]
        private static bool Prefix(Vessel __instance)
        {
            if (__instance == null)
                return true;

            bool isManaged = VesselTracking.IsVesselManaged(__instance.id);
            bool isDebris = __instance.vesselType == VesselType.Debris;

            bool blocked = isManaged && !isDebris;
            if (blocked)
                VerboseLogging.Log($"Blocked GoOnRails for {__instance.vesselName}.");

            return !blocked;
        }
    }

    // Applies temporary hardening to managed vessels.
    [HarmonyPatch(typeof(Vessel), "MakeActive")]
    internal static class VesselMakeActivePatch
    {
        [HarmonyPostfix]
        private static void Postfix(Vessel __instance)
        {
            if (__instance == null)
                return;

            if (!VesselTracking.IsVesselManaged(__instance.id))
                return;

            VerboseLogging.Log($"Applying hardening to {__instance.vesselName}.");
            __instance.SafeIgnoreGForces(Settings.GHardeningDuration);
        }
    }

    // Stops PRE camera changes to avoid scene load & physics issues.
    [HarmonyPatch(typeof(FlightGlobals), nameof(FlightGlobals.ForceSetActiveVessel))]
    internal static class PreventPreForceSetActiveVesselPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Vessel __0)
        {
            if (__0 == null)
                return true;

            bool isManaged = VesselTracking.IsVesselManaged(__0.id);
            bool callerIsPre = VesselRectifier.IsCallerPhysicsRangeExtender();

            bool blocked = isManaged && callerIsPre;
            if (blocked)
                VerboseLogging.Log($"Blocked PRE switch for {__0.vesselName}.");

            return !blocked;
        }
    }

    // Prevents PRE from lifting managed vessels (overload 1).
    [HarmonyPatch(typeof(Vessel), nameof(Vessel.SetPosition), new Type[] { typeof(Vector3) })]
    internal static class PreventPreSetPositionPatch1
    {
        [HarmonyPrefix]
        private static bool Prefix(Vessel __instance)
        {
            if (__instance == null)
                return true;

            if (
                VesselTracking.IsVesselManaged(__instance.id)
                && VesselRectifier.IsCallerPhysicsRangeExtender()
            )
            {
                VerboseLogging.Log($"Blocked PRE from lifting {__instance.vesselName}.");
                return false; // Skips the method.
            }

            return true;
        }
    }

    // Prevents PRE from lifting managed vessels (overload 2).
    [HarmonyPatch(
        typeof(Vessel),
        nameof(Vessel.SetPosition),
        new Type[] { typeof(Vector3), typeof(bool) }
    )]
    internal static class PreventPreSetPositionPatch2
    {
        [HarmonyPrefix]
        private static bool Prefix(Vessel __instance)
        {
            if (__instance == null)
                return true;

            if (
                VesselTracking.IsVesselManaged(__instance.id)
                && VesselRectifier.IsCallerPhysicsRangeExtender()
            )
            {
                VerboseLogging.Log($"Blocked PRE from lifting {__instance.vesselName}.");
                return false;
            }

            return true;
        }
    }

    // Suppresses PRE screen messages triggered by managed vessels.
    [HarmonyPatch(
        typeof(ScreenMessages),
        nameof(ScreenMessages.PostScreenMessage),
        new Type[] { typeof(ScreenMessage) }
    )]
    internal static class SuppressPreScreenMessagePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ScreenMessage message)
        {
            if (message == null || string.IsNullOrEmpty(message.message))
                return true;

            if (!VesselRectifier.IsCallerPhysicsRangeExtender())
                return true;

            if (FlightGlobals.Vessels != null)
            {
                for (int i = FlightGlobals.Vessels.Count - 1; i >= 0; i--)
                {
                    Vessel v = FlightGlobals.Vessels[i];
                    if (
                        v != null
                        && VesselTracking.IsVesselManaged(v.id)
                        && message.message.Contains(v.vesselName)
                    )
                    {
                        VerboseLogging.Log($"Suppressed PRE message: {message.message}");
                        return false;
                    }
                }
            }

            return true;
        }
    }

    // Suppresses PRE string messages triggered by managed vessels.
    [HarmonyPatch(
        typeof(ScreenMessages),
        nameof(ScreenMessages.PostScreenMessage),
        new Type[] { typeof(string), typeof(float), typeof(ScreenMessageStyle) }
    )]
    internal static class SuppressPreStringMessagePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(string message)
        {
            if (string.IsNullOrEmpty(message))
                return true;

            if (!VesselRectifier.IsCallerPhysicsRangeExtender())
                return true;

            if (FlightGlobals.Vessels != null)
            {
                for (int i = FlightGlobals.Vessels.Count - 1; i >= 0; i--)
                {
                    Vessel v = FlightGlobals.Vessels[i];
                    if (
                        v != null
                        && VesselTracking.IsVesselManaged(v.id)
                        && message.Contains(v.vesselName)
                    )
                    {
                        VerboseLogging.Log($"Suppressed PRE string message: {message}");
                        return false;
                    }
                }
            }

            return true;
        }
    }

    // Suppresses PRE string/bool messages triggered by managed vessels.
    [HarmonyPatch(
        typeof(ScreenMessages),
        nameof(ScreenMessages.PostScreenMessage),
        new Type[] { typeof(string), typeof(float), typeof(ScreenMessageStyle), typeof(bool) }
    )]
    internal static class SuppressPreStringBoolMessagePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(string message)
        {
            if (string.IsNullOrEmpty(message))
                return true;

            if (!VesselRectifier.IsCallerPhysicsRangeExtender())
                return true;

            if (FlightGlobals.Vessels != null)
            {
                for (int i = FlightGlobals.Vessels.Count - 1; i >= 0; i--)
                {
                    Vessel v = FlightGlobals.Vessels[i];
                    if (
                        v != null
                        && VesselTracking.IsVesselManaged(v.id)
                        && message.Contains(v.vesselName)
                    )
                    {
                        VerboseLogging.Log($"Suppressed PRE string/bool message: {message}");
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
