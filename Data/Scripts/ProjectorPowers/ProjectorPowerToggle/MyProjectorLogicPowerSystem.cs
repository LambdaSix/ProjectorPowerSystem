using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace ProjectorPowerToggle
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Projector),false)]
    public class MyProjectorLogicPowerSystem : MyGameLogicComponent
    {
        private MyObjectBuilder_EntityBase _objectBuilder;
        private bool _isInitialized;

        private MyProjectorBase _self;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
            _self = (MyProjectorBase)Entity;

            // Add the "Is Jump Beacon" toggle

            _self.ResourceSink.Init(_self.BlockDefinition.ResourceSinkGroup, _self.BlockDefinition.RequiredPowerInput, CalculateRequiredPowerInput);
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();
            ModifyDetailedInfo(_self, _self.DetailedInfo);
        }

        private void ModifyDetailedInfo(IMyTerminalBlock block, StringBuilder builder)
        {
            var currentUsage = FormatPowerString(block.ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId));
            var maxUsage = FormatPowerString(block.ResourceSink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId));

            builder.AppendLine($"Power Usage: {currentUsage}/{maxUsage}");
        }

        private string FormatPowerString(float powerLevel)
        {
            if (powerLevel >= 1.0)
            {
                return $"{powerLevel}MW";
            } else if (powerLevel < 1.0 && powerLevel > 0.000_001)
            {
                return $"{powerLevel}kW";
            }
            else
            {
                return $"{powerLevel}W";
            }
        }

        private float CalculateRequiredPowerInput()
        {
            if (_self.Enabled)
            {
                var projector = (IMyProjector) _self;
                if (projector.IsProjecting)
                    return _self.BlockDefinition.RequiredPowerInput;
            }

            return 1E-04f;
        }
    }
}
