using System.Collections;
using System.Collections.Generic;
using ActiveRagdoll;
using UnityEngine;

public class AnimationEventManager : MonoBehaviour
{
    public PhysicalBodyController bodyController;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    
    public void PunchStartEvent(int whichHand)
    {
        bodyController.PunchStart(whichHand);
    }
    public void PunchEndEvent(int whichHand)
    {
        bodyController.PunchEnd(whichHand);
    }

    public void SwingUpEvent(int whichHand)
    {
        bodyController.SwingUp(whichHand);
    }
    public void SwingStartEvent(int whichHand)
    {
        bodyController.SwingStart(whichHand);
    }
    public void SwingEndEvent(int whichHand)
    {
        bodyController.SwingEnd(whichHand);
    }

    public void Shoot()
    {
        bodyController.Shoot();
    }
}
