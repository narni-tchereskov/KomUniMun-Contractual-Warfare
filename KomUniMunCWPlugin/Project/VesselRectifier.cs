using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Debug = UnityEngine.Debug;

[assembly: KSPAssembly("KomUniMunVesselRectifier", 1, 0)]

namespace KomUniMunVesselRectifier
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class VesselRectifier : MonoBehaviour
    {
        private const string HarmonyInstanceId = "com.kum.vesselrectifier";

        private static Harmony _harmonyInstance;
        private static int _addonInstanceCount;

        private static float _addonAwakeTime;
        public static bool IsSceneSettled =>
            Time.time - _addonAwakeTime > Settings.SceneSettleDuration;

        // Walks the call stack to see whether PRE triggered the current chain.
        internal static bool IsCallerPhysicsRangeExtender()
        {
            try
            {
                StackTrace stackTrace = new StackTrace(2);

                for (int i = 0; i < stackTrace.FrameCount; i++)
                {
                    StackFrame frame = stackTrace.GetFrame(i);

                    if (frame == null)
                        continue;

                    MethodBase method = frame.GetMethod();

                    if (method == null)
                        continue;

                    Type declaringType = method.DeclaringType;

                    if (declaringType == null)
                        continue;

                    if (
                        declaringType.Namespace != null
                        && declaringType.Namespace.StartsWith(
                            "PhysicsRangeExtender",
                            StringComparison.Ordinal
                        )
                    )
                        return true;

                    if (declaringType.Assembly.GetName().Name == "PhysicsRangeExtender")
                        return true;
                }
            }
            catch
            {
                return Environment.StackTrace.Contains("PhysicsRangeExtender");
            }

            return false;
        }

        // Initializes the mod, registers events, and applies Harmony patches.
        private void Awake()
        {
            _addonInstanceCount++;
            _addonAwakeTime = Time.time;

            BdaIntegration.DetectPresence();

            GameEvents.onVesselCreate.Add(OnVesselCreated);
            GameEvents.onVesselDestroy.Add(OnVesselDestroyed);

            Debug.Log($"[KUM] Awake VerboseLogs={Settings.VerboseLogs}");

            if (_harmonyInstance == null)
            {
                _harmonyInstance = new Harmony(HarmonyInstanceId);
                ApplyHarmonyPatches();
            }
        }

        // Avoids crashing all patches if one fails.
        private static void ApplyHarmonyPatches()
        {
            Type[] types;
            try
            {
                types = typeof(VesselRectifier).Assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray();
            }

            int applied = 0;
            int skipped = 0;
            int failed = 0;

            foreach (Type type in types)
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length == 0)
                    continue;

                try
                {
                    var result = new PatchClassProcessor(_harmonyInstance, type).Patch();
                    if (result != null && result.Count > 0)
                    {
                        applied++;
                        VerboseLogging.Log(
                            $"Applied Harmony patch '{type.Name}' ({result.Count} target(s))."
                        );
                    }
                    else
                    {
                        skipped++;
                        Debug.LogWarning(
                            $"[KUM] Skipped Harmony patch '{type.Name}': target method not found on this KSP version."
                        );
                    }
                }
                catch (Exception exception)
                {
                    failed++;
                    Debug.LogWarning(
                        $"[KUM] Failed Harmony patch '{type.Name}': "
                            + $"{exception.GetBaseException().Message}"
                    );
                }
            }

            Debug.Log(
                $"[KUM] Harmony patches: {applied} applied, {skipped} skipped, {failed} failed."
            );
        }

        // Sweeps and registers any managed vessels.
        private void Start()
        {
            if (FlightGlobals.Vessels == null)
                return;

            VerboseLogging.Log($"Sweep found {FlightGlobals.Vessels.Count} vessels.");
            for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
                VesselTracking.TryRegisterVessel(FlightGlobals.Vessels[i]);
        }

        // Cleans all effects when mod is destroyed.
        private void OnDestroy()
        {
            GameEvents.onVesselCreate.Remove(OnVesselCreated);
            GameEvents.onVesselDestroy.Remove(OnVesselDestroyed);

            _addonInstanceCount = Math.Max(0, _addonInstanceCount - 1);

            if (_addonInstanceCount > 0)
                return;

            try
            {
                _harmonyInstance?.UnpatchAll(HarmonyInstanceId);
                VerboseLogging.Log("Harmony patches removed.");
            }
            catch { }

            _harmonyInstance = null;
            VesselTracking.ClearAll();
            BdaIntegration.ClearReflectionCache();
        }

        // Handles the creation and registration of a new vessel.
        private void OnVesselCreated(Vessel vessel)
        {
            VerboseLogging.Log($"OnVesselCreated triggered for {vessel?.vesselName}");
            VesselTracking.TryRegisterVessel(vessel);
        }

        // Handles the destruction of a vessel and removes it from tracking.
        private void OnVesselDestroyed(Vessel vessel)
        {
            if (vessel == null)
                return;

            VerboseLogging.Log($"OnVesselDestroyed triggered for {vessel.vesselName}");
            VesselTracking.StopTracking(vessel.id);
            VesselPositioned.Instance?.RemovePositioned(vessel.id);
        }

        // Force every issue to fix itself through brute force.
        private void Update()
        {
            if (FlightGlobals.Vessels == null)
                return;

            bool sceneSettled = IsSceneSettled;

            // Iterate backwards to avoid out-of-bounds.
            for (int i = FlightGlobals.Vessels.Count - 1; i >= 0; i--)
            {
                Vessel vessel = FlightGlobals.Vessels[i];

                // NullPo begone, yes it needed to be said here and nowhere else.
                if (
                    vessel == null
                    || vessel.gameObject == null
                    || vessel.vesselType == VesselType.Debris
                )
                    continue;

                if (!VesselTracking.IsVesselManaged(vessel.id))
                    continue;

                ProcessVessel(vessel, sceneSettled);
            }
        }

        // Prepares vessel during spawn for combat and banishes kraken.
        private void ProcessVessel(Vessel vessel, bool sceneSettled)
        {
            if (vessel == null)
                return;

            if (!VesselTracking.IsVesselManaged(vessel.id))
            {
                VesselTracking.TryRegisterVessel(vessel);
                if (!VesselTracking.IsVesselManaged(vessel.id))
                    return;
            }

            if (vessel.vesselType == VesselType.Debris)
            {
                VesselTracking.StopTracking(vessel.id);
                return;
            }

            VesselTrack tracking = VesselTracking.GetVesselTracking(vessel.id);
            if (tracking == null)
                return;

            if (vessel.loaded && !vessel.packed)
            {
                if (
                    vessel.vesselName.IsContractAircraft()
                    || vessel.vesselName.IsContractHelicopter()
                )
                    vessel.SafeIgnoreGForces(Settings.GHardeningDuration);

                if (tracking.UnpackedAtTime < 0)
                {
                    tracking.UnpackedAtTime = Time.time;
                    VerboseLogging.Log($"Vessel {vessel.vesselName} unpacked.");
                }

                bool vesselSettled = (Time.time - tracking.UnpackedAtTime) > 1.0f;

                if (
                    !VesselTracking.VesselHasFlag(vessel.id, VesselFlags.CombatStateApplied)
                    && sceneSettled
                    && vesselSettled
                )
                {
                    VerboseLogging.Log($"Applying combat state to {vessel.vesselName}.");
                    BdaIntegration.ForceCombatState(vessel, tracking);
                    VesselTracking.SetVesselFlag(vessel.id, VesselFlags.CombatStateApplied);
                }
            }
            else
            {
                tracking.UnpackedAtTime = -1f;
                VesselTracking.ClearVesselFlag(vessel.id, VesselFlags.CombatStateApplied);
            }

            CheckAndUnrailVessel(vessel, sceneSettled);
        }

        // Evaluates if packed vessel needs to be forced to load.
        private void CheckAndUnrailVessel(Vessel vessel, bool sceneSettled)
        {
            if (!sceneSettled || !vessel.packed || !vessel.IsFullyInitialized())
                return;

            bool isPositioned = VesselPositioned.Instance?.HasPositioned(vessel.id) == true;

            if (isPositioned)
            {
                try
                {
                    vessel.GoOffRails();
                }
                catch { }
                return;
            }

            if (vessel.vesselName.IsContractAircraft() || vessel.vesselName.IsContractHelicopter())
            {
                try
                {
                    VerboseLogging.Log($"Loading new vessel {vessel.vesselName}.");
                    vessel.GoOffRails();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        $"[KUM] GoOffRails exception for {vessel.vesselName}: {ex.Message}"
                    );
                }
            }
        }
    }
}
