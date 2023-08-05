using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.FinalIK;

public class IKManager : MonoBehaviour
{
    public IK[] components;
    void Start() {
        // Disable all the IK components so they won't update their solvers. Use Disable() instead of enabled = false, the latter does not guarantee solver initiation.
        foreach (IK component in components)
        {
            component.enabled = false;
        }
    }
    void LateUpdate() {
        // Updating the IK solvers in a specific order. 
        foreach (IK component in components) component.GetIKSolver().Update();
    }
}
