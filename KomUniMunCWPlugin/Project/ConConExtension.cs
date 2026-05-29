using System;
using ContractConfigurator;
using ContractConfigurator.Behaviour;
using UnityEngine;

namespace KomUniMunVesselRectifier
{
    public class KomUniMunSpawnPassengersFactory : SpawnPassengersFactory
    {
        public override bool Load(ConfigNode configNode)
        {
            string countStr = configNode.GetValue("count");

            if (configNode.HasValue("count"))
            {
                configNode.RemoveValue("count");
            }

            bool valid = base.Load(configNode);

            if (countStr != null)
            {
                configNode.AddValue("count", countStr);

                try
                {
                    valid &= ConfigNodeUtil.ParseValue<int>(
                        configNode,
                        "count",
                        x => this.count = x,
                        this,
                        1
                    );
                }
                catch (Exception ex)
                {
                    Debug.LogError(
                        $"[KUM] KomUniMunSpawnPassengersFactory failed to parse count: {ex.Message}"
                    );
                }
            }

            return valid;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            if (this.count <= 0)
            {
                Debug.Log(
                    $"[KUM] KomUniMunSpawnPassengers conditions resulted in count = {this.count}. Injecting No-Op behaviour."
                );
                return new KomUniMunNoOp();
            }

            return base.Generate(contract);
        }
    }

    public class KomUniMunNoOp : ContractBehaviour { }
}
