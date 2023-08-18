using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace  ActiveRagdoll
{
    public class Bullet : Props
    {    
        public int duration;
        public float mass;
        private Rigidbody rig;
        
        private DamageEffect _damageEffect;
        // Start is called before the first frame update
        void Start()
        {
            _damageEffect = GetComponent<DamageEffect>();
            Destroy(gameObject, duration);
            isActive = true;
            _damageEffect.isActive = true;
        }
        private void Awake()
        {
            rig = GetComponent<Rigidbody>();
            rig.mass = mass;
        }

        public void Launch(Transform own, Vector3 direction, float force)
        {
            owner = own;
            rig.AddForce(direction * force, ForceMode.Impulse);
        }
    }
}

