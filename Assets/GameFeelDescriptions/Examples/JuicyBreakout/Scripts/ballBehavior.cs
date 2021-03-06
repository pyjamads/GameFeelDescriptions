﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameFeelDescriptions.Examples
{

    [RequireComponent(typeof(Rigidbody2D))]
    public class ballBehavior : MonoBehaviour
    {
        public float acceleration = 1f;
        public Vector2 velocity;
        public float maxSpeed;
        
        private Rigidbody2D body;

        public bool ballReady = true;

        //NOTE: the "tests" were used to check that OnStateChangedTrigger could handle tracking various FSM types.
        public enum TestStates
        {
            Bouncing,
            Flying,
            Destroying,
        }

        private bool testBool;
        private TestStates testEnum;
        private Action testDelegate = () => { /* DO NOTHING!!!*/ };

        // [SerializeReference]
        // public List<GameFeelEffect> activatedEffects = new List<GameFeelEffect>(); 

        // Start is called before the first frame update
        void Start()
        {
            body = GetComponent<Rigidbody2D>();
            velocity = Vector2.up * 5 + Random.onUnitSphere.AsVector2();
        }


        public void ResetBall()
        {
            ballReady = true;
            transform.position = new Vector3(0, -3, 0);
            velocity = Vector2.up * 5 + Random.insideUnitCircle.normalized;
            body.velocity = Vector2.zero;
        }


        // Update is called once per frame
        void Update()
        {
            if (ballReady) return;

            //FAKE FSM CALL
            testDelegate();

            //Standard movement update
//        velocity += acceleration * Time.deltaTime;
//        acceleration = Vector2.zero;
            body.velocity = velocity;

            if (velocity.magnitude > maxSpeed)
            {
                velocity = velocity.normalized * maxSpeed;
            }

            //TODO: fix wall clipping bug, just make it math based really...
            if (transform.position.y < -5 || 5 < transform.position.y ||
                transform.position.x < -10 || 10 < transform.position.x) ResetBall();
        }

        
        void FSMBounce()
        {
            /*DO NOTHING!!*/
        }

        void FSMFly()
        {
            /*ALSO DO NOTHING!!*/
        }
        
        private void OnCollisionEnter2D(Collision2D other)
        {
            //FSM State change tests
            testEnum = EnumExtensions.GetRandomValue<TestStates>();
            //Only true when testEnum is set to bouncing.
            testBool = testEnum == TestStates.Bouncing;

            if (testEnum == TestStates.Flying)
            {
                testDelegate = FSMFly;
            } 
            else if (testEnum == TestStates.Bouncing)
            {
                testDelegate = FSMBounce;
            }
            
            if (other.gameObject.CompareTag("paddle"))
            {
                var dir = transform.position - other.gameObject.transform.position;
                var mag = velocity.magnitude;
                velocity = dir.normalized * mag * acceleration;
            }
            else
            {
//            if (other.contactCount > 1)
//            {
//                var maxDot = float.NegativeInfinity;
//                var normal = Vector2.one;
//
//                for (int i = 0; i < other.contactCount; i++)
//                {
//                    var contact = other.GetContact(0);
//
//                    var directionToZero = Vector2.zero - contact.point;
//
//                    var dot = Vector2.Dot(contact.normal, directionToZero);
//                    if (dot >= maxDot)
//                    {
//                        normal = contact.normal;
//                    }
//                }
//                
//                velocity = Vector2.Reflect(-velocity, normal);
//                
//                acceleration = velocity.normalized * velocity.magnitude;
//            }
//            else
//            {
                var contact = other.GetContact(0);
                velocity = Vector2.Reflect(velocity, contact.normal) * acceleration;
//            }

                if (other.gameObject.CompareTag("block") || other.gameObject.CompareTag("Enemy"))
                {
                    //Direct activation test...
                    // var effect =Squeezer.Trigger(other.gameObject, StepThroughModeWindow.EffectGeneratorCategories.EXPLODE, 5, new CollisionData {Collision2D = other, ActivationType = OnCollisionTrigger.CollisionActivationType.OnCollisionEnter2D});
                    // activatedEffects.Add(effect);
                    
                    Destroy(other.gameObject);
                }
            }

        }
    }
}