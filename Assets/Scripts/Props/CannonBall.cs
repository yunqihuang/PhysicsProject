using System;
using System.Collections;
using System.Collections.Generic;
using ActiveRagdoll;
using UnityEngine;



public class CannonBall : MonoBehaviour
{
    public LayerMask layerMask;
    public int duration;
    public float mass;
    public float radius;
    public float power;
    public GameObject explosionEffect;
    private Rigidbody rig;

    // Start is called before the first frame update
    void Start()
    {

        Destroy(gameObject, duration);
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

    private void OnCollisionEnter(Collision other)
    {
        var position = other.contacts[0].point;
        Collider[] colliders = Physics.OverlapSphere(position, radius, layerMask);
        var effect = Instantiate(explosionEffect, position, Quaternion.identity);
        Destroy(effect, 3);
        foreach (var collider in colliders)
        {
            var hitter = collider.GetComponent<HeadHitter>();
            if (hitter != null)
            {
                hitter.AddHitForce(2000);
            }
            
            var rigBody = collider.GetComponent<Rigidbody>();
            if (rigBody != null)
                rigBody.AddExplosionForce(power, position, radius,3.0f, ForceMode.VelocityChange);
        }
        Destroy(gameObject);
    }
}