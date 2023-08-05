using UnityEngine;

namespace ActiveRagdoll
{
    public class PlayerController : CharacterController
    {
        public Camera cam;
        protected override void UpdateInput()
        {
            // Moving: 
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            var cameraRotation = cam.transform.rotation.eulerAngles;

            movingBack = vertical < 0;
            moveDirection = new Vector3(horizontal, 0, vertical) * (movingBack ? -1 : 1);
            moveDirection =  Quaternion.Euler(0, cameraRotation.y, 0) * moveDirection;
            moving = moveDirection.magnitude > 0.1f;

            // Facing:     
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
            
            //Grab:
            grabLeft = Input.GetMouseButton(0);
            grabRight = Input.GetMouseButton(1);
            
            // punching Left:
            if (!readyPunchingLeft && !punchingLeft &&  Input.GetKeyDown(KeyCode.Q))
            {
                readyPunchingLeft = true;
            }
            if (readyPunchingLeft && Input.GetKeyUp(KeyCode.Q))
            {
                readyPunchingLeft = false;
                punchingLeft = true;
                punchingLeftTimer = 0;
            }
            if (punchingLeft)
            {
                punchingLeftTimer += Time.deltaTime;
                if (punchingLeftTimer > punchHoldTime)
                {
                    punchingLeft = false;
                }
            }
            
            // punching Right:
            if (!readyPunchingRight && !punchingRight  && Input.GetKeyDown(KeyCode.E))
            {
                readyPunchingRight = true;
            }

            if (readyPunchingRight && Input.GetKeyUp(KeyCode.E))
            {
                readyPunchingRight = false;
                punchingRight = true;
                punchingRightTimer = 0;
            }
            if (punchingRight)
            {
                punchingRightTimer += Time.deltaTime;
                if (punchingRightTimer > punchHoldTime)
                {
                    punchingRight = false;
                }
            }
        }
    }
}