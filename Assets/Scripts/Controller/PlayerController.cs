using System;
using Unity.VisualScripting;
using UnityEngine;

namespace ActiveRagdoll
{
    public class PlayerController : CharacterController
    {
        public Camera cam;
        public float longPressTime = 0.5f;
        
        private float _leftMouseClickTime;
        private float _rightMouseClickTime;


        private void Start()
        {
            faceDirection = transform.forward;
        }

        protected override void UpdateInput()
        {
            // Moving: 
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            var cameraRotation = cam.transform.rotation.eulerAngles;

            movingBack = vertical < 0;
            moveDirection = new Vector3(horizontal, 0, vertical);
            moveDirection =  Quaternion.Euler(0, cameraRotation.y, 0) * moveDirection;
            moving = moveDirection.magnitude > 0.2f;

            // Facing: 
            if (moving)
            {
                faceDirection = moveDirection;
            }

            faceAngle = 180.0f - Vector3.Angle(cam.transform.forward, Vector3.up);
            faceAngle *= Mathf.Deg2Rad;
            
            accelerating = Input.GetKey(KeyCode.LeftShift);

            // Jump:
            if (!jumping && Input.GetKeyDown(KeyCode.Space))
            {
                jumping = true;
            }
            else
            {
                jumping = false;
            }

            if (Input.GetMouseButton(0))
            {
                if (_leftMouseClickTime < 0.01f)
                {
                    _leftMouseClickTime = Time.time;
                }
                if (Time.time - _leftMouseClickTime > longPressTime)
                {
                    // mouse hold
                    EnableGrabLeft();
                }
            }
            
            punchTimer += Time.deltaTime;
            // punching Left:
            if (Input.GetMouseButtonUp(0))
            {
                if (Time.time - _leftMouseClickTime < longPressTime)
                {                 
                    // mouse Click
                    if (punchTimer > punchHoldTime)
                    {
                        Attack();
                        punchTimer = 0;
                    }
                }
                _leftMouseClickTime = 0.0f;
                ReleaseGrabLeft();
            }
            
            
            if (Input.GetMouseButton(1))
            {
                if (_rightMouseClickTime < 0.01f)
                {
                    _rightMouseClickTime = Time.time;
                }
                if (Time.time - _rightMouseClickTime > longPressTime)
                {
                    // mouse hold
                    EnableGrabRight();
                }
            }
            
            if (Input.GetMouseButtonUp(1))
            {
                if (Time.time - _rightMouseClickTime < longPressTime)
                {
                    // mouse Click
                }

                _rightMouseClickTime = 0.0f;
                ReleaseGrabRight();
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                dropEquipment = true;
            }
        }
        void ReleaseGrabRight()
        {
            grabRight = false;
        }
        void EnableGrabRight()
        {
            grabRight = true;
        }
        void ReleaseGrabLeft()
        {
            grabLeft = false;
        }
        void EnableGrabLeft()
        {
            grabLeft = true;
        }
        void Attack()
        {
            attack = true;
        }
        
        /*
        void Punch()
        {
            if (_lastPunchLeft)
            {
                punchingLeft = true;
            }
            else
            {
                punchingRight = true;
            }
            _lastPunchLeft = !_lastPunchLeft;
        }
        */
    }
}