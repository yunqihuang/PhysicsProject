using System;
using UnityEngine;

namespace ActiveRagdoll
{
    public class Equipment : Props
    {
        
        public override void Initialize(Transform own)
        {
            owner = own;
        }
        
        public override void Activate()
        {

            if (!isActive)
            {
                isActive = true;
            }
        }
        
        public override void Deactivate()
        {
            isActive = false;
        }
    }
}