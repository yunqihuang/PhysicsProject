using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace ActiveRagdoll
{
    public enum PropType
    {
       None, Gun, Equipment
    }
    public class Props :MonoBehaviour
    {
        public Transform owner;
        public bool isActive;
        public PropType propType;
        
        public  SkeletonStruct skeleton;
        [SerializeField]
        protected Transform _contactPoint;

        public Vector3 originScale;


        void Start()
        {
            originScale = transform.localScale;
        }

        public Vector3 GetContactPoint()
        {
            return _contactPoint.position;
        }

        public Vector3 GetLocalContactPoint()
        {
            return _contactPoint.localPosition;
        }
        
        public virtual void Initialize(Transform own)
        {
            
        }
        
        public virtual void Disable()
        {
            owner = null;
        }
        
        public virtual void Activate()
        {
            
        }
        public virtual void Deactivate()
        {
            
        }
        
        
    }
}