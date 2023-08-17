using System;
using System.Collections;
using System.Collections.Generic;
using ActiveRagdoll;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class TestCannon :MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform fireStart;
    
    public float duration;
    public float minForce;
    public float maxForce;
    private float _gap;
    private Quaternion _targetRotation;

    private ParticleSystem _particleSystem;
    // Start is called before the first frame update
    void Start()
    {
        _gap = 0;
        _particleSystem = GetComponentInChildren<ParticleSystem>();
    }

    // Update is called once per frame

    void FixedUpdate()
    {


        transform.localRotation = Quaternion.Slerp(transform.localRotation, _targetRotation, Time.fixedDeltaTime);
        _gap += Time.fixedDeltaTime;
        if (_gap > duration)
        {
            _targetRotation = Quaternion.Euler(0, Random.Range(-30, 30), 0);
            Fire();
            _gap = 0;
        }
    }

    void Fire()
    {
        
        _particleSystem.Play();
        var thisTransform = fireStart;
        var originPos = thisTransform.position;
        GameObject bullet = Instantiate(bulletPrefab, originPos, thisTransform.rotation);
        bullet.GetComponent<CannonBall>().Launch(thisTransform.forward, Random.Range(minForce, maxForce));
    }
}
