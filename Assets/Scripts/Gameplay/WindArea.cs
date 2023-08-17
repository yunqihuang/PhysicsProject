using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class WindArea : MonoBehaviour
{
   public float windForce;
   public Vector3 direction;

   void OnTriggerStay(Collider other)
   {
      var hitObj = other.gameObject;
      if (hitObj != null)
      {
         var rb = hitObj.GetComponent<Rigidbody>();
          rb.AddForce(transform.forward * windForce, ForceMode.Acceleration);
      }
   }
}
