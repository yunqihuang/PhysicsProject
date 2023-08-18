using System;
using ActiveRagdoll;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay
{
    public class Tutorial : MonoBehaviour
    {
        public PhysicalBodyController bodyController;
        public TMP_Text hint;

        private int _stage;
        
        private float _timer;
        void OnEnable()
        {
            _stage = 0;
            hint.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            hint.gameObject.SetActive(false);
        }

        void UpdateStage()
        {
            switch (_stage)
            {
                case 0:
                {
                    if (bodyController.moving)
                        _stage++;
                    break;
                }
                case 1:
                {
                    if (bodyController.punching)
                        _stage++;
                    break;
                }
                case 2:
                {
                    if (bodyController.IsGrabbing())
                        _stage++;
                    break;
                }
                case 3:
                {
                    if (bodyController.IsGrabbingProps())
                        _stage++;
                    break;
                }
                case 4:
                {      
                    if (!bodyController.IsGrabbingProps())
                        _stage++;
                    break;
                }
                case 5:
                {
                    if (bodyController.jumping)
                        _stage++;
                    break;
                }

            }
        }
        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer > 1.0f)
            {
                UpdateStage();
                _timer = 0;
            }

            
            
            string hintText = "";
            switch (_stage)
            {
                case 0:
                {
                    hintText = "Welcome! Press W/A/S/D to move";
                    break;
                }
                case 1:
                {
                    hintText = "Click left mouse button to Attack";
                    break;
                }
                case 2:
                {
                    hintText = "Hold left/right mouse button to grab something";
                    break;
                }
                case 3:
                {
                    hintText = "When your hands are close to a prop, you can hold left/right mouse button to pick it up";
                    break;
                }
                case 4:
                {
                    hintText =  "Press F to drop the props";
                    break;
                }
                case 5:
                {
                    hintText = "Press space to jump";
                    break;
                }
                case 6:
                {
                    hintText = "Good job! Now you are ready to start your adventure!";
                    break;
                }
            }
            hint.SetText(hintText);
        }
    }
}