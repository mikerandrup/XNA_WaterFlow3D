using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaterFlowSim.Control
{
    public static class CursorLocate
    {
        public static void Initialize(int xHigh = 100, int zHigh = 100)
        {
            xMin = 0;
            xMax = xHigh;
            zMin = 0;
            zMax = zHigh;

            ResetToCenter();
        }

        public static void ResetToCenter()
        {
            xLoc = FinderCenterX();
            zLoc = FinderCenterZ();
        }

        public static int xLoc { get; set; }
        public static int zLoc { get; set; }

        private static int xMin, xMax, zMin, zMax;

        public static int getX()
        {
            return xLoc;
        }
        public static int getZ()
        {
            return zLoc;
        }

        public static void AlterX(int delta)
        {
            int newVal = xLoc += delta;
            if (newVal < xMin)
                newVal = xMin;
            if (newVal > xMax)
                newVal = xMin;
            xLoc = newVal;
        }

        public static void AlterZ(int delta)
        {
            int newVal = zLoc += delta;
            if (newVal < zMin)
                newVal = zMin;
            if (newVal > zMax)
                newVal = zMin;
            zLoc = newVal;
        }

        public static int FinderCenterX()
        {
            int delta = xMax - xMin;
            int center = xMin + delta / 2;

            return center;
        }

        public static int FinderCenterZ()
        {
            int delta = zMax - zMin;
            int center = zMin + delta / 2;

            return center;
        }
    }
}
