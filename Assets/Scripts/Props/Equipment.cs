using System;
using UnityEngine;

namespace ActiveRagdoll
{
    public class Equipment : Props
    {
        private DamageEffect _damageEffect;

        void Start()
        {
            originScale = transform.localScale;
            _damageEffect = GetComponent<DamageEffect>();
        }
        public override void Initialize(Transform own)
        {
            owner = own;
            
        }
        
        public override void Activate()
        {

            if (!isActive)
            {
                isActive = true;
                _damageEffect.isActive = true;
            }
        }
        
        public override void Deactivate()
        {
            isActive = false;
            _damageEffect.isActive = false;
        }
    }
}