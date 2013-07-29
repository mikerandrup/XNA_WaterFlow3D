using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaterFlowSim.WaterFlowCore.Control
{
    class RenderControl
    {
        private GeometryAndSettings _geometryAndSettings;

        public RenderControl(GeometryAndSettings geoAndSettings)
        {
            _geometryAndSettings = geoAndSettings;
        }

        public void toggleWireFramesOnly()
        {
            if (_geometryAndSettings.WireFramesOnly) _geometryAndSettings.WireFramesOnly = false;
            else _geometryAndSettings.WireFramesOnly = true;
        }
    }
}
