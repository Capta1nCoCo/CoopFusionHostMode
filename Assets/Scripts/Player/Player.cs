using UnityEngine;
using Fusion;
using Fusion.Addons.SimpleKCC;
using System;

/// <summary>
/// Main player script - controls movement and updates animations.
/// </summary>
public class Player : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private SimpleKCC _kcc;
    [SerializeField] private PlayerInput _input;
    [SerializeField] private PlayerAnimator _playerAnimator;
    [SerializeField] private PlayerAudio _playerAudio;
    [SerializeField] private Transform _cameraPivot;
    [SerializeField] private Transform _cameraHandle;
    [SerializeField] private UINameplate _nameplate;

    [Header("Movement Setup")]
    [SerializeField] private float _walkSpeed = 2f;
    [SerializeField] private float _sprintSpeed = 5f;
    [SerializeField] private float _jumpImpulse = 10f;
    [SerializeField] private float _upGravity = -25f;
    [SerializeField] private float _downGravity = -40f;
    [SerializeField] private float _rotationSpeed = 8f;

    [Header("Movement Accelerations")]
    [SerializeField] private float _groundAcceleration = 55f;
    [SerializeField] private float _groundDeceleration = 25f;
    [SerializeField] private float _airAcceleration = 25f;
    [SerializeField] private float _airDeceleration = 1.3f;

    [Networked] private Vector3 _moveVelocity { get; set; }
    [Networked] private NetworkBool _isJumping { get; set; }
    [Networked] private NetworkButtons _previousButtons { get; set; }

    [Networked, HideInInspector, Capacity(24), OnChangedRender(nameof(OnNicknameChanged))]
    public string Nickname { get; private set; }

    private void Awake()
    {
        _playerAnimator.Init();
        _playerAudio.Init(_kcc);
    }

    private void LateUpdate()
    {
        if (!HasInputAuthority) { return; }
        MoveCamera();
    }

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            RPC_SetNickname(PlayerPrefs.GetString(Constants.PlayerPrefsVars.PLAYER_NAME));
        }
        OnNicknameChanged();
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
        _playerAnimator.UpdateMovementAnimationVars(_kcc.RealSpeed, _isJumping, _kcc.IsGrounded, _kcc.RealVelocity);
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
            acceleration = GetDeceleration();
        }
        else
        {
            RotateCharacterTowardsMoveDirectionOverTime(moveDirection);
            acceleration = GetAcceleration();
        }

        return acceleration;
    }

    private float GetDeceleration()
    {
        return _kcc.IsGrounded ? _groundDeceleration : _airDeceleration;
    }

    private float GetAcceleration()
    {
        return _kcc.IsGrounded ? _groundAcceleration : _airAcceleration;
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

    private void OnNicknameChanged()
    {
        if (HasInputAuthority) { return; }
        _nameplate.SetNickname(Nickname);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetNickname(string nickname)
    {
        Nickname = nickname;
    }
}