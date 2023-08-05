using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.PlayerLoop;
using UnityEngine.Serialization;

namespace ActiveRagdoll
{
    public class AIController : CharacterController
    {
        
        public Transform target;
        private NavMeshAgent _agent;

        public float holdTime;
        public float attackDistance;
        public float movingDistance;
        public float stopTracingDistance;   
        public float testFacingAngle;
        public float coolDownTime;
        
        private Vector3 _nextPosition;
        private float _readyPunchTimer;
        private Transform _hip;

        private float _leftPunchTimer, _rightPunchCoolDown;
        protected override void UpdateInput()
        {
            var targetPosition = target.position;
            var hipPosition = _hip.position;
            _agent.SetDestination(targetPosition);
            _nextPosition = _agent.nextPosition;

            var moveNextDir = _nextPosition - hipPosition;
            var targetDir = targetPosition - hipPosition;
            moveDirection = new Vector3(moveNextDir.x, 0, moveNextDir.z);
            // Moving: 
            moving = moveDirection.magnitude > movingDistance;
            
            // accelerating = targetDir.magnitude > 20.0f;
           
            _agent.isStopped = moveDirection.magnitude > stopTracingDistance;
            
            movingBack = false;
            faceAngle = testFacingAngle + Random.Range(-40.0f, 40.0f);
            faceAngle *= Mathf.Deg2Rad;
            var distance = targetDir.magnitude;
            if (distance < attackDistance)
            {
                Attack();
            }
        }

        void Attack()
        {
            if (_rightPunchCoolDown > coolDownTime)
            {
                PunchRight();
            }
            else
            {
                _rightPunchCoolDown += Time.deltaTime;
            }
        }

        void PunchRight()
        {
            if (!readyPunchingRight && !punchingRight)
            {
                readyPunchingRight = true;
                _readyPunchTimer = 0;
            }
            if (readyPunchingRight)
            {
                _readyPunchTimer += Time.deltaTime;
                if (_readyPunchTimer > holdTime)
                {
                    readyPunchingRight = false;
                    punchingRight = true;
                    punchingRightTimer = 0;
                }
            }
            if (punchingRight)
            {
                punchingRightTimer += Time.deltaTime;
                if (punchingRightTimer > punchHoldTime)
                {
                    punchingRight = false;
                    _rightPunchCoolDown = 0;
                }
            }
        }
        void PunchLeft()
        {
            if (!readyPunchingLeft && !punchingLeft)
            {
                readyPunchingLeft = true;
                _readyPunchTimer = 0;
            }
            if (readyPunchingLeft)
            {
                _readyPunchTimer += Time.deltaTime;
                if (_readyPunchTimer > holdTime)
                {
                    readyPunchingLeft = false;
                    punchingLeft = true;
                    punchingLeftTimer = 0;
                }
            }
            if (punchingLeft)
            {
                punchingLeftTimer += Time.deltaTime;
                if (punchingLeftTimer > punchHoldTime)
                {
                    punchingLeft = false;
                }
            }
            
        }
        // Start is called before the first frame update
        void Start()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.updatePosition = false;
            _agent.updateRotation = false;
            _hip = transform.Find("Hips");
        }
    }
}


