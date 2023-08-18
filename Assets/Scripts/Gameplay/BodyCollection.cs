using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ActiveRagdoll
{
    public class BodyCollection : MonoBehaviour
    {
        public LayerMask destroyLayer;
        public GameManager gameManager;
        void OnTriggerEnter(Collider other)
        {
            if (( (1 << other.gameObject.layer ) & destroyLayer) > 0)
            {
                gameManager.DestroyNpc(other.transform.root);
            }
        
            if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                gameManager.ResetPlayer(other.transform);
            }
        }
    }

}
