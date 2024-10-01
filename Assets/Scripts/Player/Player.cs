using UnityEngine;
using Fusion;
using Fusion.Addons.SimpleKCC;
using System;
using static Constants;

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
    [SerializeField] private float _groundDeceleration = 25f;
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

    private void Awake()
    {
        AssignAnimationIDs();
    }

    private void LateUpdate()
    {
        if (!HasInputAuthority) { return; }
        MoveCamera();
    }

    public override void FixedUpdateNetwork()
    {
        GameplayInput input = GetInput<GameplayInput>().GetValueOrDefault();
        ProcessInput(input, _previousButtons);
        StopJumping();

        // Save current button input as previous.
        // Previous buttons need to be networked to
        // detect correctly pressed/released events.
        _previousButtons = input.Buttons;
    }

    public override void Render()
    {
        _animator.SetFloat(_animIDSpeed, _kcc.RealSpeed, Animations.DAMP_TIME, Time.deltaTime);
        _animator.SetFloat(_animIDMotionSpeed, Animations.NORMAL_SPEED);
        _animator.SetBool(_animIDJump, _isJumping);
        _animator.SetBool(_animIDGrounded, _kcc.IsGrounded);
        _animator.SetBool(_animIDFreeFall, _kcc.RealVelocity.y < Animations.GRAVITY_THRESHOLD);
    }

    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash(Animations.SPEED);
        _animIDGrounded = Animator.StringToHash(Animations.GROUNDED);
        _animIDJump = Animator.StringToHash(Animations.JUMP);
        _animIDFreeFall = Animator.StringToHash(Animations.FREE_FALL);
        _animIDMotionSpeed = Animator.StringToHash(Animations.MOTION_SPEED);
    }

    private void MoveCamera()
    {
        _cameraPivot.rotation = Quaternion.Euler(_input.LookRotation);
        Camera.main.transform.SetPositionAndRotation(_cameraHandle.position, _cameraHandle.rotation);
    }

    private void ProcessInput(GameplayInput input, NetworkButtons previousButtons)
    {
        float jumpImpulse = Jump(input, previousButtons);
        float speed = Sprint(input);
        Quaternion lookRotation = Quaternion.Euler(0f, input.LookRotation.y, 0f);
        // Calculate correct move direction from input (rotated based on camera look)
        Vector3 moveDirection = lookRotation * new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);
        Vector3 desiredMoveVelocity = moveDirection * speed;
        float acceleration = CalculateAcceleration(moveDirection, desiredMoveVelocity);
        CalculateMoveVelocity(desiredMoveVelocity, acceleration);

        _kcc.Move(_moveVelocity, jumpImpulse);
    }

    private float Jump(GameplayInput input, NetworkButtons previousButtons)
    {
        float jumpImpulse = 0f;
        // Comparing current input buttons to previous input buttonsthis prevents glitches when input is lost.
        if (_kcc.IsGrounded && input.Buttons.WasPressed(previousButtons, EInputButton.Jump))
        {
            // Set world space jump vector
            jumpImpulse = _jumpImpulse;
            _isJumping = true;
        }
        ApplyFallAcceleration();
        return jumpImpulse;
    }

    private void ApplyFallAcceleration()
    {
        _kcc.SetGravity(_kcc.RealVelocity.y >= 0f ? _upGravity : _downGravity);
    }

    private float Sprint(GameplayInput input)
    {
        float speed = input.Buttons.IsSet(EInputButton.Sprint) ? _sprintSpeed : _walkSpeed;
        return speed;
    }

    private float CalculateAcceleration(Vector3 moveDirection, Vector3 desiredMoveVelocity)
    {
        float acceleration;
        if (desiredMoveVelocity == Vector3.zero)
        {
            acceleration = ProcessStopping();
        }
        else
        {
            RotateCharacterTowardsMoveDirectionOverTime(moveDirection);
            acceleration = ProcessStopping();
        }

        return acceleration;
    }

    private float ProcessStopping()
    {
        return _kcc.IsGrounded ? _groundDeceleration : _airDeceleration;
    }

    private void RotateCharacterTowardsMoveDirectionOverTime(Vector3 moveDirection)
    {
        Quaternion currentRotation = _kcc.TransformRotation;
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        Quaternion nextRotation = Quaternion.Lerp(currentRotation, targetRotation, _rotationSpeed * Runner.DeltaTime);

        _kcc.SetLookRotation(nextRotation.eulerAngles);
    }

    private void CalculateMoveVelocity(Vector3 desiredMoveVelocity, float acceleration)
    {
        _moveVelocity = Vector3.Lerp(_moveVelocity, desiredMoveVelocity, acceleration * Runner.DeltaTime);
        // Ensure consistent movement speed even on steep slope
        if (_kcc.ProjectOnGround(_moveVelocity, out Vector3 projectedVector))
        {
            _moveVelocity = projectedVector;
        }
    }

    private void StopJumping()
    {
        if (_kcc.IsGrounded)
        {
            if (_isJumping)
            {
                _isJumping = false;
            }
        }
    }
}