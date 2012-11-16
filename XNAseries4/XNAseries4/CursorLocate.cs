using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XNAseries4
{
    class CursorLocate
    {

        public CursorLocate(int xLow = 0, int xHigh = 100, int zLow = 0, int zHigh = 100)
        {
            xMin = xLow;
            xMax = xHigh;
            zMin = zLow;
            zMax = zHigh;

            ResetToCenter();
            ResetToCenter();
        }

        public void ResetToCenter()
        {
            xLoc = FinderCenterX();
            zLoc = FinderCenterZ();
        }

        public int xLoc { get; set; }
        public int zLoc { get; set; }

        private int xMin, xMax, zMin, zMax;

        public int getX()
        {
            return xLoc;
        }
        public int getZ()
        {
            return zLoc;
        }

        public void AlterX(int delta)
        {
            int newVal = xLoc += delta;
            if (newVal < xMin)
                newVal = xMin;
            if (newVal > xMax)
                newVal = xMin;
            xLoc = newVal;
        }

        public void AlterZ(int delta)
        {
            int newVal = zLoc += delta;
            if (newVal < zMin)
                newVal = zMin;
            if (newVal > zMax)
                newVal = zMin;
            zLoc = newVal;
        }

        public int FinderCenterX()
        {
            int delta = xMax - xMin;
            int center = xMin + delta / 2;

            return center;
        }

        public int FinderCenterZ()
        {
            int delta = zMax - zMin;
            int center = zMin + delta / 2;

            return center;
        }
    }
}
