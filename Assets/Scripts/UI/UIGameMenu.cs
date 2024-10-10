using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using static Constants.GeneralStrings;

/// <summary>
/// Shows in-game menu, handles player connecting/disconnecting and cursor locking.
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
        HandlePanelGroupStates();
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
        string nickname = PlayerPrefs.GetString(Constants.PlayerPrefsVars.PLAYER_NAME);
        if (string.IsNullOrEmpty(nickname))
        {
            const int MIN_SUFFIX_VALUE = 10000;
            const int MAX_SUFFIX_VALUE = 100000;
            nickname = DEFAULT_PLAYER_NICKNAME + Random.Range(MIN_SUFFIX_VALUE, MAX_SUFFIX_VALUE);
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

    private void HandlePanelGroupStates()
    {
        if (_panelGroup.gameObject.activeSelf)
        {
            ControlPanelGroup();
            ChangeCursorState(CursorLockMode.None, true);
        }
        else
        {
            ChangeCursorState(CursorLockMode.Locked, false);
        }
    }

    private void ControlPanelGroup()
    {
        _startGroup.SetActive(_runnerInstance == null);
        _disconnectGroup.SetActive(_runnerInstance != null);
        _roomText.interactable = _runnerInstance == null;
        _nicknameText.interactable = _runnerInstance == null;
    }

    private static void ChangeCursorState(CursorLockMode lockMode, bool isVisible)
    {
        Cursor.lockState = lockMode;
        Cursor.visible = isVisible;
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
            SessionProperties = new Dictionary<string, SessionProperty>() { [GAME_MODE] = _gameModeIdentifier },
            Scene = sceneInfo,
            SceneManager = _runnerInstance.GetComponent<NetworkSceneManagerDefault>()
        };
        return startArguments;
    }

    private void ProvideGameModeFeedbackText(StartGameArgs startArguments)
    {
        _statusText.text = startArguments.GameMode == GameMode.Single ? SINGLE_PLAYER_TEXT : CONNECTING_TEXT;
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
            _statusText.text = $"{CONNECTION_FAILED_TEXT}: {startTask.Result.ShutdownReason}";
        }
    }

    private void ProvideDisconnectionFeedback()
    {
        _statusText.text = DISCONNECTING_TEXT;
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
        _shutdownStatus = $"{SHUTDOWN_TEXT}: {reason}";
        Debug.LogWarning(_shutdownStatus);
    }

    private void ReloadCurrentScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }
}