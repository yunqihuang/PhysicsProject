using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActiveRagdoll
{
    public class CharacterController : MonoBehaviour
    {
        public float punchHoldTime;
        public bool moving;
        public bool movingBack;
        public bool accelerating;
        public bool jumping;
        public Vector3 moveDirection;

        public float faceAngle;
        
        public bool readyPunchingLeft,readyPunchingRight;
        public bool punchingLeft,punchingRight;
        public float punchingLeftTimer, punchingRightTimer;
        public bool grabLeft,grabRight;

        protected virtual void UpdateInput()
        {
            
        }

        private void Update()
        {
            UpdateInput();
        }
    }
}