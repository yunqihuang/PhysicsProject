using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.FinalIK;

public class IKManager : MonoBehaviour
{
    public bool enableLeftArmIK;
    public bool enableRightArmIK;
    public bool enableAimIK;
    public IK leftArmIK;
    public IK rightArmIK;
    public IK aimIK;
    void Start() {
        // Disable all the IK components so they won't update their solvers. Use Disable() instead of enabled = false, the latter does not guarantee solver initiation.


        enableAimIK = enableLeftArmIK = enableLeftArmIK = false;
        rightArmIK.enabled = false;
        leftArmIK.enabled = false;
        aimIK.enabled = false;
    }
    void LateUpdate() {
        // Updating the IK solvers in a specific order.
        
        if (enableAimIK)
        {
            aimIK.GetIKSolver().Update();
        }        
        if (enableLeftArmIK)
        {
            leftArmIK.GetIKSolver().Update();
        }
        if (enableRightArmIK)
        {
            rightArmIK.GetIKSolver().Update();
        }
    }
}
