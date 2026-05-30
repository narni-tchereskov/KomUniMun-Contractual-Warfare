using System;
using UnityEngine;

namespace KomUniMunVesselRectifier
{
    internal static class Positioning
    {
        // Sets vessel situation as FLYING to prevent wacky physics.
        public static void MarkVesselAsFlying(Vessel vessel)
        {
            VerboseLogging.Log($"Marking vessel {vessel.vesselName} as FLYING.");
            ProtoVessel protoVessel = vessel.protoVessel;

            // This shouldn't be necessary, right? It works so...
            if (protoVessel != null)
            {
                protoVessel.situation = Vessel.Situations.FLYING;
                protoVessel.landed = false;
                protoVessel.splashed = false;
            }

            vessel.situation = Vessel.Situations.FLYING;
            vessel.Landed = false;
            vessel.Splashed = false;
        }

        // Calculates terrain elevation to get AGL altitude.
        private static double GetTerrainRadius(Vessel vessel, Vector3d worldPosition)
        {
            if (vessel.mainBody.pqsController == null)
                return vessel.mainBody.Radius;

            double latRad = vessel.mainBody.GetLatitude(worldPosition) * Math.PI / 180.0;
            double lonRad = vessel.mainBody.GetLongitude(worldPosition) * Math.PI / 180.0;

            Vector3d unitRadial = new Vector3d(
                Math.Cos(latRad) * Math.Cos(lonRad),
                Math.Sin(latRad),
                Math.Cos(latRad) * Math.Sin(lonRad)
            );

            return Math.Max(
                vessel.mainBody.pqsController.GetSurfaceHeight(unitRadial),
                vessel.mainBody.Radius
            );
        }

        // Repositions vessel to specific altitude with velocity. Yes this is necessary.
        public static bool ApplySafeSpawnPosition(Vessel vessel)
        {
            if (vessel?.mainBody?.bodyTransform == null)
                return false;

            Vector3d worldPosition = vessel.GetWorldPos3D();

            if (double.IsNaN(worldPosition.x))
                return false;

            Vector3d radialVector = worldPosition - vessel.mainBody.position;
            double radialMagnitude = radialVector.magnitude;

            if (
                radialMagnitude < vessel.mainBody.Radius * 0.5
                || radialMagnitude > vessel.mainBody.Radius * 10
            )
                return false;

            VerboseLogging.Log($"Applying transform to {vessel.vesselName}.");

            Vector3d upDirection = radialVector.normalized;
            Vector3d polarAxis = vessel.mainBody.bodyTransform.up;
            Vector3d northTangent = (
                polarAxis - Vector3d.Dot(polarAxis, upDirection) * upDirection
            ).normalized;

            double terrainRadius = GetTerrainRadius(vessel, worldPosition);

            float altitudeAboveGround = UnityEngine.Random.Range(Settings.MinAGL, Settings.MaxAGL);
            Vector3d newPosition =
                vessel.mainBody.position + upDirection * (terrainRadius + altitudeAboveGround);

            // Determine relative velocity vector based on vessel type
            Vector3d relativeVelocity = Vector3d.zero;

            if (vessel.vesselName.IsContractHelicopter())
            {
                relativeVelocity = upDirection * Settings.HelicopterSpawnSpeed;
            }
            else if (vessel.vesselName.IsContractAircraft())
            {
                relativeVelocity = northTangent * Settings.AircraftSpawnSpeed;
            }
            else
            {
                relativeVelocity = northTangent * Settings.SpawnSpeed;
            }

            Vector3d newVelocity = vessel.mainBody.getRFrmVel(newPosition) + relativeVelocity;

            vessel.SetPosition(newPosition);
            vessel.SetWorldVelocity(newVelocity);
            vessel.SafeIgnoreGForces(Settings.GHardeningDuration);
            vessel.orbit?.UpdateFromStateVectors(
                newPosition - vessel.mainBody.position,
                newVelocity,
                vessel.mainBody,
                Planetarium.GetUniversalTime()
            );

            return true;
        }

        // Forces all parts of the vessel to unpack so physics can resume. Kludgy but ok.
        public static void ForceUnpackAllParts(Vessel vessel)
        {
            if (
                vessel == null
                || (
                    !vessel.vesselName.IsContractAircraft()
                    && !vessel.vesselName.IsContractHelicopter()
                )
            )
                return;

            VerboseLogging.Log($"Forcing unpack on {vessel.vesselName}.");
            if (vessel.parts != null)
            {
                for (int i = 0; i < vessel.parts.Count; i++)
                {
                    try
                    {
                        vessel.parts[i]?.Unpack();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(
                            $"[KUM] Failed to unpack part {vessel.parts[i]?.partName} on {vessel.vesselName}: {ex.Message}"
                        );
                    }
                }
            }
            vessel.packed = false;
        }
    }
}
