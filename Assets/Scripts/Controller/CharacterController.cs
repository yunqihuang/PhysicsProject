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
        public bool attack;
        public bool dropEquipment;
        
        public Vector3 moveDirection;
        public Vector3 faceDirection;
        public float faceAngle;
        

        
        public bool grabLeft, grabRight;
        public float punchTimer;
        
        protected virtual void UpdateInput()
        {
            
        }
        
        
        private void Update()
        {
            UpdateInput();
        }
    }
}