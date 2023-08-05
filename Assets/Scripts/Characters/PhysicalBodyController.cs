using System;
using System.Collections.Generic;
using RootMotion.FinalIK;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace ActiveRagdoll
{
    internal struct SkeletonStruct
    {
        public ConfigurableJoint Joint;
        public Transform TargetJoint;
        public Rigidbody RigBody;
        public float InitialMass;
        public Quaternion WorldToJointSpace;
        public Quaternion TargetInitialRotation;
    }
    public class PhysicalBodyController : MonoBehaviour
    {
        internal static readonly string[] Arms;
        internal static readonly string[] Trunks;
        internal static readonly string[] Legs;
        
        public bool enableAnimControlMovement;
        [SerializeField] private Transform _punchTarget;

        public Animator anim;
        public Vector2 basicDrive;
        public Vector2 hipDrive;
        public Vector2 armDrive;
        public Vector2 legDrive;
        public Vector2 trunkDrive;
        public Vector2 grabDrive;
        public float climbPushForce;
        public float punchForce;
        public float jumpForce;
        public float acceleratePushForce;
        
        public float minTargetDirAngle = - 30, maxTargetDirAngle = 60;

        private Dictionary<string, SkeletonStruct> _skeleton;
        private SkeletonStruct _rootJoint;
        private CharacterController characterController;
        
        [Header("States")]
        [SerializeField]
        private bool _knockout;
        [SerializeField]
        private bool _moving;
        [SerializeField]
        private bool _grounded;
        [SerializeField]
        private bool _jumping;
        [SerializeField]
        private bool _accelerating;
        [SerializeField] private bool _punchingLeft, _punchRight,_grabbingLeft,_grabbingRight;
        [SerializeField]
        private bool _walkBack, _walkForward, _stepRight,_stepLeft, _fall;
        [SerializeField]
        float Step_R_Time, Step_L_Time;

        public float timeStep;

        [Header("Moving Parameter")] 
        
        public float walkForwardUpLegSpeed;
        public float walkForwardLowLegWalkSpeed;
        public float walkForwardOtherLegSpeed;
        
        public float walkBackUpLegSpeed;
        public float walkBackLowLegWalkSpeed;
        public float walkBackOtherLegSpeed;
        
        public float upLegRecoverFactor, lowLegRecoverFactor;
        
        [Header("Balancing")]
        public float fallFactor;
        public float forwardUpLegSpeed, forwardLowLegSpeed, forwardOtherLegSpeed;
        public float backwardUpLegSpeed,backwardLowLegSpeed, backwardOtherLegSpeed;
        public float balanceUpLegRecoverFactor, balanceLowLegRecoverFactor;
        
        
        private float _totalMass;
        private float _speed;
        private float _knockoutTime;

        private Vector2 _targetHipDrive;
        private Vector2 _targetLegDrive;
        private Vector2 _targetTrunkDrive;
        private Vector2 _targetArmDrive;
        
        private Vector3 _centerOfMass;

        private Vector3 _bodyUp; 
        private Vector3 _bodyDirection;
        private Vector3 _faceDirection;
        private Quaternion _hipInitialRotation;
        
        // punching  & grab parameter
        private bool _leftPunchForce,_rightPushForce;

        private Transform aimAnimTarget;
        private Transform leftAnimHandTarget;
        private Transform rightAnimHandTarget;
        private Vector3 leftAnimHandTargetInitialPosition;
        private Vector3 rightAnimHandTargetInitialPosition;
        private Vector3 aimAnimTargetInitialPosition;
        
        private GrabController leftHandGrabController;
        private GrabController rightHandGrabController;
        
        private static readonly int VelocityId = Animator.StringToHash("velocity");
        static PhysicalBodyController()
        {
            Arms = new[] {
                "LeftShoulder", "RightShoulder",
                "LeftArm", "RightArm",
                "LeftForeArm","RightForeArm",
                "LeftHand", "RightHand"
            };
            Trunks = new[] {"Spine", "Chest", "UpperChest",  "Neck", "Head" };

            Legs = new[]
            {
                "LeftUpLeg", "RightUpLeg",
                "LeftLeg", "RightLeg",
                "LeftFoot", "RightFoot",
            };
        }
        void OnEnable()
        {
            Application.targetFrameRate = 60;
            characterController = GetComponent<CharacterController>();
            _speed = 0;
            
            InitializeSkeleton();
            IgnoreCollision();
            InitializeAnimation();
            
            var thisTransform = transform;
            _bodyDirection = thisTransform.forward;
            leftHandGrabController.activeRagDoll = thisTransform;
            rightHandGrabController.activeRagDoll = thisTransform;
            leftHandGrabController.enabled = false;
            rightHandGrabController.enabled = false;
        }
        void FixedUpdate()
        {
            // Arms and Trunks are always controlled by animation & IK;
            foreach (var joint in Trunks)
            {
                Utils.SetTargetRotationInternal(_skeleton[joint], _skeleton[joint].TargetJoint.localRotation, Space.Self);
            }
            foreach (var joint in Arms)
            {
                Utils.SetTargetRotationInternal(_skeleton[joint], _skeleton[joint].TargetJoint.localRotation, Space.Self);
            }
            
            if (enableAnimControlMovement)
            {            
                anim.SetBool("fall", _fall);
                anim.SetFloat(VelocityId, _speed);

                Utils.SetTargetRotationInternal(_skeleton["LeftUpLeg"], _skeleton["LeftUpLeg"].TargetJoint.localRotation, Space.Self);
                Utils.SetTargetRotationInternal(_skeleton["RightUpLeg"], _skeleton["RightUpLeg"].TargetJoint.localRotation, Space.Self);
                Utils.SetTargetRotationInternal(_skeleton["LeftLeg"], _skeleton["LeftLeg"].TargetJoint.localRotation, Space.Self);
                Utils.SetTargetRotationInternal(_skeleton["RightLeg"], _skeleton["RightLeg"].TargetJoint.localRotation, Space.Self);
            }
            else
            {            
                if (_moving)
                {
                    _fall = false;
                    _walkForward = !characterController.movingBack;
                    _walkBack = characterController.movingBack;
                }
                else
                {
                    _walkForward = false;
                    _walkBack = false;
                    if (!_grabbingLeft && !_grabbingRight)
                    {
                        Balance();
                    }
                } 
                
                _targetLegDrive = (_moving) ? legDrive : basicDrive;
                _targetTrunkDrive = (_moving || _punchRight || _punchingLeft) ? trunkDrive : basicDrive;
                _targetArmDrive = (_fall || _punchingLeft || _punchRight) ?  armDrive : basicDrive;
                
                _targetHipDrive = (_grabbingLeft || _grabbingRight) ? grabDrive : _targetHipDrive;
                _targetTrunkDrive = (_grabbingLeft || _grabbingRight) ? grabDrive : _targetTrunkDrive;
                _targetArmDrive = (_grabbingLeft || _grabbingRight) ? grabDrive : _targetArmDrive;

                _targetHipDrive = (_fall) ? new Vector2(0,0) : hipDrive;
                // MoveArms();
                DetermineStepFoot();
                MoveLegs();
            }

            if (!_knockout)
            {
                SetLimbsStrength();
            }
            GetSteppingSurface(_skeleton["LeftFoot"], out var leftHitInfo);
            GetSteppingSurface(_skeleton["RightFoot"], out var rightHitInfo);
            _bodyUp = Vector3.Lerp(leftHitInfo.normal, rightHitInfo.normal,0.5f);
        }
        void Update()
        {
            ComputeCenterOfMass();
            Debug.DrawLine(_centerOfMass,_centerOfMass + 3*_bodyUp);
            Debug.DrawLine(_centerOfMass, _centerOfMass + 3*_bodyDirection, Color.green);
            
            if (_knockout)
            {
                return;
            }
            // ground logic:
            if (_bodyUp == Vector3.zero || _jumping)
            {
                _bodyUp = Vector3.up;
                _grounded = false;
            }
            else
            {
                _grounded = true;
            }
            
            // jump logic:
            if (_grounded && characterController.jumping)
            {
                _rootJoint.RigBody.velocity += new Vector3(0, jumpForce, 0);
                _jumping = true;
                _grounded = false;
                // _rootJoint.RigBody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
            else
            {
                _jumping = false;
            }
            
            _moving = characterController.moving;
            _accelerating = _moving && characterController.accelerating;
            
            var moveDirection = characterController.moveDirection;
            _speed = Mathf.Lerp(_speed, moveDirection.magnitude, 0.05f);
            if (_moving)
            {
                _bodyDirection = moveDirection.normalized;
                var backwardFactor = characterController.movingBack ? -1 : 1;
                var crossAxis = Vector3.Cross(backwardFactor * _bodyDirection, _bodyUp);
                var forceDirection = Vector3.Cross(_bodyUp, crossAxis).normalized;
            
                var angle = Mathf.Clamp(forceDirection.y , 0, 1);
                var forceScale = angle * climbPushForce;
                if (_accelerating)
                {
                    forceScale += acceleratePushForce;
                }
                _rootJoint.RigBody.AddForce(forceScale * forceDirection);
            }
            
            var faceDirection = _bodyDirection;
            var rot = Quaternion.LookRotation(faceDirection) * _rootJoint.TargetInitialRotation;
            // rot = Quaternion.Slerp(_rootJoint.Joint.targetRotation, rot, 0.1f);
            Utils.SetTargetRotationInternal(_rootJoint, rot, Space.World);
            
            // Update Animation:
            UpdateAim();
            UpdateGrab();
            UpdatePunch();
        }
        void InitializeSkeleton()
        {            
            _skeleton = new Dictionary<string, SkeletonStruct>();
            _totalMass = 0;
            SkeletonConfig skeletonConfig = GetComponent<SkeletonConfig>();
            if (skeletonConfig == null)
            {
                return;
            }
            Type type = skeletonConfig.GetType();
            foreach (var joint in GetComponentsInChildren<ConfigurableJoint>())
            {
                var jointName = joint.gameObject.name;
                var skeletonStruct = new SkeletonStruct();
                skeletonStruct.Joint = joint;
                skeletonStruct.RigBody = joint.GetComponent<Rigidbody>();
                skeletonStruct.InitialMass = skeletonStruct.RigBody.mass;
                _totalMass += skeletonStruct.InitialMass;
                var value = type.GetField(jointName)?.GetValue(skeletonConfig);
                Transform targetJoint = value.ConvertTo<Transform>();
                if (targetJoint == null)
                {
                    Debug.Log(jointName);
                    break;
                }
                skeletonStruct.TargetInitialRotation = targetJoint.localRotation; 
                var right = joint.axis;
                var forward = Vector3.Cross (right, joint.secondaryAxis).normalized;
                var up = Vector3.Cross (forward, right).normalized;
                skeletonStruct.TargetJoint = targetJoint;
                skeletonStruct.WorldToJointSpace = Quaternion.LookRotation(forward, up);
                if (jointName == "Hips")
                {
                    skeletonStruct.TargetInitialRotation = joint.transform.rotation;
                    _rootJoint = skeletonStruct;
                    Utils.SetTargetRotationInternal(skeletonStruct, skeletonStruct.TargetInitialRotation, Space.World);
                    continue;
                }
                _skeleton[jointName] = skeletonStruct;
            }
        }
        void InitializeAnimation()
        {
            var armIKs = anim.GetComponents<ArmIK>();
            leftAnimHandTarget = armIKs[0].solver.arm.target;
            rightAnimHandTarget = armIKs[1].solver.arm.target;

            var aimIK = anim.GetComponent<AimIK>();
            aimAnimTarget = aimIK.solver.target;
            aimAnimTargetInitialPosition = aimAnimTarget.localPosition;
            
            leftHandGrabController = _skeleton["LeftHand"].Joint.GetComponent<GrabController>();
            rightHandGrabController = _skeleton["RightHand"].Joint.GetComponent<GrabController>();
            
            leftAnimHandTargetInitialPosition = leftAnimHandTarget.localPosition;
            rightAnimHandTargetInitialPosition = rightAnimHandTarget.localPosition;
        }
        // Arm Movement:
        void UpdateAim()
        {
            var bendScale = 2f;
            var faceOffset = Math.Clamp(-bendScale * Mathf.Cos(characterController.faceAngle), -2f, 3f) ;
            if (_grabbingLeft || _grabbingRight || _punchingLeft || _punchRight)
            {
                aimAnimTarget.localPosition = Vector3.Lerp(aimAnimTarget.localPosition, new Vector3(0, 1 +faceOffset, 0.3f), 0.2f);
            }
            else
            {
                aimAnimTarget.localPosition =Vector3.Lerp(aimAnimTarget.localPosition,aimAnimTargetInitialPosition,0.2f);
            }
        }
        void UpdateGrab()
        {

            var grabScale = 5.0f;
            var faceOffset =  Math.Clamp(-grabScale * Mathf.Cos(characterController.faceAngle), -0.8f, 4f) ;
            if (characterController.grabLeft)
            {
                leftAnimHandTarget.localPosition = Vector3.Lerp(leftAnimHandTarget.localPosition, new Vector3(-0.1f, 1 + faceOffset, 0.5f), 0.2f);
                // leftAnimHandTarget.localPosition = Vector3.Lerp(leftAnimHandTarget.localPosition, new Vector3(0, 1 - Mathf.Cos(faceAngle),1), 0.8f);
                _grabbingLeft = true;
                leftHandGrabController.enabled = true;
            }
            else
            {
                leftAnimHandTarget.localPosition =Vector3.Lerp(leftAnimHandTarget.localPosition,leftAnimHandTargetInitialPosition,0.2f);
                _grabbingLeft = false;
                leftHandGrabController.enabled = false;
            } 
            if (characterController.grabRight)
            {
                //rightAnimHandTarget.localPosition = Vector3.Lerp(rightAnimHandTarget.localPosition, new Vector3(0, 1 - Mathf.Cos(faceAngle),1), 0.8f);
                rightAnimHandTarget.localPosition = Vector3.Lerp(rightAnimHandTarget.localPosition, new Vector3(0.1f, 1 + faceOffset, 0.5f), 0.2f);
                _grabbingRight = true;
                rightHandGrabController.enabled = true;
            }
            else
            {
                rightAnimHandTarget.localPosition =Vector3.Lerp(rightAnimHandTarget.localPosition,rightAnimHandTargetInitialPosition,0.2f);
                _grabbingRight = false;
                rightHandGrabController.enabled = false;
            }
        }
        void UpdatePunch()
        {
            //Debug.DrawLine(_skeleton["UpperChest"].Joint.transform.position,_punchTarget.position, Color.magenta);
            // Punch Left
            if (!_grabbingLeft)
            {
                if (characterController.readyPunchingLeft)
                {
                    _punchingLeft = true;
                    leftAnimHandTarget.localPosition = Vector3.Lerp(leftAnimHandTarget.localPosition,new Vector3(-0.2f, 1.1f,0.05f), 0.3f);
                    _leftPunchForce = false;
                
                } else if (characterController.punchingLeft)
                {
                    var leftHandPosition = _skeleton["LeftHand"].Joint.transform.position;
                    //new Vector3(0, 1.5f,1)
                    leftAnimHandTarget.localPosition = Vector3.Lerp(leftAnimHandTarget.localPosition, new Vector3(0, 1.5f,1), 0.7f);
                    if (!_leftPunchForce)
                    {
                        var puchForceDir = (_punchTarget.position - leftHandPosition).normalized;
                        _skeleton["LeftHand"].RigBody.AddForce(punchForce * puchForceDir, ForceMode.Impulse);
                        _rootJoint.RigBody.AddForce(punchForce * -puchForceDir, ForceMode.Impulse);
                        _leftPunchForce = true;
                    }
                }
                else
                {
                    leftAnimHandTarget.localPosition =Vector3.Lerp(leftAnimHandTarget.localPosition,leftAnimHandTargetInitialPosition,0.1f);  
                    _punchingLeft = false;
                }
            }

            if (!_grabbingRight)
            {
                // Punch Right
                if (characterController.readyPunchingRight)
                {
                    _punchRight = true;
                    rightAnimHandTarget.localPosition = Vector3.Lerp(rightAnimHandTarget.localPosition,new Vector3(0.2f, 1.1f,0.05f), 0.3f);
                    _rightPushForce = false;
                
                } else if (characterController.punchingRight)
                {
                    var rightHandPosition = _skeleton["RightHand"].Joint.transform.position;
                    rightAnimHandTarget.localPosition = Vector3.Lerp(rightAnimHandTarget.localPosition,new Vector3(0, 1.5f,1), 0.7f);
                    if (!_rightPushForce)
                    {
                        var puchForceDir = (_punchTarget.position - rightHandPosition).normalized;
                        _skeleton["RightHand"].RigBody.AddForce(punchForce * puchForceDir, ForceMode.Impulse);
                        _rootJoint.RigBody.AddForce(punchForce * -puchForceDir, ForceMode.Impulse);
                        _rightPushForce = true;
                    }
                }
                else
                {
                    rightAnimHandTarget.localPosition =Vector3.Lerp(rightAnimHandTarget.localPosition,rightAnimHandTargetInitialPosition,0.1f);  
                    _punchRight = false;
                }
            }
        }
        
        // Leg Movement:
        void Balance()
        {
            var leftFootPosition = _skeleton["LeftFoot"].Joint.transform.position;
            var rightFootPosition = _skeleton["RightFoot"].Joint.transform.position;
            Debug.DrawLine(_centerOfMass, leftFootPosition);
            Debug.DrawLine(_centerOfMass, rightFootPosition);
            
            var cross = Vector3.Cross(_bodyDirection, _bodyUp);
            
            var leftFootOffsetX = Vector3.Dot(_bodyDirection, leftFootPosition - _centerOfMass);
            var rightFootOffsetX = Vector3.Dot(_bodyDirection, rightFootPosition - _centerOfMass);
            var leftFootOffsetY = Vector3.Dot(cross, leftFootPosition - _centerOfMass);
            var rightFootOffsetY = Vector3.Dot(cross, rightFootPosition - _centerOfMass);
            _fall = Mathf.Abs(leftFootOffsetX) > fallFactor ||
                    Mathf.Abs(rightFootOffsetX) > fallFactor ||
                    Mathf.Abs(leftFootOffsetY) > fallFactor ||
                    Mathf.Abs(rightFootOffsetY) > fallFactor;
            // balance Vertical
            if (!_moving)
            {
                _walkBack = leftFootOffsetX > 0 && rightFootOffsetX > 0;
                _walkForward = leftFootOffsetX < 0 && rightFootOffsetX < 0;
            }
        }
        void DetermineStepFoot()
        {
            var leftFootPosition = _skeleton["LeftFoot"].Joint.transform.position;
            var rightFootPosition = _skeleton["RightFoot"].Joint.transform.position;
            // var dir = Quaternion.Inverse(_rootJoint.TargetInitialRotation) * _rootJoint.Joint.transform.forward;
            var dir = _skeleton["Spine"].Joint.transform.forward;
            if (_fall && !_walkForward && !_walkBack)
            {
                if (dir.y > 0)
                {
                    _walkBack = true;
                }
                else
                {
                    _walkForward = true;
                }
            }
            var isRightFootFront = Vector3.Dot(dir, rightFootPosition - leftFootPosition) > 0; 
            if (_walkForward)
            {
                if (!isRightFootFront && !_stepLeft)
                {
                    _stepRight = true;
                }
                if (isRightFootFront && !_stepRight)
                {
                    _stepLeft = true;
                }
            }else if (_walkBack)
            {
                if (isRightFootFront && !_stepLeft)
                {
                    _stepRight = true;
                }
                if (!isRightFootFront && !_stepRight)
                {
                    _stepLeft = true;
                }
            } else
            {
                _stepRight = false;
                _stepLeft = false;
                Step_R_Time = 0;
                Step_L_Time = 0;
                // JointParts[0].targetRotation = Quaternion.Lerp(JointParts[0].targetRotation, new Quaternion(-0.1f, JointParts[0].targetRotation.y, JointParts[0].targetRotation.z, JointParts[0].targetRotation.w), 6 * Time.fixedDeltaTime);
            }
        }
        void MoveLegs()
        {
            float time = timeStep;
            float factor1 = 0, factor2= 0, factor3= 0;
            float upRecover = _moving ? upLegRecoverFactor : balanceUpLegRecoverFactor;
            float lowRecover = _moving ? lowLegRecoverFactor : balanceLowLegRecoverFactor;
            if (_walkForward)
            {
                factor1 = _moving ? walkForwardUpLegSpeed :  forwardUpLegSpeed;
                factor2 = _moving ? walkForwardLowLegWalkSpeed : forwardLowLegSpeed;
                factor3 = _moving ? walkForwardOtherLegSpeed : forwardOtherLegSpeed;
            }
            if (_walkBack)
            {
                factor1 = (_moving ? walkBackUpLegSpeed :  backwardUpLegSpeed);
                factor2 = _moving ? walkBackLowLegWalkSpeed : backwardLowLegSpeed;
                factor3 = - (_moving ? walkBackOtherLegSpeed : backwardOtherLegSpeed);
            }
            if (_fall || _accelerating)
            {
                time = 0.2f;
                factor1 *= 2;
                factor2 *= 2;
                factor3 *= 2;
            }

            if (_stepRight)
            {
                var leftUpLegJoint = _skeleton["LeftUpLeg"];
                var leftUpLegTargetRotation = leftUpLegJoint.Joint.targetRotation;
                var rightUpLegJoint = _skeleton["RightUpLeg"];
                var rightUpLegTargetRotation = rightUpLegJoint.Joint.targetRotation;
                var rightLegJoint = _skeleton["RightLeg"];
                var rightLegTargetRotation = rightLegJoint.Joint.targetRotation;
                Step_R_Time += Time.fixedDeltaTime;
                rightUpLegJoint.Joint.targetRotation = new Quaternion(rightUpLegTargetRotation.x + factor1, rightUpLegTargetRotation.y,rightUpLegTargetRotation.z, rightUpLegTargetRotation.w);
                rightLegJoint.Joint.targetRotation = new Quaternion(rightLegTargetRotation.x -factor2, rightLegTargetRotation.y,rightLegTargetRotation.z, rightLegTargetRotation.w);
                leftUpLegJoint.Joint.targetRotation = new Quaternion(leftUpLegTargetRotation.x - factor3, leftUpLegTargetRotation.y,leftUpLegTargetRotation.z, leftUpLegTargetRotation.w);
                if (Step_R_Time > time)
                {
                    Step_R_Time = 0;
                    _stepRight = false;
                    if (_walkBack || _walkForward)
                    {
                        _stepLeft = true;
                    }
                }
            }
            else
            {
                var rightUpLegJoint = _skeleton["RightUpLeg"];
                var rightUpLegTargetRotation = rightUpLegJoint.Joint.targetRotation;
                var rightLegJoint = _skeleton["RightLeg"];
                var rightLegTargetRotation = rightLegJoint.Joint.targetRotation;
                rightUpLegJoint.Joint.targetRotation = Quaternion.Lerp( rightUpLegTargetRotation,
                    Utils.GetTargetRotationInternal(rightUpLegJoint, rightUpLegJoint.TargetInitialRotation, Space.Self),
                    upRecover* Time.fixedDeltaTime);
                rightLegJoint.Joint.targetRotation = Quaternion.Lerp(rightLegTargetRotation, 
                    Utils.GetTargetRotationInternal(rightLegJoint, rightLegJoint.TargetInitialRotation, Space.Self),
                    lowRecover * Time.fixedDeltaTime);
            }
            if (_stepLeft)
            {
                var leftUpLegJoint = _skeleton["LeftUpLeg"];
                var leftUpLegTargetRotation = leftUpLegJoint.Joint.targetRotation;
                var rightUpLegJoint = _skeleton["RightUpLeg"];
                var rightUpLegTargetRotation = rightUpLegJoint.Joint.targetRotation;
                var leftLegJoint = _skeleton["LeftLeg"];
                var leftLegTargetRotation = leftLegJoint.Joint.targetRotation;
                Step_L_Time += Time.fixedDeltaTime;
                leftUpLegJoint.Joint.targetRotation = new Quaternion(leftUpLegTargetRotation.x + factor1, leftUpLegTargetRotation.y, leftUpLegTargetRotation.z, leftUpLegTargetRotation.w);
                leftLegJoint.Joint.targetRotation = new Quaternion(leftLegTargetRotation.x - factor2, leftLegTargetRotation.y, leftLegTargetRotation.z, leftLegTargetRotation.w);
                rightUpLegJoint.Joint.targetRotation = new Quaternion(rightUpLegTargetRotation.x - factor3, rightUpLegTargetRotation.y,rightUpLegTargetRotation.z, rightUpLegTargetRotation.w);
                if (Step_L_Time > time)
                {
                    Step_L_Time = 0;
                    _stepLeft = false;
                    if (_walkBack || _walkForward)
                    {
                        _stepRight = true;
                    }
                }
            }
            else
            {
                var leftUpLegJoint = _skeleton["LeftUpLeg"].Joint;
                var leftUpLegTargetRotation = leftUpLegJoint.targetRotation;
                var leftLegJoint = _skeleton["LeftLeg"].Joint;
                var leftLegTargetRotation = leftLegJoint.targetRotation;
                leftUpLegJoint.targetRotation = Quaternion.Lerp(leftUpLegTargetRotation, 
                    Utils.GetTargetRotationInternal(_skeleton["LeftUpLeg"], _skeleton["LeftUpLeg"].TargetInitialRotation, Space.Self), 
                    upRecover * Time.fixedDeltaTime);
                leftLegJoint.targetRotation = Quaternion.Lerp( leftLegTargetRotation, 
                    Utils.GetTargetRotationInternal(_skeleton["LeftLeg"], _skeleton["LeftLeg"].TargetInitialRotation, Space.Self), 
                    lowRecover * Time.fixedDeltaTime);
            }
        }
        
        // Useful function:
        void GetSteppingSurface(SkeletonStruct joint, out RaycastHit info)
        {
            Ray ray = new Ray(joint.Joint.transform.position, Vector3.down);
            Physics.Raycast(ray, out info, 0.5f);
        }
        void IgnoreCollision()
        {
            foreach (var joint in _skeleton.Values)
            {
                var connectedBody = joint.Joint.connectedBody;
                if (connectedBody != null)
                {
                    Physics.IgnoreCollision(joint.Joint.GetComponent<Collider>(), connectedBody.GetComponent<Collider>());
                }
            }
            Physics.IgnoreCollision(_skeleton["LeftArm"].Joint.GetComponent<Collider>(),_skeleton["UpperChest"].Joint.GetComponent<Collider>());
            Physics.IgnoreCollision(_skeleton["RightArm"].Joint.GetComponent<Collider>(),_skeleton["UpperChest"].Joint.GetComponent<Collider>());
        }
        void SetLimbsStrength()
        {
            var hipDriveSpring = _rootJoint.Joint.angularXDrive.positionSpring;
            hipDriveSpring = Mathf.Lerp(hipDriveSpring, _targetHipDrive.x, 0.1f);
            Utils.SetJointStrength(_rootJoint.Joint, hipDriveSpring, hipDriveSpring, _targetHipDrive.y);
            foreach (var joint in Arms)
            {
                Utils.SetJointStrength(_skeleton[joint].Joint, _targetArmDrive.x, _targetArmDrive.x, _targetArmDrive.y);
            }
            foreach (var joint in Trunks)
            {
                Utils.SetJointStrength(_skeleton[joint].Joint, _targetTrunkDrive.x,_targetTrunkDrive.x, _targetTrunkDrive.y);
            }

            var currentLegStrength = _targetLegDrive.x;
            var currentLegDamper = _targetLegDrive.y;
            
            foreach (var joint in Legs)
            {
                Utils.SetJointStrength(_skeleton[joint].Joint, currentLegStrength,currentLegStrength, currentLegDamper);
            }
        }
        public void Knockout()
        {
            _knockout = true;
            _rootJoint.RigBody.mass = 1.0f;
            Utils.SetJointStrength(_rootJoint.Joint, 0,0,0.5f);
            foreach (var joint in Arms)
            {
                _skeleton[joint].RigBody.mass = 0.05f;
                Utils.SetJointStrength(_skeleton[joint].Joint, 0,0,0.2f);
            }
            foreach (var joint in Trunks)
            {
                _skeleton[joint].RigBody.mass = 0.3f;
                Utils.SetJointStrength(_skeleton[joint].Joint, 0,0,0.3f);
            }
            foreach (var joint in Legs)
            {
                _skeleton[joint].RigBody.mass = 0.15f;
                Utils.SetJointStrength(_skeleton[joint].Joint, 0,0, 0.2f);
            }
        }

        public void Recover(){
            _knockout = false;
            _rootJoint.RigBody.mass = _rootJoint.InitialMass;
            foreach (var joint in _skeleton.Values)
            {
                joint.RigBody.mass = joint.InitialMass;
            }
        }

        void ComputeCenterOfMass()
        {
            Vector3 centerPos = _rootJoint.RigBody.position * _rootJoint.InitialMass;
            foreach (var joint in _skeleton.Values)
            {
                centerPos += joint.InitialMass * joint.RigBody.position;
            }

            centerPos /= _totalMass;
            _centerOfMass =  centerPos;
        }
    }
}