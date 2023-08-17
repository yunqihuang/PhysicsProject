using System.Collections;
using System.Collections.Generic;
using ActiveRagdoll;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using UnityEngine.AI;

public class ChasePlayer : Action
{
    public NavMeshAgent Agent;
    public AIController AiController;
    
    public Transform target;
    
    private Vector3 _targetPosition;
    private Vector3 _hipPosition;
    private Vector3 _targetDir;
    private Vector3 _moveNextDir;
    private Vector3 _nextPosition;
    
    
    public override TaskStatus OnUpdate()
    {
        _targetPosition = target.position;
        _hipPosition = transform.position;
            
        _nextPosition = Agent.nextPosition;
        _moveNextDir = _nextPosition - _hipPosition;
        _moveNextDir.y = 0;
        _targetDir = _targetPosition - _hipPosition;
        _targetDir.y = 0;
        
        Agent.SetDestination(_targetPosition);
        Agent.stoppingDistance = 1.0f;
        // Moving: 
        if (_moveNextDir.magnitude > 0.1f)
        {
            AiController.moving = true;
            AiController.moveDirection = _moveNextDir;
            AiController.faceDirection = AiController.moveDirection;
            return TaskStatus.Running;
        }
        AiController.moving = false;
        AiController.moveDirection = Vector3.zero;
        AiController.faceDirection = _targetDir;
        return TaskStatus.Success;
    }
}
