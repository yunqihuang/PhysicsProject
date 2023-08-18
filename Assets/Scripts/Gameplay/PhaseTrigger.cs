using System;
using System.Collections;
using System.Collections.Generic;
using ActiveRagdoll;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace ActiveRagdoll
{
    public class PhaseTrigger : MonoBehaviour
    {
        public GameManager gameManager;
        
        public Transform birthPlace;
        public int totalNumber;
        public int maxNumber;
        public int levelID;
        public bool triggered;
    
        public List<GameObject> needActivateOnBegin;
        public List<GameObject> needActivateOnFinished;
        public List<GameObject> needCloseOnBegin;
        public List<GameObject> needCloseOnFinished;
        // Start is called before the first frame update
    
        void OnTriggerEnter(Collider other)
        {
            if (!triggered && other.gameObject == gameManager.player.gameObject)
            {
                triggered = true;
                var region = GetComponent<BoxCollider>().bounds;
                gameManager.ActivateRegion(region, this, birthPlace.position, totalNumber, maxNumber);
                
                foreach (var item in needActivateOnBegin)
                {
                    item.SetActive(true);
                }
                foreach (var item in needCloseOnBegin)
                {
                    item.SetActive(false);
                }
            }
        }
    
        public void Deactivate()
        {
            if (triggered)
            {
                foreach (var item in needActivateOnFinished)
                {
                    item.SetActive(true);
                }
                foreach (var item in needCloseOnFinished)
                {
                    item.SetActive(false);
                }
            }
            
        }
    }
}

