using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace ActiveRagdoll
{
    public class HeadHitter : MonoBehaviour
    {
        public Transform activeRagDoll;
        public LayerMask hitLayerMask;
        public PhysicalBodyController controller;
        public float knockOutThreshold;
        public float recoverPerFrame;
        
        [SerializeField]
        private float accumulateHitForce;
        private float _recover;

        private void Start()
        {
            _recover = recoverPerFrame;
        }

        void Update()
        {
            
            if (accumulateHitForce > knockOutThreshold)
            {
                controller.Knockout();
            }

            if (accumulateHitForce < recoverPerFrame)
            {
                controller.Recover();
            }
        }

        private void FixedUpdate()
        {
            if (accumulateHitForce > recoverPerFrame)
            {
                accumulateHitForce -= _recover;
            }
        }

        private void OnCollisionEnter(Collision col)
        {
            
            if (accumulateHitForce < knockOutThreshold && col.rigidbody != null
                && !col.rigidbody.transform.IsChildOf(activeRagDoll))
            {
                var layer = col.rigidbody.gameObject.layer;
                if (hitLayerMask == (hitLayerMask | (1 << layer)))
                {
                    var contactNormal = col.GetContact(0).normal;
                    var hitForce = Vector3.Dot(col.impulse / Time.fixedDeltaTime, contactNormal);
                    accumulateHitForce += Mathf.Abs(hitForce);
                }
            }

            // Debug.Log($"{col.impulse}, ${collisionForce}, {Time.fixedDeltaTime}");
        }
    }
}