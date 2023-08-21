using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using ActiveRagdoll.Gameplay;
using BehaviorDesigner.Runtime;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.PlayerLoop;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace ActiveRagdoll
{
    public class AIController : CharacterController
    {
        
        public int id;
        public Transform target;
        public EnemyManager enemyManager;
       
        public float attackDistance;
        public float stopTracingDistance;
        public float rotationSpeed;
        public bool catchPlayer;
        
        
        private Vector3 _nextPosition;
        private NavMeshAgent _agent;
        
        private float _grabTimer;
        private float _standByTimer;
        private float _checkPropTimer;
        
        private Vector3 _targetPosition;
        private Vector3 _hipPosition;
        private Vector3 _targetDir;
        private Vector3 _moveNextDir;

        private PhysicalBodyController _controller;
        private PhysicalBodyController _playerController;
        private float _attackDistance;

        [SerializeField]
        private Vector3 _edgePosition;
        [SerializeField]
        private bool _findEdge;

        private Vector3 _randomPoint;
        protected override void UpdateInput()
        {
            _targetPosition = target.position;
            _hipPosition = transform.position;
            
            _nextPosition = _agent.nextPosition;
            _moveNextDir = _nextPosition - _hipPosition;
            _moveNextDir.y = 0;
            _targetDir = _targetPosition - _hipPosition;
            _targetDir.y = 0;
            
            _standByTimer += Time.deltaTime;
            if (_moveNextDir.magnitude > stopTracingDistance)
            {
                _agent.Warp(transform.position);
            }
            if (!_agent.isOnNavMesh)
            {
                return;
            }
            
            
            if (_playerController.knockedOut)
            {
                if (id == enemyManager.leader)
                {
                    
                    CheckGrabbed();
                    if (catchPlayer)
                    {
                        if (!_findEdge)
                        {
                            SearchEdge();
                        }
                        else
                        {
                            GotoEdgeAndDrop();
                        }
                    }
                    else
                    {
                        accelerating = false;
                        Chase();
                        Grab();
                    }

                }
                else
                {
                    grabLeft = grabRight = false;
                    catchPlayer = false;
                    _findEdge = false;
                    accelerating = false;
                    StandBy();
                }
            }
            else
            {               

                grabLeft = grabRight = false;
                catchPlayer = false;
                _findEdge = false;
                accelerating = false;
                Chase();
                CheckProps();
                
                var distance = _targetDir.magnitude;
                if (distance < _attackDistance)
                {
                    Attack();
                }
            }
        }
        
        void CheckProps()
        {
            _checkPropTimer += Time.deltaTime;
            if (_checkPropTimer > 5.0f)
            {
                grabLeft = false;
                grabRight = false;

                if (_controller.IsGrabbingProps())
                {
                    _controller.GrabPropType(out var leftPropType, out var rightPropType);
                    if (leftPropType == PropType.Gun || rightPropType == PropType.Gun)
                    {
                        _attackDistance = attackDistance * 2f;
                    }
                    else
                    {
                        _attackDistance = attackDistance;
                    }
                    return;
                }
                var items = Physics.OverlapSphere(transform.position, 6.0f, (1 << LayerMask.NameToLayer("Props")));
                foreach (var item in items)
                {
                    var prop = item.GetComponent<Props>();
                    if (prop != null && prop.owner == null)
                    {
                        StartCoroutine(TryGrab());
                    }
                }
            }

        }
        
        void CheckGrabbed()
        {
            catchPlayer = false;
            if (_controller.GrabbedLeft(out var grabTag))
            {
                if (grabTag == "Player")
                {
                    catchPlayer = true;
                }
                else
                {
                    grabLeft = false;

                }
            }
            if (_controller.GrabbedRight(out grabTag))
            {
                if (grabTag == "Player")
                {
                    catchPlayer = true;
                }
                else
                {
                    grabRight = false;
                }
            }
        }
        
        void StandBy()
        {
            if (_standByTimer > 1.0f)
            {
                var randomPoint =transform.position +  Random.insideUnitSphere * 10;
                NavMesh.SamplePosition (randomPoint, out var navHit, 10.0f, NavMesh.AllAreas);
                _agent.SetDestination(navHit.position);
                _standByTimer = 0;
            }


            if (_moveNextDir.magnitude > 0.1f)
            {
                moving = true;
                moveDirection = _moveNextDir;
                faceDirection = moveDirection;
            }
            else
            {
                moving = false;
                moveDirection = Vector3.zero;
                faceDirection = transform.forward;
            }
 
        }

        void SearchEdge()
        {
            _agent.FindClosestEdge(out var navHit);
            _agent.stoppingDistance = 1f;
            _edgePosition = navHit.position;
            _findEdge = true;
        }
        
        void GotoEdgeAndDrop()
        {
            Debug.DrawLine(transform.position,_edgePosition,Color.yellow, 3.0f);
            var distance = (_edgePosition - transform.position).magnitude;
            _agent.SetDestination(_edgePosition);
            if (_moveNextDir.magnitude > 0.1f)
            {
                moving = true;
                accelerating = true;
                moveDirection = _moveNextDir;
            }
            else
            {
                moving = true;
                accelerating = false;
                moveDirection = Vector3.zero;
            }
            
            movingBack = false;
            if (distance < 2.0f)
            {
                faceDirection = Quaternion.AngleAxis(rotationSpeed*Time.deltaTime, Vector3.up) * faceDirection;
                DropPlayer();
            }
        }
        
        void Chase()
        {
            _agent.SetDestination(_targetPosition + _randomPoint);
            _agent.stoppingDistance = 1.0f;
            // Moving: 
            if (_moveNextDir.magnitude > 0.1f)
            {
                moving = true;
                moveDirection = _moveNextDir;
                faceDirection = moveDirection;
            }  
            else
            {
                moving = false;
                moveDirection = Vector3.zero;
                faceDirection = _targetDir;
            }
            // _agent.isStopped = moveNextDir.magnitude > stopTracingDistance;
            
            movingBack = false;
        }
        
        void Grab()
        {
            _grabTimer += Time.deltaTime;
            if(_grabTimer > 0.5f)
            {            
                grabLeft = true;
                grabRight = true;
                _grabTimer = 0;
            }
        }
        
        void Attack()
        {
            punchTimer += Time.deltaTime;
            if (punchTimer > punchHoldTime)
            {
                punchTimer = 0;
                if (Random.Range(0.0f, 1.0f) > 0.5f)
                {
                    attack = true;
                }
            }
        }

        IEnumerator TryGrab()
        {
            grabLeft = true;
            grabRight = true;
            yield return new WaitForSeconds(2.0f);
            grabLeft = false;
            grabRight = false;
        }

        IEnumerator ChangeRandomPosition()
        {
            while (true)
            {
                _randomPoint = Random.insideUnitSphere * _attackDistance;
                yield return new WaitForSeconds(2.0f);
            }

        }
        void Awake()
        {
            _agent = GetComponentInChildren<NavMeshAgent>();
            _agent.updatePosition = false;
            _agent.updateRotation = false;

            catchPlayer = false;
        }

        void OnDisable()
        {
            _agent.enabled = false;

        }
        
        void OnEnable()
        {
            _agent.enabled = true;
        }

        void Start()
        {
            _agent.enabled = true;
            _playerController = target.GetComponent<PhysicalBodyController>();
            _controller = GetComponent<PhysicalBodyController>();
            _attackDistance = attackDistance;
            StartCoroutine(ChangeRandomPosition());
        }

        void DropPlayer()
        {
            var origin = target.position;
            Debug.DrawLine(origin, origin + Vector3.down * 10.0f);
            if (!Physics.Raycast(origin, Vector3.down, 10.0f, (1 << LayerMask.NameToLayer("Static Scene"))))
            {
                grabLeft = false;
                grabRight = false;
                catchPlayer = false;
                accelerating = false;
            }
        }
        
        
    }
}


