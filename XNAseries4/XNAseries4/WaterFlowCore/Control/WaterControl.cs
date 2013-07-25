using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaterFlowSim.Control;

namespace WaterFlowSim.WaterFlowCore.Control
{
    public class WaterControl
    {
        public WaterControl(GeometryAndSettings geoAndSettings)
        {
            _geometryAndSettings = geoAndSettings;
        }
        public GeometryAndSettings _geometryAndSettings;

        public void actionDrainWaterAll()
        {
            for (int i = 0; i < _geometryAndSettings.landVertices.Length; i++)
            {
                _geometryAndSettings.waterValueModel[i].waterValue -= _geometryAndSettings.globalEmitterStrength;
            }
        }

        public void actionResetState()
        {
            _geometryAndSettings.autoEmmiter = false;

            for (int x = 0; x < _geometryAndSettings.terrainWidth; x++)
            {
                for (int y = 0; y < _geometryAndSettings.terrainLength; y++)
                {
                    int cellIndex = x + y * _geometryAndSettings.terrainWidth;
                    _geometryAndSettings.landVertices[cellIndex].Position.Y = _geometryAndSettings.heightData[x, y];
                }
            }

            _geometryAndSettings.device.SetVertexBuffer(null);
            _geometryAndSettings.landVertexBuffer.SetData(_geometryAndSettings.landVertices);
        }


        public void actionEmitWaterAll()
        {
            for (int i = 0; i < _geometryAndSettings.landVertices.Length; i++)
            {
                _geometryAndSettings.waterValueModel[i].waterValue += _geometryAndSettings.globalEmitterStrength;
            }
        }
        public void actionEmitWaterCursor()
        {

            _geometryAndSettings.autoEmmiter = true;


            int cursorSlot = _geometryAndSettings.findCursor();
            _geometryAndSettings.waterValueModel[cursorSlot].waterValue += _geometryAndSettings.cursorEmitterStrength;
            _geometryAndSettings.waterValueModel[cursorSlot].saturationValue = Erosion.INITIAL_SATURATION;
        }
        public void actionDrainWaterCursor()
        {
            int cursorSlot = _geometryAndSettings.findCursor();
            _geometryAndSettings.waterValueModel[cursorSlot].waterValue -= _geometryAndSettings.cursorEmitterStrength;
        }
        public void actionEliminateWater()
        {
            for (int i = 0; i < _geometryAndSettings.waterValueModel.Length; i++)
            {
                _geometryAndSettings.waterValueModel[i].waterValue = 0;
                _geometryAndSettings.waterValueModel[i].saturationValue = Erosion.INITIAL_SATURATION;
            }
        }
        public void actionTidalWave()
        {
            for (int i = 0; i < _geometryAndSettings.terrainWidth; i++)
            {
                _geometryAndSettings.waterValueModel[i].waterValue += _geometryAndSettings.globalEmitterStrength * _geometryAndSettings.terrainLength;
            }
        }
    }
}
