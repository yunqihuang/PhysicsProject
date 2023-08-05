using System;
using System.Collections;
using System.Collections.Generic;
using ActiveRagdoll;
using Unity.VisualScripting;
using UnityEngine;

public class TestCannon : PropTrigger
{
    public Transform target;
    public GameObject prefab;
    public Transform fireStart;
    
    public float duration;
    public float force;

    private float _gap;

    private int _touchCount;
    // Start is called before the first frame update
    void Start()
    {
        _gap = 0;
        _touchCount = 0;
    }

    // Update is called once per frame
    void Update()
    {

        if (_touchCount > 0)
        {
            transform.LookAt(target);
            _gap += Time.deltaTime;
            if (_gap > duration)
            {
                Fire();
                _gap = 0;
            }
        }
    }

    public override void Trigger()
    {
        _touchCount++;
    }
    public override void Release()
    {
        _touchCount--;
    }
    void Fire()
    {
        var thisTransform = fireStart;
        var originPos = thisTransform.position;
        GameObject bullet = Instantiate(prefab, originPos, thisTransform.rotation);
        bullet.GetComponent<Bullet>().Launch(thisTransform.forward, force);
    }
}
