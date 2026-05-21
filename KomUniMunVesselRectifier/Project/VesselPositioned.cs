using System;
using System.Collections.Generic;
using UnityEngine;

namespace KomUniMunVesselRectifier
{
    // Prevents re-positioning of already positioned vessels during save loading.
    [KSPScenario(
        ScenarioCreationOptions.AddToAllGames,
        GameScenes.FLIGHT,
        GameScenes.SPACECENTER,
        GameScenes.TRACKSTATION
    )]
    public class VesselPositioned : ScenarioModule
    {
        private readonly HashSet<Guid> _positionedIds = new HashSet<Guid>();

        public static VesselPositioned Instance;

        // Checks if a vessel has already been positioned in this save.
        public bool HasPositioned(Guid id) => _positionedIds.Contains(id);

        // Marks a vessel as positioned to not move it again.
        public void AddPositioned(Guid id)
        {
            if (_positionedIds.Add(id))
                VerboseLogging.Log($"Added vessel {id} to list.");
        }

        // Removes a vessel from the positioned list.
        public void RemovePositioned(Guid id)
        {
            if (_positionedIds.Remove(id))
                VerboseLogging.Log($"Removed vessel {id} from list.");
        }

        // Initializes the scenario instance.
        public override void OnAwake() => Instance = this;

        // Loads the positioned vessels data from the save file.
        public override void OnLoad(ConfigNode node)
        {
            _positionedIds.Clear();
            if (node == null)
                return;

            foreach (string raw in node.GetValues("id"))
            {
                if (Guid.TryParse(raw, out Guid parsedId))
                    _positionedIds.Add(parsedId);
            }

            VerboseLogging.Log($"Scenario loaded {_positionedIds.Count} vessel(s).");
        }

        // Saves the positioned vessels data to the save file.
        public override void OnSave(ConfigNode node)
        {
            if (node == null)
                return;

            foreach (Guid id in _positionedIds)
                node.AddValue("id", id.ToString());

            VerboseLogging.Log($"Scenario saved {_positionedIds.Count} vessel(s).");
        }
    }
}
