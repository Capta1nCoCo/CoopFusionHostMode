using UnityEngine;
using Fusion;
using Fusion.Addons.SimpleKCC;
using System;

/// <summary>
/// Main player script - controls movement and animations.
/// </summary>
public class Player : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private SimpleKCC _kcc;
    [SerializeField] private PlayerInput _input;
    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _cameraPivot;
    [SerializeField] private Transform _cameraHandle;

    [Header("Movement Setup")]
    [SerializeField] private float _walkSpeed = 2f;
    [SerializeField] private float _sprintSpeed = 5f;
    [SerializeField] private float _jumpImpulse = 10f;
    [SerializeField] private float _upGravity = 25f;
    [SerializeField] private float _downGravity = 40f;
    [SerializeField] private float _rotationSpeed = 8f;

    [Header("Movement Accelerations")]
    [SerializeField] private float _groundAcceleration = 55f;
    [SerializeField] private float _groundDeaceleration = 25f;
    [SerializeField] private float _airAcceleration = 25f;
    [SerializeField] private float _airDeceleration = 1.3f;

    [Header("Sounds")]
    [SerializeField] private AudioClip[] _footstepAudioClips;
    [SerializeField] private AudioClip _landingAudioClip;
    [Range(0f, 1f)]
    [SerializeField] private float _footstepAudioVolume = 0.5f;

    [Networked] private Vector3 _moveVelocity { get; set; }
    [Networked] private NetworkBool _isJumping { get; set; }
    [Networked] private NetworkButtons _previousButtons { get; set; }

    // TODO: Encapsulate animation logic to a separate class.
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;

    public override void FixedUpdateNetwork()
    {
        GameplayInput input = GetInput<GameplayInput>().GetValueOrDefault();
        ProcessInput(input, _previousButtons);

        if (_kcc.IsGrounded)
        {
            if (_isJumping)
            {
                _isJumping = false;
            }
        }

        // Save current button input as previous.
        // Previous buttons need to be networked to detect correctly pressed/released events.
        _previousButtons = input.Buttons;
    }

    private void ProcessInput(GameplayInput input, NetworkButtons previousButtons)
    {
        // TODO: Implementation.
    }
}