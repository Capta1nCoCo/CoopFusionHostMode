using UnityEngine;
using Fusion;

public class PlayerInput : NetworkBehaviour
{
    private NetworkEvents _networkEvents;
    private GameplayInput _input;

    public Vector2 LookRotation { get => _input.LookRotation; }

    private void Update()
    {
        // Input accumulation is mandatory (at least for look rotation) as Update can be
        // called multiple times before next OnInput is called - common if rendering speed is faster than Fusion simulation.
        if (!HasInputAuthority) { return; }
        AccumulateInputFromMouseAndKeyboard();
    }

    public override void Spawned()
    {
        if (!HasInputAuthority) { return; }
        RegisterToFusionInputPollCallback();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (runner == null) { return; }
        UnregisterFromFusionInputPollCallbacks();
    }

    private void AccumulateInputFromMouseAndKeyboard()
    {
        CheckInputAccumulationRequirenments();
        UpdateLookRotation();
        UpdateMoveDirection();
        UpdateButtons();
    }

    private void CheckInputAccumulationRequirenments()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            _input.MoveDirection = default;
            return;
        }
    }

    private void UpdateLookRotation()
    {
        Vector2 lookRotationDelta = new Vector2(-Input.GetAxisRaw("Mouse Y"), Input.GetAxisRaw("Mouse X"));
        _input.LookRotation = ClampLookRotation(_input.LookRotation + lookRotationDelta);
    }

    private void UpdateMoveDirection()
    {
        Vector2 moveDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        _input.MoveDirection = moveDirection.normalized;
    }

    private void UpdateButtons()
    {
        _input.Buttons.Set(EInputButton.Jump, Input.GetButton("Jump"));
        _input.Buttons.Set(EInputButton.Sprint, Input.GetButton("Sprint"));
    }

    private void RegisterToFusionInputPollCallback()
    {
        _networkEvents = Runner.GetComponent<NetworkEvents>();
        _networkEvents.OnInput.AddListener(OnInput);
    }

    private void UnregisterFromFusionInputPollCallbacks()
    {
        if (_networkEvents != null)
        {
            _networkEvents.OnInput.RemoveListener(OnInput);
        }
    }

    // Fusion polls accumulated input. This callback can be executed multiple times in a raw if there is a perfomance spike.
    private void OnInput(NetworkRunner runner, NetworkInput networkInput)
    {
        networkInput.Set(_input);
    }

    private Vector2 ClampLookRotation(Vector2 lookRotation)
    {
        lookRotation.x = Mathf.Clamp(lookRotation.x, -30f, 70f);
        return lookRotation;
    }
}
