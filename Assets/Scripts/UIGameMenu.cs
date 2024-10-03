using Fusion;
using System;
using System.Collections.Generic;
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
    [SerializeField] private string _gameModeIdentifier;
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

        PlayerPrefs.SetString("PlayerName", _nicknameText.text);

        _runnerInstance = Instantiate(_runnerPrefab);

        AddListnerForShutdowns();
        NetworkSceneInfo sceneInfo = CreateSceneInfoFromActiveScene();
        StartGameArgs startArguments = CreateMatchmakingArguments(sceneInfo);
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

    private void AddListnerForShutdowns()
    {
        NetworkEvents events = _runnerInstance.GetComponent<NetworkEvents>();
        events.OnShutdown.AddListener(OnShutdown);
    }

    private static NetworkSceneInfo CreateSceneInfoFromActiveScene()
    {
        NetworkSceneInfo sceneInfo = new NetworkSceneInfo();
        sceneInfo.AddSceneRef(SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex));
        return sceneInfo;
    }

    private StartGameArgs CreateMatchmakingArguments(NetworkSceneInfo sceneInfo)
    {
        StartGameArgs startArguments = new StartGameArgs()
        {
            GameMode = Application.isEditor && _forceSinglePlayer ? GameMode.Single : GameMode.AutoHostOrClient,
            SessionName = _roomText.text,
            PlayerCount = _maxPlayerCount,
            // We need to specify a session property for matchmaking to decide where the player wants to join.
            // So players from other game mods couldn't join. I plan 1v1 PvP Duels mode, so I added it.
            SessionProperties = new Dictionary<string, SessionProperty>() { ["GameMode"] = _gameModeIdentifier },
            Scene = sceneInfo,
        };
        return startArguments;
    }

    private void OnShutdown(NetworkRunner runner, ShutdownReason reason)
    {
        // TODO: Implementation.
    }
}