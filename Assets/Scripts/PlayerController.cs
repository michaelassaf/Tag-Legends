﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Globalization;
using System.Numerics;
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;
using System.Reflection.Emit;

public class PlayerController : MonoBehaviourPunCallbacks
{
    [Header("Components")] public Rigidbody rig;
    public Animator animator;

    [SerializeField] private bool grounded;
    [SerializeField] public LayerMask groundLayer;

    public float speed = 10f;
    public Vector3 inputVector;
    public Camera cam;
    public AudioSource SoundFootSteps;

    public float horizontal;
    public float vertical;

    private PlayerManager playerManager;

    public Joystick joystick;
    private static readonly int Jump = Animator.StringToHash("Jump");
    private static readonly int Sprint = Animator.StringToHash("Sprint");

    private void Awake()
    {
        playerManager = gameObject.GetComponent<PlayerManager>();
    }

    private void Start()
    {
        if (photonView.IsMine)
        {
            SoundFootSteps.enabled = true;
            SoundFootSteps.Stop();
        }
    }

    // update is called once per frame
    private void Update()
    {
        // check if the game is started
        if (PlayerManager.Instance.startGame && !GameManager.Instance.gameEnded)
        {
            // only move my player
            if (photonView.IsMine)
            {
                // Get movement vertices
                horizontal = Input.GetAxisRaw("Horizontal") + joystick.Horizontal;
                vertical = Input.GetAxisRaw("Vertical") + joystick.Vertical;

                Vector3 joystickDirection = cam.transform.rotation * new Vector3(horizontal, 0, vertical);

                horizontal = joystickDirection.x;
                vertical = joystickDirection.z;

                Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
                float targetAngle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
                inputVector = direction * (speed * Time.deltaTime);

                // if shout is active - set fear animation and set kinematic to true
                if (playerManager.isFearedActive)
                {
                    animator.SetBool(BerserkerAbilities.ShoutActiveAnimatorFloatVar, true);
                    rig.isKinematic = true;
                }
                // if i'm a berserker and im shouting - then don't move until animation is complete
                else if (playerManager.isShoutAnimationActive)
                {
                    rig.isKinematic = true;
                }
                // am I stunned by an axe?
                else if (playerManager.isAxeStunned)
                {
                    animator.SetBool(BerserkerAbilities.AxeStunnedAnimatorFloatVar, true);
                    rig.isKinematic = true;
                }
                else if (playerManager.isIceBoltFreeze)
                {
                    rig.isKinematic = true;
                }
                else if (playerManager.isIceBlock)
                {
                    rig.isKinematic = true;
                }
                else if (playerManager.isFreezingWindsActive)
                {
                    rig.isKinematic = true;
                }
                else
                {
                    // reset to normal state player behaviour - no ability effects
                    animator.SetBool(BerserkerAbilities.ShoutActiveAnimatorFloatVar, false);
                    animator.SetBool(BerserkerAbilities.AxeStunnedAnimatorFloatVar, false);
                    rig.isKinematic = false;

                    // check if my player is grounded
                    grounded = Physics.Raycast(transform.position + Vector3.up,
                        transform.TransformDirection(Vector3.down), 1.2f, groundLayer);

                    // can only move while grounded

                    animator.SetBool(Jump, false);

                    // only move if input was calculated
                    if (inputVector.x < 0 || inputVector.z < 0 || inputVector.x > 0 || inputVector.z > 0)
                    {
                        transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
                        transform.forward = inputVector;
                        rig.velocity = new Vector3(transform.forward.x * speed, rig.velocity.y,
                            transform.forward.z * speed);
                        
                        // play sound footsteps
                        if(!SoundFootSteps.isPlaying)
                            SoundFootSteps.Play();
                    }

                    // Completely stop moving (velocity-wise) if no input was found
                    if (horizontal == 0f && vertical == 0f)
                    {
                        rig.velocity = new Vector3(0f, rig.velocity.y, 0f);
                        
                        // stop playing sound footsteps
                        if(SoundFootSteps.isPlaying)
                            SoundFootSteps.Stop();
                    }

                    // Animatioms
                    if (Mathf.Abs(Input.GetAxisRaw("Vertical")) > .1 ||
                        Mathf.Abs(Input.GetAxisRaw("Horizontal")) > .1 || Mathf.Abs(joystick.Vertical) > .1 ||
                        Mathf.Abs(joystick.Horizontal) > .1
                    ) //(rig.velocity.x > 0 || rig.velocity.z > 0 || rig.velocity.x < 0 || rig.velocity.z < 0)
                    {
                        animator.SetBool(Sprint, true);
                    }
                    else
                    {
                        animator.SetBool(Sprint, false);
                    }

                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                GameUI.instance.EscapeMenu();
            }
        }
    }
}