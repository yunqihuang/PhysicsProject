using System;
using System.Collections.Generic;
using RootMotion.FinalIK;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace ActiveRagdoll
{
    public struct SkeletonStruct
    {
        public ConfigurableJoint Joint;
        public Transform TargetJoint;
        public Rigidbody RigBody;
        public float InitialMass;
        public Vector3 TargetInitialPosition;
        public Quaternion TargetInitialRotation;
        public Quaternion WorldToJointSpace;
    }
    public class PhysicalBodyController : MonoBehaviour
    {
        internal static readonly string[] Arms;
        internal static readonly string[] Trunks;
        internal static readonly string[] Legs;

        public bool knockedOut => _knockout;

        public bool enableAnimControlMovement;
        [SerializeField] private Transform _punchTarget;
        [SerializeField] private Transform _swingTarget,_swingUpTarget;
        public LayerMask groundLayer;
        
        public Animator anim;
        public Vector2 basicDrive;
        public Vector2 hipDrive;
        public Vector2 armDrive;
        public Vector2 legDrive;
        public Vector2 trunkDrive;
        public float rotationTorque;
        public float punchForce;
        public float swingForce;
        public float jumpForce;
        public float balanceForce;
        
        public float footHeight;

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

        [SerializeField] 
        private bool _punching;
        
        [SerializeField] 
        private bool _swing;
        
        [SerializeField]
        private bool _walkBack, _walkForward, _stepRight,_stepLeft, _fall;
        [SerializeField]
        float Step_R_Time, Step_L_Time;


        [Header("Moving Parameter")]
        private StepData _movementStepData;

        public float rideSpringStrength;
        public float rideSpringDamper;

        [Header("Locomotion")] 
        public float accelerationFactor;
        public float maxSpeed;
        public float acceleration;
        public AnimationCurve accelerationFromDot;
        public float maxAccelerationForce;
        public AnimationCurve maxAccelerationForceFactorFromDot;


        public Vector3 forceScale;

        private Vector3 _goalVelocity;
        private float _speedFactor;
        private float _maxAccelerationForceFactor;
        
        [Header("Balancing")]
        public float timeStep;
        public float balanceFactor;
        public float fallFactor;
        public float forwardUpLegSpeed, forwardLowLegSpeed, forwardOtherLegSpeed;
        public float backwardUpLegSpeed,backwardLowLegSpeed, backwardOtherLegSpeed;
        public float balanceUpLegRecoverFactor, balanceLowLegRecoverFactor;

        private float _totalMass;
        private float _knockoutTime;
        private float _jumpTime;
        
        private Vector2 _targetHipDrive;
        private Vector2 _targetLegDrive;
        private Vector2 _targetTrunkDrive;
        private Vector2 _targetArmDrive;
        
        private Vector3 _centerOfMass;

        public Vector3 bodyUp; 
        public Vector3 bodyDirection;
        public Vector3 faceDirection;

        // punching  & grab parameter
        private bool _punchLeft, _punchRight,
            _grabbingLeft, _grabbingRight;
        private float _punchTimer;
        private bool _lastPunchLeft;

        private bool _swingLeftUp, _swingRightUp, _swingLeft, _swingRight;
        private float _swingTimer;

        private float _fallDistance;
        private Transform aimAnimTarget;
        private Transform leftAnimHandTarget;
        private Transform rightAnimHandTarget;
        private Vector3 aimAnimTargetInitialPosition;
        
        private GrabController leftHandGrabController;
        private GrabController rightHandGrabController;
        

        private IKManager _ikManager;
        static PhysicalBodyController()
        {
            Arms = new[] {
                "LeftShoulder", "RightShoulder",
                "LeftArm", "RightArm",
                "LeftForeArm","RightForeArm",
                "LeftHand", "RightHand"
            };
            Trunks = new[] {"Hips", "Spine", "Chest", "UpperChest",  "Neck", "Head" };

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
            _movementStepData = GetComponent<StepData>();
            characterController = GetComponent<CharacterController>();
        }
        private void Start()
        {
            InitializeSkeleton();            
            InitializeAnimation();
            faceDirection = bodyDirection = transform.forward;
            leftHandGrabController = _skeleton["LeftHand"].Joint.GetComponent<GrabController>();
            rightHandGrabController = _skeleton["RightHand"].Joint.GetComponent<GrabController>();
            leftHandGrabController.enabled = false;
            rightHandGrabController.enabled = false;
            _fallDistance = 5.0f;
        }
        
        void FixedUpdate()
        {
            if (!_knockout)
            {
                RootLocomotion();
                MatchingAnimation();
                UpdateGrab();
                UpdatePunch();
                UpdateSwing();
                _targetLegDrive =  legDrive;
                _targetTrunkDrive = (_punching|| _swing) ? trunkDrive : basicDrive;
                _targetArmDrive =  armDrive;
                _targetHipDrive = (_moving || _punching|| _swing|| _swing) ? hipDrive : basicDrive; 
                _targetHipDrive = (_fall) ? new Vector2(0,0) : hipDrive;
                
                // Additional Balance Force
                if (!_fall)
                {
                    _skeleton["Neck"].RigBody.AddForce(Vector3.up * balanceForce, ForceMode.Force);
                    _skeleton["Hips"].RigBody.AddForce(-Vector3.up * balanceForce, ForceMode.Force);
                }
                SetLimbsStrength();
            }
            _grounded = GetSteppingSurface(_rootJoint, footHeight, out var rayHit);
            bodyUp = rayHit.normal;
        }
        
        void Update()
        {            
            ComputeCenterOfMass();
            if (_knockout)
            {
                leftHandGrabController.DropEquipment();
                rightHandGrabController.DropEquipment();
                return;
            }
            Debug.DrawLine(_centerOfMass,_centerOfMass + 3*bodyUp);
            Debug.DrawLine(_centerOfMass, _centerOfMass + 3*faceDirection, Color.green);
            
            // jump logic:
            if (_grounded && characterController.jumping)
            {            
                bodyUp = Vector3.up;
                characterController.jumping = false;
                _rootJoint.RigBody.velocity += new Vector3(0, jumpForce, 0);
                _jumping = true;
                _grounded = false;
                _fallDistance = 10.0f;
            }
            if (_jumping)
            {
                _jumpTime += Time.deltaTime;
                if (_jumpTime > 2.0f)
                {
                    if (_grounded)
                    {
                        _jumpTime = 0;
                        _jumping = false;
                        _fallDistance = 5.0f;
                    }
                }
                // _rootJoint.RigBody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }

            // attack logic:
            if (characterController.attack)
            {
                characterController.attack = false;
                Attack();
            }

            if (characterController.dropEquipment)
            {
                characterController.dropEquipment = false;
                DropEquipment();
            }
            _moving = characterController.moving;
            _accelerating = _moving && characterController.accelerating;
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
            var joints = GetComponentsInChildren<ConfigurableJoint>();
            foreach (var joint in joints)
            {
                var jointName = joint.gameObject.name;
                var skeletonStruct = new SkeletonStruct();
                skeletonStruct.Joint = joint;
                
                var right = joint.axis;
                var forward = Vector3.Cross (right, joint.secondaryAxis).normalized;
                var up = Vector3.Cross (forward, right).normalized;
                skeletonStruct.WorldToJointSpace = Quaternion.LookRotation(forward, up);
                
                skeletonStruct.RigBody = joint.GetComponent<Rigidbody>();
                skeletonStruct.RigBody.mass /= 2;
                skeletonStruct.InitialMass = skeletonStruct.RigBody.mass;
                _totalMass += skeletonStruct.InitialMass;
                
                var value = type.GetField(jointName)?.GetValue(skeletonConfig);
                if (jointName == "PhysicalRagdoll")
                {
                    skeletonStruct.TargetJoint = null;
                    skeletonStruct.TargetInitialRotation = joint.transform.rotation;
                    skeletonStruct.TargetInitialPosition = joint.transform.position;
                    _rootJoint = skeletonStruct;
                }
                else
                {
                    Transform targetJoint = value.ConvertTo<Transform>();
                    if (targetJoint == null)
                    {
                        break;
                    }
                    skeletonStruct.TargetJoint = targetJoint;
                    skeletonStruct.TargetInitialRotation = targetJoint.localRotation; 
                    skeletonStruct.TargetInitialPosition = targetJoint.position;
                    _skeleton[jointName] = skeletonStruct;
                }
            }
            Utils.SetTargetRotationInternal(_rootJoint, _rootJoint.TargetInitialRotation, Space.World);
            
            IgnoreCollision();
        }
        void InitializeAnimation()
        {
            _ikManager = anim.GetComponent<IKManager>();
            var armIKs = anim.GetComponents<ArmIK>();
            leftAnimHandTarget = armIKs[1].solver.arm.target;
            rightAnimHandTarget = armIKs[0].solver.arm.target;

            var aimIK = anim.GetComponent<AimIK>();
            aimAnimTarget = aimIK.solver.target;
            aimAnimTargetInitialPosition = aimAnimTarget.localPosition;
        }

        #region Movements
        void RootLocomotion()
        {
            // floating Body:
            _fall = !GetSteppingSurface(_rootJoint, _fallDistance, out var rayHit);
            if (!_fall)
            {
                var rigBody = _rootJoint.RigBody;
                var vel = rigBody.velocity;
                // var rayDir = transform.TransformDirection(Vector3.down);
                var rayDir = Vector3.down;
                
                var otherVel = Vector3.zero;
                var hitBody = rayHit.rigidbody;
                if (hitBody != null)
                {
                    otherVel = hitBody.velocity;
                }
                var rayDirVel = Vector3.Dot(rayDir, vel);
                var otherDirVel = Vector3.Dot(rayDir, otherVel);
                float relVel = rayDirVel - otherDirVel;

                // float x =  rayHit.distance - (rideHeight + _skeleton["Hips"].TargetJoint.position.y - _skeleton["Hips"].TargetInitialPosition.y);
                float x = rayHit.distance - (_skeleton["Hips"].TargetJoint.position.y - _skeleton["Hips"].TargetInitialPosition.y);
                float springForce = (x * rideSpringStrength) - (relVel * rideSpringDamper);
                rigBody.AddForce(rayDir * springForce);
                if (hitBody != null)
                {
                    hitBody.AddForceAtPosition(-rayDir * springForce, rayHit.point);
                }
            }
            
            // moving:
            if (_moving || _swing || _punching ||  _goalVelocity.magnitude > 0.1f)
            {
                _speedFactor = _accelerating ? accelerationFactor : 1.0f;
                _maxAccelerationForceFactor = _accelerating ? accelerationFactor : 1.0f;

                if (_fall)
                {
                    _maxAccelerationForceFactor /= 20.0f;
                }
                
                var moveDirection = characterController.moveDirection.normalized;

                float velDot = Vector3.Dot(_goalVelocity, moveDirection);
                var goalVel =  maxSpeed * _speedFactor * moveDirection;
                var accel = acceleration * accelerationFromDot.Evaluate(velDot);
            
                _goalVelocity = Vector3.MoveTowards(_goalVelocity, goalVel, accel * Time.fixedDeltaTime);
                var neededAccel = (_goalVelocity - _rootJoint.RigBody.velocity) / Time.fixedDeltaTime;
                var maxAccel = maxAccelerationForce*_maxAccelerationForceFactor*maxAccelerationForceFactorFromDot.Evaluate(velDot);

                neededAccel = Vector3.ClampMagnitude(neededAccel, maxAccel);
                _rootJoint.RigBody.AddForce(Vector3.Scale(neededAccel * _totalMass, forceScale));
            
                bodyDirection =  Vector3.Slerp(bodyDirection, moveDirection , 0.2f);
                var backwardFactor = Vector3.Dot(bodyDirection, bodyUp) > 0.1f ? -1 : 1;
                var forceDirection = Vector3.ProjectOnPlane(backwardFactor * bodyDirection, bodyUp);
                
                faceDirection = Vector3.Slerp(faceDirection,characterController.faceDirection.normalized,0.8f);

            }
            

            if (_moving)
            {
                var rot = Quaternion.LookRotation(faceDirection) * _rootJoint.TargetInitialRotation;
                Utils.SetTargetRotationInternal(_rootJoint, rot, Space.World);
                
                _rootJoint.RigBody.AddTorque(Vector3.Cross(_rootJoint.Joint.transform.forward, faceDirection)*rotationTorque);
            }
        }
        #endregion
        
        #region Actions
        void Attack()
        {
            anim.SetLayerWeight(anim.GetLayerIndex("UpperBody"), 1.0f);
            
            if (leftHandGrabController.propType == PropType.Equipment || rightHandGrabController.propType == PropType.Equipment)
            {
                anim.SetTrigger(leftHandGrabController.propType == PropType.Equipment ? "leftSwing" : "rightSwing");
                leftHandGrabController.ActivateProps();
                rightHandGrabController.ActivateProps();
                _swing = true;
                
            }else if (leftHandGrabController.propType == PropType.Gun || rightHandGrabController.propType == PropType.Gun)
            {
                anim.SetTrigger(leftHandGrabController.propType == PropType.Gun ? "leftFire" : "rightFire");
            }
            else
            {
                // punch
                _punching = true;
                anim.SetTrigger(_lastPunchLeft ? "rightPunch" : "leftPunch");
                _lastPunchLeft = !_lastPunchLeft;
            }
        }
        

        void DropEquipment()
        {
            leftHandGrabController.DropEquipment();
            rightHandGrabController.DropEquipment();
        }
        void MatchingAnimation()
        {
            // Update Animation:
            // Arms and Trunks are always controlled by animation & IK;
            foreach (var joint in Trunks)
            {
                Utils.SetTargetRotationInternal(_skeleton[joint], _skeleton[joint].TargetJoint.localRotation, Space.Self);
            }
            foreach (var joint in Arms)
            {
                Utils.SetTargetRotationInternal(_skeleton[joint], _skeleton[joint].TargetJoint.localRotation, Space.Self);
            }

            anim.SetBool("fall", _fall);
            anim.SetBool("run", _moving);
            anim.SetBool("accelerate", _accelerating);
            _ikManager.enableAimIK = _grabbingLeft || _grabbingRight;
            _ikManager.enableLeftArmIK = _grabbingLeft;
            _ikManager.enableRightArmIK = _grabbingRight;
            if (enableAnimControlMovement)
            {
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
                    DetermineStepFoot();
                    MoveLegs();
                }
                else
                {
                    _walkForward = false;
                    _walkBack = false;
                    if (!_grabbingLeft && !_grabbingRight)
                    {
                        Balance();
                    }                    
                    DetermineStepFoot();
                    BalanceLegs();
                }
                // MoveArms();
            }
        }
        void UpdateGrab()
        {
            // var grabScale = 6.0f;
            var grabScale = 0.0f;
            var faceOffset =  Math.Clamp(-grabScale * Mathf.Cos(characterController.faceAngle), -3.0f, 6.0f) ;
            if (characterController.grabLeft)
            {
                leftAnimHandTarget.localPosition = Vector3.Lerp(leftAnimHandTarget.localPosition, new Vector3(-0.8f, 1 + faceOffset, 1f), 0.2f);
                // leftAnimHandTarget.localPosition = Vector3.Lerp(leftAnimHandTarget.localPosition, new Vector3(0, 1 - Mathf.Cos(faceAngle),1), 0.8f);
                _grabbingLeft = true;
                leftHandGrabController.enabled = true;
            }
            else
            {
                _grabbingLeft = false;
                leftHandGrabController.enabled = false;
            } 
            if (characterController.grabRight)
            {
                //rightAnimHandTarget.localPosition = Vector3.Lerp(rightAnimHandTarget.localPosition, new Vector3(0, 1 - Mathf.Cos(faceAngle),1), 0.8f);
                rightAnimHandTarget.localPosition = Vector3.Lerp(rightAnimHandTarget.localPosition, new Vector3(0.8f, 1 + faceOffset, 1f), 0.2f);
                _grabbingRight = true;
                rightHandGrabController.enabled = true;
                //rightHandGrabController.DetectEquipment(out var propPoint);
            }
            else
            {
                _grabbingRight = false;
                rightHandGrabController.enabled = false;
            }
        }
        void UpdatePunch()
        {
            //_punchTarget.position = _rootJoint.RigBody.position + _bodyDirection * 3.5f + Vector3.up * 2f;
            //Debug.DrawLine(_skeleton["UpperChest"].Joint.transform.position,_punchTarget.position, Color.magenta);
            // Punch Left
            if (_punching)
            {
                _punchTimer += Time.fixedDeltaTime;
                if (_punchLeft || _punchRight)
                {
                    var hand=  _punchRight ?  _skeleton["RightHand"] : _skeleton["LeftHand"];
                    var shoulder=  _punchRight ?  _skeleton["RightShoulder"] : _skeleton["LeftShoulder"];
                    var forceDir = (_punchTarget.position - hand.RigBody.position);
                    
                    hand.RigBody.AddForce(punchForce * forceDir);
                    shoulder.RigBody.AddForce(-punchForce * forceDir);
                   // _skeleton["LeftFoot"].RigBody.AddForce(Vector3.down * balanceForce);
                   // _skeleton["RightFoot"].RigBody.AddForce(Vector3.down * balanceForce);
                }
                if (_punchTimer > 1.0f)
                {
                    _punching = false;
                    anim.SetLayerWeight(anim.GetLayerIndex("UpperBody"), 0.0f);
                    _punchTimer = 0;
                }
            }
        }
        void UpdateSwing()
        {
            if (_swing)
            {
                _swingTimer += Time.fixedDeltaTime;
                if (_swingLeftUp || _swingRightUp)
                {
                    var hand=  _swingRightUp ?  _skeleton["RightHand"] : _skeleton["LeftHand"];
                    var shoulder=  _swingRightUp ?  _skeleton["RightShoulder"] : _skeleton["LeftShoulder"];

                    var targetLocalPosition = _swingUpTarget.localPosition;
                    if (_swingRightUp)
                    {
                        targetLocalPosition.x *= -1;
                    }
                    
                    var targetPosition = transform.TransformPoint(targetLocalPosition);
                    Debug.DrawLine(_centerOfMass, targetPosition);
                    var forceDir = (targetPosition- hand.RigBody.position);
                    // var forceDir = Vector3.down + _faceDirection;
                    var forceMag = anim.GetFloat("swingForce");
                    hand.RigBody.AddForce(swingForce * forceMag * forceDir);
                    shoulder.RigBody.AddForce(-swingForce  * forceMag * forceDir);
                    // _skeleton["LeftFoot"].RigBody.AddForce(Vector3.down * balanceForce);
                    // _skeleton["RightFoot"].RigBody.AddForce(Vector3.down * balanceForce);
                    
                }
                if (_swingLeft || _swingRight)
                {
                    var hand=  _swingRight ?  _skeleton["RightHand"] : _skeleton["LeftHand"];
                    var shoulder=  _swingRight ?  _skeleton["RightShoulder"] : _skeleton["LeftShoulder"];
                    var targetLocalPosition = _swingTarget.localPosition;
                    if (_swingRight)
                    {
                        targetLocalPosition.x *= -1;
                    }
                    var targetPosition = transform.TransformPoint(targetLocalPosition);
                    Debug.DrawLine(_centerOfMass, targetPosition);
                    var forceDir = (targetPosition - hand.RigBody.position);
                    var forceMag = anim.GetFloat("swingForce");
                    // var forceDir = Vector3.down + _faceDirection;
                    hand.RigBody.AddForce(swingForce * forceMag * forceDir);
                    shoulder.RigBody.AddForce(-swingForce * forceMag *  forceDir);
                    //  _skeleton["LeftFoot"].RigBody.AddForce(Vector3.down * balanceForce);
                    //  _skeleton["RightFoot"].RigBody.AddForce(Vector3.down * balanceForce);

                }
                if (_swingTimer > 1.0f)
                {
                    _swing = false;
                    anim.SetLayerWeight(anim.GetLayerIndex("UpperBody"), 0.0f);
                    leftHandGrabController.DeactivateProps();
                    rightHandGrabController.DeactivateProps();
                    _swingTimer = 0;
                }
            }
        }
        #endregion
        
        #region Leg Movement
        // Leg Movement:
        void Balance()
        {
            var leftFootPosition = _skeleton["LeftFoot"].Joint.transform.position;
            var rightFootPosition = _skeleton["RightFoot"].Joint.transform.position;
            Debug.DrawLine(_centerOfMass, leftFootPosition);
            Debug.DrawLine(_centerOfMass, rightFootPosition);
            
            var cross = Vector3.Cross(bodyDirection, bodyUp);
            
            var leftFootOffsetX = Vector3.Dot(bodyDirection, leftFootPosition - _centerOfMass);
            var rightFootOffsetX = Vector3.Dot(bodyDirection, rightFootPosition - _centerOfMass);
            var leftFootOffsetY = Vector3.Dot(cross, leftFootPosition - _centerOfMass);
            var rightFootOffsetY = Vector3.Dot(cross, rightFootPosition - _centerOfMass);
            _fall = Mathf.Abs(leftFootOffsetX) > fallFactor ||
                    Mathf.Abs(rightFootOffsetX) > fallFactor ||
                    Mathf.Abs(leftFootOffsetY) > fallFactor ||
                    Mathf.Abs(rightFootOffsetY) > fallFactor;
            // balance Vertical
            if (!_moving)
            {
                _walkBack = leftFootOffsetX > balanceFactor && rightFootOffsetX > balanceFactor;
                _walkForward = leftFootOffsetX < -balanceFactor && rightFootOffsetX < -balanceFactor;
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
        void BalanceLegs()
        {
            float time = timeStep;
            float factor1 = 0, factor2= 0, factor3= 0;
            float upRecover =   balanceUpLegRecoverFactor;
            float lowRecover =  balanceLowLegRecoverFactor;
            if (_walkForward)
            {
                factor1 = forwardUpLegSpeed;
                factor2 = forwardLowLegSpeed;
                factor3 =  forwardOtherLegSpeed;
            }
            if (_walkBack)
            {
                factor1 =  backwardUpLegSpeed;
                factor2 = backwardLowLegSpeed;
                factor3 = - backwardOtherLegSpeed;
            }
            if (_fall)
            {
                time = 0.2f;
                factor1 *= 5;
                factor2 *= 5;
                factor3 *= 5;
            }
            if (_stepRight)
            {
                //抬右腿
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
                //放右腿
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
        void MoveLegs()
        {
            var stepInfo = _movementStepData.forwardStepInfo;
            if (_walkForward)
            {
                stepInfo = _movementStepData.forwardStepInfo;
            }

            if (_walkBack)
            {
                stepInfo = _movementStepData.backwardStepInfo;
            }
            if (_stepLeft)
            {
                var duration = stepInfo.StepDuration;
                var t = Step_L_Time / duration;
                LegMovement(_skeleton["LeftUpLeg"], stepInfo.UpperLegCurve, t,
                    stepInfo.UpperLegTargetAngle);
                LegMovement(_skeleton["LeftLeg"], stepInfo.LowerLegCurve, t,
                    stepInfo.LowerLegTargetAngle);
                LegMovement(_skeleton["RightUpLeg"], stepInfo.OtherUpperLegCurve, t,
                    stepInfo.OtherUpperLegTargetAngle);
                LegMovement(_skeleton["RightLeg"], stepInfo.OtherLowerLegCurve, t,
                    stepInfo.OtherLowerLegTargetAngle);
                Step_L_Time += Time.fixedDeltaTime;
                if (Step_L_Time > duration)
                {
                    Step_L_Time = 0;
                    _stepLeft = false;
                    if (_walkBack || _walkForward)
                    {
                        _stepRight = true;
                    }
                }
            }
            if (_stepRight)
            {
                var duration = stepInfo.StepDuration;
                var t = Step_R_Time / duration;
                LegMovement(_skeleton["RightUpLeg"], stepInfo.UpperLegCurve, t,
                    stepInfo.UpperLegTargetAngle);
                LegMovement(_skeleton["RightLeg"], stepInfo.LowerLegCurve, t,
                    stepInfo.LowerLegTargetAngle);
                LegMovement(_skeleton["LeftUpLeg"], stepInfo.OtherUpperLegCurve, t,
                    stepInfo.OtherUpperLegTargetAngle);
                LegMovement(_skeleton["LeftLeg"], stepInfo.OtherLowerLegCurve, t,
                    stepInfo.OtherLowerLegTargetAngle);
                Step_R_Time += Time.fixedDeltaTime;
                if (Step_R_Time > duration)
                {
                    Step_R_Time = 0;
                    _stepRight = false;
                    if (_walkBack || _walkForward)
                    {
                        _stepLeft = true;
                    }
                }
            }
        }
        void LegMovement(SkeletonStruct skeleton, AnimationCurve curve, float time, float targetAngle)
        {
            var angle = -curve.Evaluate(time)*targetAngle;
            var rot = Quaternion.Euler(0, 0, angle) * skeleton.TargetInitialRotation;
            Utils.SetTargetRotationInternal(skeleton,rot , Space.Self);
        }
        #endregion
        
        #region  Utils Function
        // Useful function:
        bool GetSteppingSurface(SkeletonStruct joint, float distance,  out RaycastHit info)
        {
            Ray ray = new Ray(joint.Joint.transform.position, Vector3.down);
            return Physics.Raycast(ray, out info, distance, groundLayer);
        }
        void IgnoreCollision()
        {
            foreach (var joint in _skeleton.Values)
            {
                Physics.IgnoreCollision(joint.Joint.GetComponent<Collider>(), _rootJoint.Joint.GetComponent<Collider>());
                foreach (var joint2 in _skeleton.Values)
                {
                    Physics.IgnoreCollision(joint.Joint.GetComponent<Collider>(), joint2.Joint.GetComponent<Collider>());
                }
            }

            var enableCollision1 = new[] {
                "LeftForeArm","RightForeArm",
                "LeftHand", "RightHand"
            };
            var enableCollision2 = new[] {
                "Head","Chest",
                "Spine"
            };
            foreach (var joint1 in enableCollision1)
            {
                foreach (var joint2 in enableCollision2)
                {
                    Physics.IgnoreCollision(_skeleton[joint1].Joint.GetComponent<Collider>(), _skeleton[joint2].Joint.GetComponent<Collider>(), false);
                }
            }

            //Physics.IgnoreCollision(_skeleton["LeftArm"].Joint.GetComponent<Collider>(),_skeleton["UpperChest"].Joint.GetComponent<Collider>());
            //Physics.IgnoreCollision(_skeleton["RightArm"].Joint.GetComponent<Collider>(),_skeleton["UpperChest"].Joint.GetComponent<Collider>());
        }
        void SetLimbsStrength()
        {
            foreach (var joint in Arms)
            {
                Utils.SetJointStrength(_skeleton[joint].Joint, _targetArmDrive.x, _targetArmDrive.x, _targetArmDrive.y);
            }
            foreach (var joint in Trunks)
            {
                Utils.SetJointStrength(_skeleton[joint].Joint, _targetTrunkDrive.x,_targetTrunkDrive.x, _targetTrunkDrive.y);
            }
            var hipDriveSpring = _skeleton["Hips"].Joint.angularXDrive.positionSpring;
            hipDriveSpring = Mathf.Lerp(hipDriveSpring, _targetHipDrive.x, 0.1f);
            Utils.SetJointStrength(_skeleton["Hips"].Joint, hipDriveSpring, hipDriveSpring, _targetHipDrive.y);
            Utils.SetJointStrength(_rootJoint.Joint, hipDriveSpring, hipDriveSpring, _targetHipDrive.y);
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
            characterController.enabled = false;
            leftHandGrabController.DropEquipment();
            rightHandGrabController.DropEquipment();
            leftHandGrabController.enabled = false;
            rightHandGrabController.enabled = false;
            //_rootJoint.RigBody.mass = 0.3f;
            Utils.SetJointStrength(_rootJoint.Joint, 0,0,1f);
            foreach (var joint in Arms)
            {
                //_skeleton[joint].RigBody.mass = 0.2f;
                Utils.SetJointStrength(_skeleton[joint].Joint, 0,0,20f);
            }
            foreach (var joint in Trunks)
            {
                //_skeleton[joint].RigBody.mass = 0.3f;
                Utils.SetJointStrength(_skeleton[joint].Joint, 0,0,1f);
            }
            //_skeleton["Head"].RigBody.mass = 1.2f;
            foreach (var joint in Legs)
            {
                //_skeleton[joint].RigBody.mass = 0.2f;
                Utils.SetJointStrength(_skeleton[joint].Joint, 0,0, 10f);
            }
        }
        public void Recover(){
            _rootJoint.RigBody.mass = _rootJoint.InitialMass;
            foreach (var joint in _skeleton.Values)
            {
                joint.RigBody.mass = joint.InitialMass;
            }
            _knockout = false;
            characterController.enabled = true;
            _rootJoint.RigBody.detectCollisions = true;
        }

        public bool GrabbedRight(out string grabbedTag)
        {
            grabbedTag = rightHandGrabController.grabbedTag;
            return rightHandGrabController.grabbing;
        }
        public bool GrabbedLeft(out string grabbedTag)
        {
            grabbedTag = leftHandGrabController.grabbedTag;
            return leftHandGrabController.grabbing;
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
        
        

        #endregion
        
        #region EventHandler
        
        /// <summary>
        ///  hand = -1 : leftHand
        ///  hand = 1 : rightHand
        /// </summary>
        /// <param name="hand"></param>
        public void PunchStart(int hand)
        {
            if (hand == -1)
            {
                _punchLeft = true;
                _punchTimer = 0;
            }
            else if (hand == 1)
            {
                _punchRight = true;
                _punchTimer = 0;
            }
        }
        public void PunchEnd(int hand)
        {
            if (hand == -1)
            {
                _punchLeft = false;
            }
            else if (hand == 1)
            {
                _punchRight = false;
            }
        }
        
        public void SwingUp(int hand)
        {
            if (hand == -1)
            {
                _swingLeftUp = true;
            }
            else if (hand == 1)
            {
                _swingRightUp = true;
            }
        }
        public void SwingStart(int hand)
        {
            if (hand == -1)
            {
                _swingLeftUp = false;
                _swingLeft = true;
            }
            else if (hand == 1)
            {
                _swingRightUp = false;
                _swingRight = true;
            }
        }
        public void SwingEnd(int hand)
        {
            if (hand == -1)
            {
                _swingLeft = false;
            }
            else if (hand == 1)
            {
                _swingRight = false;
            }
        }
        
        public void SetFireState(bool b)
        {
            
            anim.SetBool("fire", b);
        }
        
        public void Shoot()
        {
            leftHandGrabController.ActivateProps();
            rightHandGrabController.ActivateProps();
        }
        #endregion

    }
}