using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaterFlowSim.Control;

namespace WaterFlowSim.WaterFlowCore.Control
{
    public class LandControl
    {
        private GeometryAndSettings _geometryAndSettings;

        public LandControl(GeometryAndSettings geometryAndSettings)
        {
            _geometryAndSettings = geometryAndSettings;
        }

        public void actionScaleLandUp()
        {
            for (int i = 0; i < _geometryAndSettings.landVertices.Length; i++)
            {
                _geometryAndSettings.landVertices[i].Position.Y *= _geometryAndSettings.landMultStrength; // scale appropriate to terrain
            }
            _geometryAndSettings.device.SetVertexBuffer(null);
            _geometryAndSettings.landVertexBuffer.SetData(_geometryAndSettings.landVertices);
        }

        public void actionScaleLandDown()
        {
            for (int i = 0; i < _geometryAndSettings.landVertices.Length; i++)
            {
                _geometryAndSettings.landVertices[i].Position.Y *= 1 / _geometryAndSettings.landMultStrength; //reciprocal, yo!
            }
            _geometryAndSettings.device.SetVertexBuffer(null);
            _geometryAndSettings.landVertexBuffer.SetData(_geometryAndSettings.landVertices);
        }

        public void actionEmitLandCursor()
        {
            int cursorSlot = _geometryAndSettings.findCursor();
            float effectiveEmitterStrength = _geometryAndSettings.cursorEmitterStrength * 0.003f;

            _geometryAndSettings.landVertices[cursorSlot].Position.Y += effectiveEmitterStrength; // scale appropriate to terrain
            _geometryAndSettings.landVertexBuffer.SetData(_geometryAndSettings.landVertices);
        }
    }
}
