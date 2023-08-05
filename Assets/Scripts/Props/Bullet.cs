using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{    
    public int lastTime;
    public int mass;
    private Rigidbody rig;
    // Start is called before the first frame update
    void Start()
    {
        
        Destroy(gameObject, lastTime);
    }
    private void Awake()
    {
        rig = GetComponent<Rigidbody>();
        rig.mass = mass;
    }

    public void Launch(Vector3 direction, float force)
    {
        rig.AddForce(direction * force, ForceMode.Impulse);
    }
}
