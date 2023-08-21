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
                case 6:
                {
                    if (bodyController.accelerating)
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
                    // hintText = "Welcome! Press W/A/S/D to move";
                    hintText = "欢迎！请使用W/A/S/D键进行人物移动";
                    break;
                }
                case 1:
                {
                    hintText = "单击鼠标左键来进行攻击";
                    break;
                }
                case 2:
                {
                    hintText = "长按鼠标左键或右键，来抓住物体";
                    break;
                }
                case 3:
                {
                    hintText = "当您的手靠近道具时，可以按住鼠标左/右键将其拾起";
                    break;
                }
                case 4:
                {
                    hintText =  "按F键可以丢弃手中的道具";
                    break;
                }
                case 5:
                {
                    hintText = "按空格键起跳";
                    break;
                }
                case 6:
                {
                    hintText = "按左Shift键可以加速跑";
                    break;
                }
                case 7:
                {
                    hintText = "干的不错！现在请开始你的冒险吧！";
                    break;
                }
            }
            hint.SetText(hintText);
        }
    }
}