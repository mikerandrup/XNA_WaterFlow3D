using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XNAseries4
{

    class Erosion
    {
        private static float MAX_SATURATION = 0.25f;
        public static float INITIAL_SATURATION = MAX_SATURATION * 0.0f;
        
        private const float HEIGHT_MOVED_DISSOLVE_THRESHOLD = 0.75f;
        private const float HEIGHT_MOVED_DEPOSIT_THRESHOLD = 0.15f;
        private const float MINIMUM_LAND_VALUE = 0.0f;

        private static float BASE_STEP_AMOUNT = 0.025f;
        private static float DISSOLVE_STEP_AMOUNT = BASE_STEP_AMOUNT * 0.5f;
        private static float DEPOSIT_STEP_AMOUNT = BASE_STEP_AMOUNT * 1.1f;

        public enum ErosionMode
        {
            Acquire,
            Hold,
            Deposit
        } 

        public static ErosionMode PerformErosion(ref float waterValue, ref float saturationValue, ref float landValue, float heightJustMoved = 0f)
        {
            var retVal = ErosionMode.Hold;

            if (saturationValue < MAX_SATURATION && heightJustMoved > HEIGHT_MOVED_DISSOLVE_THRESHOLD && landValue >= MINIMUM_LAND_VALUE)
            {
                // acquire land
                landValue -= DISSOLVE_STEP_AMOUNT;
                saturationValue += DISSOLVE_STEP_AMOUNT;
                retVal = ErosionMode.Acquire;
            }
            else if (saturationValue > 0 && heightJustMoved < HEIGHT_MOVED_DEPOSIT_THRESHOLD)
            {
                float depositAmount = (saturationValue < DEPOSIT_STEP_AMOUNT) ? saturationValue : DEPOSIT_STEP_AMOUNT;

                // deposit land
                saturationValue -= depositAmount;
                landValue += depositAmount;
                retVal = ErosionMode.Deposit;
            }

            return retVal;
        }
    }
}
