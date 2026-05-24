using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace KomUniMunVesselRectifier
{
    // Resolves Harmony target methods at load time.
    internal static class PatchTargets
    {
        private const BindingFlags AllInstanceOrStatic =
            BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.Public
            | BindingFlags.NonPublic;

        internal static MethodBase ByName(Type type, string name)
        {
            return AccessTools.Method(type, name);
        }

        // Enumerates every overload of a method whose first parameter matches firstParameter.
        internal static IEnumerable<MethodBase> ByNameFirstParam(
            Type type,
            string name,
            Type firstParameter
        )
        {
            MethodInfo[] candidates = type.GetMethods(AllInstanceOrStatic);
            for (int i = 0; i < candidates.Length; i++)
            {
                MethodInfo m = candidates[i];

                if (m.Name != name)
                    continue;

                ParameterInfo[] parameters = m.GetParameters();

                if (parameters.Length == 0)
                    continue;

                if (parameters[0].ParameterType != firstParameter)
                    continue;

                yield return m;
            }
        }

        // Enumerates every overload of a method by name.
        internal static IEnumerable<MethodBase> ByNameAny(Type type, string name)
        {
            MethodInfo[] candidates = type.GetMethods(AllInstanceOrStatic);
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i].Name == name)
                    yield return candidates[i];
            }
        }
    }

    // Intercepts unrailing to delay physics or apply safe spawn position.
    [HarmonyPatch]
    internal static class GoOffRailsPatch
    {
        private static MethodBase TargetMethod()
        {
            return PatchTargets.ByName(typeof(Vessel), nameof(Vessel.GoOffRails));
        }

        private static bool Prepare()
        {
            return TargetMethod() != null;
        }

        [HarmonyPrefix]
        private static bool Prefix(Vessel __instance, out bool __state)
        {
            __state = false;

            if (__instance == null)
                return true;

            if (!VesselTracking.IsVesselManaged(__instance.id))
                return true;

            if (__instance.vesselType == VesselType.Debris)
                return true;

            if (!VesselRectifier.IsSceneSettled)
            {
                __state = true;
                return false;
            }

            if (!__instance.vesselName.IsContractAircraft())
                return true;

            if (!__instance.IsFullyInitialized())
                return true;

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

            return true;
        }

        // Applies combat states and forces part unpacking.
        [HarmonyPostfix]
        private static void Postfix(Vessel __instance, bool __state)
        {
            if (__state)
                return;

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
    [HarmonyPatch]
    internal static class GoOnRailsPatch
    {
        private static MethodBase TargetMethod()
        {
            return PatchTargets.ByName(typeof(Vessel), nameof(Vessel.GoOnRails));
        }

        private static bool Prepare()
        {
            return TargetMethod() != null;
        }

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
    [HarmonyPatch]
    internal static class VesselMakeActivePatch
    {
        private static MethodBase TargetMethod()
        {
            return PatchTargets.ByName(typeof(Vessel), "MakeActive");
        }

        private static bool Prepare()
        {
            return TargetMethod() != null;
        }

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

    // Stops PRE camera changes for aircraft to avoid scene load & physics issues.
    [HarmonyPatch]
    internal static class PreventPreForceSetActiveVesselPatch
    {
        private static MethodBase TargetMethod()
        {
            return PatchTargets.ByName(
                typeof(FlightGlobals),
                nameof(FlightGlobals.ForceSetActiveVessel)
            );
        }

        private static bool Prepare()
        {
            return TargetMethod() != null;
        }

        [HarmonyPrefix]
        private static bool Prefix(Vessel __0)
        {
            if (__0 == null)
                return true;

            bool isManaged = VesselTracking.IsVesselManaged(__0.id);
            bool isAircraft = __0.vesselName.IsContractAircraft();
            bool callerIsPre = VesselRectifier.IsCallerPhysicsRangeExtender();
            bool blocked = isManaged && isAircraft && callerIsPre;

            if (blocked)
                VerboseLogging.Log($"Blocked PRE switch for {__0.vesselName}.");

            return !blocked;
        }
    }

    // Prevents PRE from lifting managed vessels.
    [HarmonyPatch]
    internal static class PreventPreSetPositionPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return PatchTargets.ByNameAny(typeof(Vessel), nameof(Vessel.SetPosition));
        }

        private static bool Prepare()
        {
            return TargetMethods().Any();
        }

        [HarmonyPrefix]
        private static bool Prefix(Vessel __instance)
        {
            if (__instance == null)
                return true;

            if (
                VesselTracking.IsVesselManaged(__instance.id)
                && __instance.vesselName.IsContractAircraft()
                && VesselRectifier.IsCallerPhysicsRangeExtender()
            )
            {
                VerboseLogging.Log($"Blocked PRE from lifting {__instance.vesselName}.");
                return false; // Skips the method.
            }

            return true;
        }
    }

    // Suppresses PRE-triggered screen messages.
    [HarmonyPatch]
    internal static class SuppressPreScreenMessagesPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return PatchTargets.ByNameAny(
                typeof(ScreenMessages),
                nameof(ScreenMessages.PostScreenMessage)
            );
        }

        private static bool Prepare()
        {
            return TargetMethods().Any();
        }

        [HarmonyPrefix]
        private static bool Prefix(object[] __args)
        {
            string text = ExtractMessageText(__args);
            if (string.IsNullOrEmpty(text))
                return true;

            if (!VesselRectifier.IsCallerPhysicsRangeExtender())
                return true;

            if (FlightGlobals.Vessels == null)
                return true;

            for (int i = FlightGlobals.Vessels.Count - 1; i >= 0; i--)
            {
                Vessel v = FlightGlobals.Vessels[i];
                if (v == null)
                    continue;
                if (!VesselTracking.IsVesselManaged(v.id))
                    continue;
                if (text.Contains(v.vesselName))
                {
                    VerboseLogging.Log($"Suppressed PRE message: {text}");
                    return false;
                }
            }

            return true;
        }

        private static string ExtractMessageText(object[] args)
        {
            if (args == null || args.Length == 0)
                return null;

            object first = args[0];
            if (first is string asString)
                return asString;
            if (first is ScreenMessage asMessage)
                return asMessage?.message;
            return null;
        }
    }
}
