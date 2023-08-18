using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using ActiveRagdoll;
using ActiveRagdoll.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace ActiveRagdoll
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager gameManager;
        
        [Header("Fade")]
        [SerializeField] Animator fadeAnimator;
        
        [SerializeField]
        string sceneToLoad;
        
        public Transform player;
        public bool pause;

        public int maxLevel;
        
        public GameObject npcPrefab;
        public bool isActivate;
        public Bounds activeBounds;
        public Vector3 birthPlace;
        public PhaseTrigger phaseTrigger;
    
        [SerializeField] 
        private GameObject menuPanel;
        
        public TMP_Text level;
        public TMP_Text enemyNumber;
        private float _refreshTimer;
        
        private int _npcID;
    
        private int _maxNpcNumber;
        private int _currentNpcNumber;
        private int _totalNpcNumber;
        private int _remainNpcNumber;
    
        private EnemyManager _enemyManager;
        // Start is called before the first frame update
        void Awake()
        {
            _currentNpcNumber = 0;
            isActivate = false;
            _npcID = 0;
            _enemyManager = GetComponent<EnemyManager>();
        }

            // Update is called once per frame

        void Update()
        {
             if (Input.GetKeyDown(KeyCode.Escape))
             {
                 Cursor.lockState =  CursorLockMode.Confined;
                 Cursor.visible = true;
                 Time.timeScale = 0;
                 pause = true;
                 menuPanel.SetActive(true);
             }
             
             if (isActivate)
             {
                 level.SetText($"Level {phaseTrigger.levelID.ToString()}");
                 enemyNumber.SetText($"Enemy Number: {_totalNpcNumber.ToString()}");
                 _refreshTimer += Time.deltaTime;
                 if (_totalNpcNumber == 0)
                 {
                     isActivate = false;
                     phaseTrigger.Deactivate();
                 }
                 else if (_remainNpcNumber > 0 && _currentNpcNumber < _maxNpcNumber && _refreshTimer > 1.0f)
                 {
                     _refreshTimer = 0;
                     GenerateNpc();
                 }
             }
             else
             {
                 level.SetText("");
                 enemyNumber.SetText("");
             }
         
        }
    
        public void ResetPlayer(Transform playerTransform)
        {
            playerTransform.position = birthPlace;
            playerTransform.GetComponentInChildren<PhysicalBodyController>().Recover();
        }
    
        public void DestroyNpc(Transform npc)
        {
            Debug.Log("Destroy "+ npc.gameObject.name);
            _enemyManager.DeleteEnemy(npc.GetComponentInChildren<AIController>().id);
            Destroy(npc.gameObject);
            _currentNpcNumber--;
            _totalNpcNumber--;
        }
    
        public void GenerateNpc()
        {
            var position = activeBounds.center;
            var randomX = Random.Range(-0.5f, 0.5f);
            var randomZ = Random.Range(-0.5f, 0.5f);
            position.x += randomX * activeBounds.size.x;
            position.z += randomZ * activeBounds.size.z;
            Debug.DrawLine(position, position + Vector3.down * 100.0f,Color.red, 2.0f);
            var layerMask = 1 << LayerMask.NameToLayer("Static Scene");
            if (Physics.Raycast(position, Vector3.down, 100.0f, layerMask))
            {
                GameObject npc = Instantiate(npcPrefab, position, Quaternion.identity);
                var aiController = npc.GetComponentInChildren<AIController>();
                aiController.gameManager = this;
                aiController.target = player;
                aiController.id = _npcID;
                aiController.enemyManager = _enemyManager;
                npc.SetActive(true);
                npc.name = "NPC" + _npcID;
                _enemyManager.AddEnemy(_npcID, aiController);
                _currentNpcNumber ++;
                _npcID++;       
                _remainNpcNumber--;
            }
        }
    
        public void ActivateRegion(Bounds bounds, PhaseTrigger trigger, Vector3 place, int total, int max)
        {
            
            isActivate = true;
            phaseTrigger = trigger;
            activeBounds = bounds;
            _maxNpcNumber = max;
            _remainNpcNumber = total;
            _totalNpcNumber = total;
            birthPlace = place;
        }

        public void MenuCancel()
        {

            Time.timeScale = 1;
            pause = false;
            menuPanel.SetActive(false);
        }

        public void LoadScene()
        {
            MenuCancel();
            
            fadeAnimator.SetTrigger("FadeOut");
            StartCoroutine(WaitToLoadLevel());
        }
        
        IEnumerator WaitToLoadLevel()
        {
            yield return new WaitForSeconds(2f);

            Cursor.lockState =  CursorLockMode.Confined;
            Cursor.visible = true;
            // Scene Load
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}

