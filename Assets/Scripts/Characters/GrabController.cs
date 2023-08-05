using System;
using System.Collections.Generic;
using RootMotion.FinalIK;
using Unity.VisualScripting;
using UnityEngine;

namespace ActiveRagdoll
{
    public class GrabController : MonoBehaviour
    {
        public Transform activeRagDoll;
        private Rigidbody _lastCollision;
        private ConfigurableJoint _joint;
        private PropTrigger _propTrigger;
        private void Start()
        {
            enabled = false;
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
            
            _joint = gameObject.AddComponent<ConfigurableJoint>();
            _joint.connectedBody = obj;
            _joint.xMotion = ConfigurableJointMotion.Locked;
            _joint.yMotion = ConfigurableJointMotion.Locked;
            _joint.zMotion = ConfigurableJointMotion.Locked;

            _propTrigger = obj.GetComponent<PropTrigger>();
            if (_propTrigger != null)
            {
                _propTrigger.Trigger();
            }
            
        }

        private void Release() {
            if (_joint == null)
                return;

            Destroy(_joint);
            _joint = null;
            if (_propTrigger != null)
            {
                _propTrigger.Release();
            }

            _propTrigger = null;
        }

        private void OnCollisionEnter(Collision collision) {
            if (collision.rigidbody != null)
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
        }
    }
}