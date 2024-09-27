using UnityEngine;
using Fusion;
using Fusion.Addons.SimpleKCC;

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
}