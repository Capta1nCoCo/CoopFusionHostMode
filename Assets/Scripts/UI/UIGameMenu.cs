using Fusion;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

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

    private void OnEnable()
    {
        SetPlayerNicknameText();
        TryLoadingPreviousShutdownStatus();
    }

    private void Update()
    {
        ProcessPanelVisibilityInput();
        HandlePanelStates();
    }

    public async void StartGame()
    {
        await Disconnect();
        PlayerPrefs.SetString(Constants.PlayerPrefsVars.PLAYER_NAME, _nicknameText.text);
        _runnerInstance = Instantiate(_runnerPrefab);

        AddListnerForShutdowns();
        NetworkSceneInfo sceneInfo = CreateSceneInfoFromActiveScene();
        StartGameArgs startArguments = CreateMatchmakingArguments(sceneInfo);
        ProvideGameModeFeedbackText(startArguments);

        Task<StartGameResult> startTask = _runnerInstance.StartGame(startArguments);
        await startTask;
        ProvideConnectionFeedback(startTask);
    }

    public async void DisconnectClicked()
    {
        await Disconnect();
    }

    public async void Exit()
    {
        await Disconnect();
        Application.Quit();
    }

    public void TogglePanelVisibility()
    {
        if (_panelGroup.gameObject.activeSelf && _runnerInstance == null)
            return;
        _panelGroup.gameObject.SetActive(!_panelGroup.gameObject.activeSelf);
    }

    public async Task Disconnect()
    {
        if (_runnerInstance == null) { return; }
        ProvideDisconnectionFeedback();
        RemoveListenerForShutdowns();

        await _runnerInstance.Shutdown();
        _runnerInstance = null;
        // Reset of scene network objects is needed, reload the whole scene
        ReloadCurrentScene();
    }

    private void SetPlayerNicknameText()
    {
        string nickname = PlayerPrefs.GetString("PlayerName");
        if (string.IsNullOrEmpty(nickname))
        {
            nickname = "Player" + Random.Range(10000, 100000);
        }
        _nicknameText.text = nickname;
    }

    private void TryLoadingPreviousShutdownStatus()
    {
        _statusText.text = _shutdownStatus != null ? _shutdownStatus : string.Empty;
        _shutdownStatus = null;
    }

    private void ProcessPanelVisibilityInput()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePanelVisibility();
        }
    }

    private void HandlePanelStates()
    {
        if (_panelGroup.gameObject.activeSelf)
        {
            _startGroup.SetActive(_runnerInstance == null);
            _disconnectGroup.SetActive(_runnerInstance != null);
            _roomText.interactable = _runnerInstance == null;
            _nicknameText.interactable = _runnerInstance == null;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
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

    private void ProvideGameModeFeedbackText(StartGameArgs startArguments)
    {
        _statusText.text = startArguments.GameMode == GameMode.Single ? "Starting single-player..." : "Connecting...";
    }

    private void ProvideConnectionFeedback(Task<StartGameResult> startTask)
    {
        if (startTask.Result.Ok)
        {
            _statusText.text = "";
            _panelGroup.gameObject.SetActive(false);
        }
        else
        {
            _statusText.text = $"Connection Failed: {startTask.Result.ShutdownReason}";
        }
    }

    private void ProvideDisconnectionFeedback()
    {
        _statusText.text = "Disconnecting...";
        _panelGroup.interactable = false;
    }

    private void RemoveListenerForShutdowns()
    {
        NetworkEvents events = _runnerInstance.GetComponent<NetworkEvents>();
        events.OnShutdown.RemoveListener(OnShutdown);
    }

    private void OnShutdown(NetworkRunner runner, ShutdownReason reason)
    {
        SaveShutdownStatusInfo(reason);
        // Reset of scene network objects is needed, reload the whole scene
        ReloadCurrentScene();
    }

    private static void SaveShutdownStatusInfo(ShutdownReason reason)
    {
        // Save status info into static variable to be used OnEnable after scene load
        _shutdownStatus = $"Shutdown: {reason}";
        Debug.LogWarning(_shutdownStatus);
    }

    private static void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}