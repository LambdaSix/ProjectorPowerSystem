using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace ProjectorPowerToggle
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Projector),false)]
    public class MyProjectorLogicPowerSystem : MyGameLogicComponent
    {
        private MyObjectBuilder_EntityBase _objectBuilder;
        private bool _isInitialized;

        private Sandbox.ModAPI.Ingame.IMyProjector _self;
        private MyResourceSinkComponent _sink;

        private float BasePowerRequirement;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
            _self = (IMyProjector)Entity;
            BasePowerRequirement = MyDefinitionManager.Static.Definitions
                .GetDefinition<MyProjectorDefinition>(_self.BlockDefinition)
                .RequiredPowerInput;
        }

        public override void UpdateOnceBeforeFrame()
        {
            try
            {
                _sink = _sink ?? Entity.Components.Get<MyResourceSinkComponent>();

                if (_sink != null)
                {
                    _sink.SetRequiredInputFuncByType(MyResourceDistributorComponent.ElectricityId, CalculateRequiredPowerInput);
                    _sink.Update();
                }
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("[ Error in " + GetType().FullName + ": " + e.Message + " ]", 10000, MyFontEnum.Red);
                MyLog.Default.WriteLine(e);
            }
        }

        private void ModifyDetailedInfo(IMyTerminalBlock block, StringBuilder builder)
        {
            var currentUsage = PowerFormat(block.ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId));
            var maxUsage = PowerFormat(block.ResourceSink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId));

            builder.AppendLine($"Power Usage: {currentUsage}/{maxUsage}");
        }

        public static string PowerFormat(float MW)
        {
            if (MW >= 1000000000)
                return Number(MW / 1000000000).Append(" PetaWatts").ToString();

            if (MW >= 1000000)
                return Number(MW / 1000000).Append(" TerraWatts").ToString();

            if (MW >= 1000)
                return Number(MW / 1000).Append(" GigaWatts").ToString();

            if (MW >= 1)
                return Number(MW).Append(" MW").ToString();

            if (MW >= 0.001)
                return Number(MW * 1000f).Append(" kW").ToString();

            return Number(MW * 1000000f).Append(" W").ToString();
        }

        public static StringBuilder Number(float value)
        {
            return new StringBuilder().AppendFormat("{0:###,###,###,###,###,##0.##}", value);
        }

        private float CalculateRequiredPowerInput()
        {
            if (_self.Enabled)
            {
                var projector = (IMyProjector) _self;
                if (projector.IsProjecting)
                {
                    var scalar = ComputeScalar(projector.ProjectedGrid);
                    return BasePowerRequirement * scalar;
                }
            }

            // 0.0001MW or 100W
            return 1E-04f;
        }

        private float ComputeScalar(IMyCubeGrid projectedGrid)
        {
            // TODO: Scale consumption based on block count + gridSize
            var blockList = new List<IMySlimBlock>();
            projectedGrid.GetBlocks(blockList);

            var gridSize = projectedGrid.GridSizeEnum;
            var blockCount = blockList.Count;

            // 0.1 (100kW) per block for Large Grid and 1kW per block for small
            var baseScalar = (gridSize == MyCubeSize.Large) ? 0.1f : 0.001f;
            return baseScalar * blockCount;
        }
    }
}
