

using System.Collections.Generic;
using RPGCharacterAnims.Extensions;
using UnityEngine;

namespace ActiveRagdoll
{
    public class DamageEffect : MonoBehaviour
    {
        public bool isActive;
        public GameObject normalVfxPrefab;
        public GameObject heavyVfxPrefab;
        public float duration;
        public void SpawnVFX(Vector3 position, Vector3 direction, bool heavy)
        {

            var vfxPrefab = heavy ? heavyVfxPrefab : normalVfxPrefab;
            float scale = heavy ? 1.5f : 0.4f;
            GameObject vfx = Instantiate(vfxPrefab, position, Quaternion.LookRotation(direction));
            var particles = vfx.GetComponentsInChildren<ParticleSystem>();
            foreach (var particle in particles)
            {
                var mainModule = particle.main;
                var originStartSize = mainModule.startSize;
                mainModule.startSize = scale * originStartSize.constantMax;
            }
            
           
            Destroy(vfx, duration);
            vfx.SetActive(true);
        }
    }
}