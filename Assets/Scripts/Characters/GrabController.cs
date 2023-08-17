using System;
using System.Collections.Generic;
using RootMotion.FinalIK;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace ActiveRagdoll
{
    public class GrabController : MonoBehaviour
    {
        public string grabbedTag;
        
        
        public Transform activeRagDoll;
        public Transform holdAnchor;
        public Props props;
        public LayerMask grabLayer;
        public bool grabbing;
        public PropType propType;
        
        private Rigidbody _lastCollision;
        private ConfigurableJoint _joint;
        
        private Rigidbody _handRigBody;
        private Props _targetProp;

        private JointDrive _drive;

        private static readonly string NoGrabTag = "None";
        private void Start()
        {
            enabled = false;
            grabLayer = LayerMask.GetMask("Props");
            _handRigBody = GetComponent<Rigidbody>();
            _targetProp = null;
            grabbing = false;
            grabbedTag = NoGrabTag;
            propType = PropType.None;
            _drive = new JointDrive()
            {
                positionSpring = 5000,
                positionDamper = 20,
                maximumForce = 6000
            };
        }
        

        private void FixedUpdate()
        {
            if (grabbing)
            {
                return;
            }
            
            
            if (_targetProp == null)
            {
                DetectEquipment(out var propPoint);
                _targetProp = propPoint;
                grabLayer = LayerMask.GetMask("Props", "Static Scene","Trunk", "Head");
            }
            else
            { 
                var forceDir = _targetProp.GetContactPoint() - _handRigBody.position; 
                _handRigBody.AddForce(forceDir * 400.0f);
                grabLayer = LayerMask.GetMask("Props");
                if (forceDir.magnitude > 1.5f)
                {
                    _targetProp = null;
                }
            }
        }

        public void ActivateProps()
        {
            if (props != null)
            {
                props.Activate();
            }
        }

        public void DeactivateProps()
        {

            if (props != null)
            {           

                props.Deactivate();
            }
        }

        public void DetectEquipment(out Props equipment)
        {
            equipment = null;
            if (Physics.SphereCast(transform.position, 0.6f, Vector3.down ,out var hitInfo, 3.0f, grabLayer))
            {
                equipment = hitInfo.transform.GetComponent<Props>();
                if (equipment != null)
                {
                    Debug.DrawLine(transform.position, hitInfo.point);
                    // Debug.Log($"{equipment.name}, {equipment.transform.position}");
                }
            }
        }

        public void DropEquipment()
        {
            if (propType !=  PropType.None)
            {
                props.transform.SetParent(null, true);
                props.transform.localScale = props.originScale;
                Physics.IgnoreCollision(_joint.GetComponent<Collider>(), _handRigBody.GetComponent<Collider>(), false);
                propType = PropType.None;
            }
            Release();
        }
        private void Grab(Rigidbody obj) {
            if (!enabled) {
                _lastCollision = obj;
                return;
            }

            if (_joint != null)
                return;

            if (obj.transform.IsChildOf(activeRagDoll))
                return;
            
            grabbing = true;
            
            grabbedTag = obj.gameObject.tag;


            props  = obj.GetComponent<Props>();
            if (props != null)
            {
                Debug.Log(props.transform.parent);
                props.transform.SetParent( _handRigBody.transform, true);
                // props.transform.localRotation = Quaternion.identity;
                
                _joint = obj.AddComponent<ConfigurableJoint>();
                _joint.anchor = props.GetLocalContactPoint();
                _joint.connectedBody = _handRigBody;
                Physics.IgnoreCollision(obj.GetComponent<Collider>(), _handRigBody.GetComponent<Collider>());
                _joint.xMotion = ConfigurableJointMotion.Locked;
                _joint.yMotion = ConfigurableJointMotion.Locked;
                _joint.zMotion = ConfigurableJointMotion.Locked;
                _joint.angularXDrive = _drive;
                _joint.angularYZDrive = _drive;
                _joint.configuredInWorldSpace = true;

                var skeletonStruct = new SkeletonStruct();
                skeletonStruct.Joint = _joint;
                skeletonStruct.RigBody = _joint.GetComponent<Rigidbody>(); 
                skeletonStruct.TargetInitialRotation = _joint.transform.rotation;
                var right = _joint.axis;
                var forward = Vector3.Cross (right, _joint.secondaryAxis).normalized;
                var up = Vector3.Cross (forward, right).normalized;
                skeletonStruct.WorldToJointSpace = Quaternion.LookRotation(forward, up);

                props.skeleton = skeletonStruct;
                
                var targetRotation = holdAnchor.rotation;
                Utils.SetTargetRotationInternal(skeletonStruct,targetRotation, Space.World);
                
                // targetRotation *= Quaternion.Inverse(_joint.transform.rotation);
                // _joint.targetRotation = Quaternion.Inverse (targetRotation);
                propType = props.propType;
                _joint.autoConfigureConnectedAnchor = false;
                _joint.connectedAnchor = holdAnchor.localPosition;
                
                props.Initialize(activeRagDoll);
            }
            else
            {
                _joint = gameObject.AddComponent<ConfigurableJoint>();
                _joint.connectedBody = obj;
                _joint.xMotion = ConfigurableJointMotion.Locked;
                _joint.yMotion = ConfigurableJointMotion.Locked;
                _joint.zMotion = ConfigurableJointMotion.Locked;
            }
        }

        private void Release() {
            if (_joint == null)
                return;

            if (propType != PropType.None)
            {
                return;
            }
            
            Destroy(_joint);
            _joint = null;

            grabbedTag = NoGrabTag;
            
            if (props != null)
            {
                props.Disable();
            }
            props = null;
            grabbing = false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.rigidbody != null && ((1 << collision.rigidbody.gameObject.layer) & grabLayer) > 0)
                Grab(collision.rigidbody);
        }

        private void OnTriggerEnter(Collider other) {
            if (other.attachedRigidbody != null)
                Grab(other.attachedRigidbody);
        }

        private void OnCollisionExit(Collision collision) {
            if (collision.rigidbody == _lastCollision)
                _lastCollision = null;
        }

        private void OnTriggerExit(Collider other) {
            if (other.attachedRigidbody == _lastCollision)
                _lastCollision = null;
        }
        
        private void OnEnable() {
            if (_lastCollision != null)
                Grab(_lastCollision);
        }

        private void OnDisable() {
            Release();
            _targetProp = null;
        }
    }
}