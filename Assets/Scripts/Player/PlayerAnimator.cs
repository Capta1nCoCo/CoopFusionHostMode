using Fusion;
using UnityEngine;
using static Constants.Animations;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Animator _animator;

    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;

    private void Awake()
    {
        AssignAnimationIDs();
    }

    public void UpdateMovementAnimationVars(float realSpeed, NetworkBool isJumping, bool isGrounded, Vector3 realVelocity)
    {
        _animator.SetFloat(_animIDSpeed, realSpeed, DAMP_TIME, Time.deltaTime);
        _animator.SetFloat(_animIDMotionSpeed, NORMAL_SPEED);
        _animator.SetBool(_animIDJump, isJumping);
        _animator.SetBool(_animIDGrounded, isGrounded);
        _animator.SetBool(_animIDFreeFall, realVelocity.y < GRAVITY_THRESHOLD);
    }

    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash(SPEED);
        _animIDGrounded = Animator.StringToHash(GROUNDED);
        _animIDJump = Animator.StringToHash(JUMP);
        _animIDFreeFall = Animator.StringToHash(FREE_FALL);
        _animIDMotionSpeed = Animator.StringToHash(MOTION_SPEED);
    }
}