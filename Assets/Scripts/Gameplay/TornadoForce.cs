using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TornadoForce : MonoBehaviour {
    
    public float windPower = 10;
    public float windSpeed = 10;
    public LayerMask layer;
    
    Vector3 heading, direction;
    float distance, remapDistance;


    private void OnTriggerStay(Collider other)
    {
        if ((layer & (1 << other.gameObject.layer)) > 0)
        {
            Pull(other);
        }
    }

    private void Pull(Collider col)
    {
        heading = transform.position - col.transform.position;
        distance = heading.magnitude;
        direction = heading / distance;
        remapDistance = distance.Remap(0, 20, 0, 1);

        var tangent = Vector3.Cross(direction, Vector3.up);

        var rigBody = col.attachedRigidbody;
        var neededAccel = (windSpeed- Vector3.Dot(tangent, rigBody.velocity)) / Time.fixedDeltaTime;
        col.attachedRigidbody.AddForce(rigBody.mass * neededAccel * tangent);
        col.attachedRigidbody.AddForce(direction * windPower * remapDistance);
    }


}

public static class ExtensionMethods
{
    public static float Remap(this float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}