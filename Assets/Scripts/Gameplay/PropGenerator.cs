using System.Collections;
using System.Collections.Generic;
using RPGCharacterAnims.Extensions;
using UnityEngine;

public class PropGenerator : MonoBehaviour
{
    
    public Bounds activeBounds;
    
    public List<GameObject> propPrefabs;

    public float interval;

    private float generateTimer;
    // Start is called before the first frame update
    void Start()
    {
        activeBounds = GetComponent<BoxCollider>().bounds;
    }

    // Update is called once per frame
    void Update()
    {
        generateTimer += Time.deltaTime;
        if (generateTimer > interval)
        {
            generateTimer = 0;
            if (Random.Range(0.0f, 1.0f) > 0.5f)
            {
                var position = activeBounds.center;
                var randomX = Random.Range(-0.5f, 0.5f);
                var randomZ = Random.Range(-0.5f, 0.5f);
                position.x += randomX * activeBounds.size.x;
                position.z += randomZ * activeBounds.size.z;
                var layerMask = 1 << LayerMask.NameToLayer("Static Scene");

                var prefab = propPrefabs.TakeRandom();
                if (Physics.Raycast(position, Vector3.down, 100.0f, layerMask))
                {
                    GameObject prop = Instantiate(prefab, position, Quaternion.identity);
                    prop.SetActive(true);
                }
            }
        }
    }
}
