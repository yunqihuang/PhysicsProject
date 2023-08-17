using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace ActiveRagdoll
{
    public class SkeletonConfig : MonoBehaviour
    {
        [Header("AnimatorSkeleton")]
        public Transform Hips;
        public Transform Spine;
        public Transform Chest;
        public Transform UpperChest;
        public Transform LeftShoulder;
        public Transform RightShoulder;
        public Transform Neck;
        public Transform Head;
        public Transform LeftArm;
        public Transform RightArm;
        public Transform LeftForeArm;
        public Transform RightForeArm;
        public Transform LeftHand;
        public Transform RightHand;
        public Transform LeftUpLeg;
        public Transform RightUpLeg;
        public Transform LeftLeg;
        public Transform RightLeg;
        public Transform LeftFoot;
        public Transform RightFoot;
        
        
        private void Awake()
        {
            
        }

        private void Update()
        {
            
        }
    }
}