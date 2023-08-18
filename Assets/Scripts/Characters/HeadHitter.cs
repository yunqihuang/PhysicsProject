using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;
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
                if ((hitLayerMask & (1 << layer)) > 0)
                {
                    var damageEffect = col.gameObject.GetComponent<DamageEffect>();
                    if (damageEffect != null && damageEffect.isActive)
                    {
                        var contactPosition = col.GetContact(0).point;
                        var contactNormal = col.GetContact(0).normal;
                        var hitForce = Mathf.Abs(col.impulse.magnitude / Time.fixedDeltaTime);

                        var propTrigger = col.gameObject.GetComponent<Props>();

                        //Additional Force:
                        GetComponent<Rigidbody>().AddForceAtPosition(col.impulse, contactPosition, ForceMode.Impulse);
                        if (propTrigger != null)
                        {
                            if (activeRagDoll == propTrigger.owner || !propTrigger.isActive)
                            {
                                hitForce = 0;
                            }
                        }


                        Debug.Log($"{transform.root.name} get hit by {col.transform.root.name}, force:{hitForce}");
                        
                        damageEffect.SpawnVFX(contactPosition, contactNormal, hitForce > knockOutThreshold);
                        
                        // accumulateHitForce += Mathf.Abs(hitForce);
                        accumulateHitForce = hitForce;
                        
                    }
                    

                }
            }
            // Debug.Log($"{col.impulse}, ${collisionForce}, {Time.fixedDeltaTime}");
        }
    }
}