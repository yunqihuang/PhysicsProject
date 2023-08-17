using System;
using System.Collections.Generic;
using System.Linq;
using RPGCharacterAnims.Extensions;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ActiveRagdoll.Gameplay
{
    public class EnemyManager : MonoBehaviour
    {
        [SerializeField]
        public Dictionary<int, AIController> activeEnemies;
        public int leader;
        
        public void AddEnemy(int id, AIController aiController)
        {
            activeEnemies.Add(id, aiController);
            leader = id;
        }
        
        public void DeleteEnemy(int id)
        {
            activeEnemies.Remove(id);
            if (leader == id && activeEnemies.Count > 0)
            {
                leader = activeEnemies.TakeRandom().Key;
            }
        }

        private void Update()
        {
            if (activeEnemies.Count > 0 && activeEnemies[leader].enabled == false)
            {
                leader = activeEnemies.TakeRandom().Key;
            }
        }

        private void Awake()
        {
            activeEnemies = new Dictionary<int, AIController>();
        }
    }
}