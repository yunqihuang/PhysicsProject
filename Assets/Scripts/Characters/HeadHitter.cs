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
        public float recoverTime;
        
        [SerializeField]
        private float accumulateHitForce;
        [SerializeField]
        private float _recover;
        
        private bool _knockout;
        private void Start()
        {
            _knockout = false;
            _recover = knockOutThreshold * Time.fixedDeltaTime / recoverTime;
        }

        void Update()
        {
            
        }
        

        private void FixedUpdate()
        {
            if (!_knockout && accumulateHitForce > knockOutThreshold)
            {
                _knockout = true;
                controller.Knockout();
            }
            
            if (_knockout)
            {
                if (accumulateHitForce > _recover)
                {                
                    accumulateHitForce -= _recover;
                }
                else
                {
                    _knockout = false;
                    controller.Recover();
                }
            }
        }

        public void AddHitForce(float force)
        {
            if (!_knockout)
            {
                accumulateHitForce = force;
            }
        }

        private void OnCollisionEnter(Collision col)
        {
            if (_knockout)
            {
                return;
            }
            
            if (accumulateHitForce < knockOutThreshold && col.rigidbody != null
                && !col.rigidbody.transform.IsChildOf(activeRagDoll))
            {
                var layer = col.rigidbody.gameObject.layer;
                if (hitLayerMask == (hitLayerMask | (1 << layer)))
                {
                    
                    var contactNormal = col.GetContact(0).normal;
                    var hitForce = Mathf.Abs(Vector3.Dot(col.impulse / Time.fixedDeltaTime, contactNormal));

                    var propTrigger = col.gameObject.GetComponent<Props>();
                    
                    if (propTrigger != null)
                    {
                        // Debug.Log($"{activeRagDoll.name} get hit by {propTrigger.owner}, hitter: {col.gameObject.name}, force:{hitForce}");
                        if (activeRagDoll == propTrigger.owner)
                        {
                            hitForce = 0;
                        }
                        else if (propTrigger.isActive)
                        {
                            hitForce *= 3;
                        }
                    }
                    // accumulateHitForce += Mathf.Abs(hitForce);
                    accumulateHitForce = hitForce;
                }
            }
            // Debug.Log($"{col.impulse}, ${collisionForce}, {Time.fixedDeltaTime}");
        }
    }
}