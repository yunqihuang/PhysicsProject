using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ActiveRagdoll
{
    public class StepData : MonoBehaviour
    {
        [Serializable]
        public struct StepInfo
        {
            public float StepDuration;
            public AnimationCurve UpperLegCurve;
            public AnimationCurve LowerLegCurve;
            public AnimationCurve OtherUpperLegCurve;
            public AnimationCurve OtherLowerLegCurve;
            public float UpperLegTargetAngle;
            public float LowerLegTargetAngle;
            public float OtherUpperLegTargetAngle;
            public float OtherLowerLegTargetAngle;
        }
        [Header("Forwards")] 
        public StepInfo forwardStepInfo;
        [Header("Backwards")] 
        public StepInfo backwardStepInfo;
    }
   
}

