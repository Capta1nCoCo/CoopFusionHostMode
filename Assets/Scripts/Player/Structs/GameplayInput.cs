using UnityEngine;
using Fusion;

/// <summary>
/// Input structure sent over network to the server.
/// </summary>
public struct GameplayInput : INetworkInput
{
    // Note: Encapsulation was ignored to avoid overhead.
    public Vector2 LookRotation;
    public Vector2 MoveDirection;
    public NetworkButtons Buttons;
}