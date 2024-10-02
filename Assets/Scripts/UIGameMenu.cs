using Fusion;
using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Shows in-game menu, handles player connecting disconnecting and cursor locking (thus, should be refactored).
/// </summary>
public class UIGameMenu : MonoBehaviour
{
    [Header("Start Game Setup")]
    [SerializeField] private NetworkRunner _runnerPrefab;
    [SerializeField] private int _maxPlayerCount = 4;

    [Header("Debug")]
    [Tooltip("Single-player starts faster")]
    [SerializeField] private bool _forceSinglePlayer;

    [Header("UI Setup")]
    [SerializeField] private CanvasGroup _panelGroup;
    [SerializeField] private TMP_InputField _nicknameText;
    [SerializeField] private TMP_InputField _roomText;
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private GameObject _startGroup;
    [SerializeField] private GameObject _disconnectGroup;

    private NetworkRunner _runnerInstance;
    private static string _shutdownStatus;

    public async void StartGame()
    {
        await Disconnect();
    }

    public async Task Disconnect()
    {
        if (_runnerInstance == null) { return; }

        _statusText.text = "Disconnecting...";
        _panelGroup.interactable = false;

        NetworkEvents events = _runnerInstance.GetComponent<NetworkEvents>();
        events.OnShutdown.RemoveListener(OnShutdown);

        await _runnerInstance.Shutdown();
        _runnerInstance = null;

        // Reset of scene network objects is needed, reload the whole scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnShutdown(NetworkRunner runner, ShutdownReason reason)
    {
        // TODO: Implementation.
    }
}