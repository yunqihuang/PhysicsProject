using System;
using System.Collections.Generic;
using UnityEngine;

namespace ActiveRagdoll
{
    public class Gun : Props
    {
        public LayerMask canHitLayer;
        public GameObject prefab;
        public Transform fireStart;
        public float force;
        public float range;
        public float angle;
        
        private PhysicalBodyController _body;
        void AlignRotation()
        {
            var targetRotation = owner.rotation;
            Utils.SetTargetRotationInternal(skeleton, targetRotation, Space.World);
        }
        
        public override void Disable()
        {
            owner = null;
            _body.SetFireState(false);
            _body = null;
        }

        
        public override void Initialize(Transform own)
        {
            owner = own;
            _body = owner.GetComponent<PhysicalBodyController>();
            AlignRotation();
            _body.SetFireState(true);
        }
        
        public override void Activate()
        {
            isActive = true;
            Fire();
        }
        
        
        public override void Deactivate()
        {
            isActive = false;
        }
        
        void Fire()
        {
            var height = 2f;
            var forward = owner.forward;
            var targetPosition = owner.position + range * forward+ height * Vector3.up;
            var origin = fireStart.position;

            var hits = Physics.OverlapSphere(origin, range, canHitLayer);
            foreach (var item in hits)
            {
                var targetPos = item.transform.position;
                var dir = (targetPos - origin).normalized;
                var cosValue = Vector3.Dot(dir, forward);
                Debug.Log($"detect {item.transform.name}, {cosValue},{Mathf.Cos(Mathf.Deg2Rad * angle)}");
                if (!item.transform.IsChildOf(owner) && cosValue > Mathf.Cos(Mathf.Deg2Rad * angle))
                {
                    targetPosition = item.transform.position;
                }
            }
            
            /*
            float length = range * 2 * Mathf.PI / (360 / angle);
            int rayCount = (int)length;
            float space = angle / rayCount;
            for (int i = 0; i < rayCount + Convert.ToInt32(angle != 360); i++)
            {

                Vector3 dir = Quaternion.AngleAxis(angle / 2 - space * i, Vector3.up) * forward;
                Debug.DrawLine(origin, origin + range*dir, Color.red, 2.0f);
                var hits = Physics.RaycastAll(origin, dir, range, canHitLayer);
                foreach (var item in hits)
                {
                    Debug.Log(item.transform.name);
                    if (!item.transform.IsChildOf(owner))
                    {
                        targetPosition = item.point;
                    }
                }
            }
            */
            transform.LookAt(targetPosition);

            fireStart.LookAt(targetPosition);
            var rot = fireStart.rotation * Quaternion.Euler(90, 0, 0);
            GameObject bullet = Instantiate(prefab, fireStart.position, rot);
            bullet.GetComponent<Bullet>().Launch(owner, fireStart.forward, force);
        }
    }
}