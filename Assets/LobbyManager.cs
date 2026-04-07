using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject waitingPanel;

    [Header("Lobby Panel UI")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Waiting Panel UI")]
    [SerializeField] private TextMeshProUGUI joinCodeDisplay;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button leaveButton;

    [Header("Settings")]
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private int maxPlayers = 4;

    private ISession currentSession;
    private bool isHost;

    private async void Start()
    {
        hostButton.onClick.AddListener(() => _ = HostGame());
        joinButton.onClick.AddListener(() => _ = JoinGame());
        startButton.onClick.AddListener(StartGame);
        leaveButton.onClick.AddListener(() => _ = LeaveGame());

        startButton.gameObject.SetActive(false);
        waitingPanel.SetActive(false);
        lobbyPanel.SetActive(true);

        await InitializeServices();
    }

    private async Task InitializeServices()
    {
        SetStatus("Connecting...");
        SetButtonsInteractable(false);

        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            SetStatus("Ready — Host or enter a code to join");
            SetButtonsInteractable(true);
        }
        catch (System.Exception e)
        {
            SetStatus($"Connection failed: {e.Message}");
            Debug.LogError(e);
        }
    }

    private async Task HostGame()
    {
        SetStatus("Creating session...");
        SetButtonsInteractable(false);

        try
        {
            var options = new SessionOptions { MaxPlayers = maxPlayers }.WithRelayNetwork();
            currentSession = await MultiplayerService.Instance.CreateSessionAsync(options);

            isHost = true;
            ShowWaitingPanel();
            joinCodeDisplay.text = $"Code: {currentSession.Code}";
            startButton.gameObject.SetActive(true);
            startButton.interactable = false;
            UpdatePlayerCount();

            currentSession.PlayerJoined += _ => UpdatePlayerCount();
            currentSession.PlayerLeaving += _ => UpdatePlayerCount();
        }
        catch (System.Exception e)
        {
            SetStatus($"Error: {e.Message}");
            Debug.LogError(e);
            SetButtonsInteractable(true);
        }
    }

    private async Task JoinGame()
    {
        string code = joinCodeInput.text.Trim().ToUpper();
        if (string.IsNullOrEmpty(code))
        {
            SetStatus("Enter a code first.");
            return;
        }

        SetStatus("Joining...");
        SetButtonsInteractable(false);

        try
        {
            currentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);

            isHost = false;
            ShowWaitingPanel();
            joinCodeDisplay.text = $"Code: {code}";
            startButton.gameObject.SetActive(false);
            UpdatePlayerCount();

            currentSession.PlayerJoined += _ => UpdatePlayerCount();
            currentSession.PlayerLeaving += _ => UpdatePlayerCount();
        }
        catch (System.Exception e)
        {
            SetStatus($"Error: {e.Message}");
            Debug.LogError(e);
            SetButtonsInteractable(true);
        }
    }

    private void StartGame()
    {
        if (!isHost) return;
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    private async Task LeaveGame()
    {
        try
        {
            if (currentSession != null)
            {
                await currentSession.LeaveAsync();
                currentSession = null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error leaving: {e.Message}");
        }

        isHost = false;
        lobbyPanel.SetActive(true);
        waitingPanel.SetActive(false);
        startButton.gameObject.SetActive(false);
        SetButtonsInteractable(true);
        SetStatus("Ready — Host or enter a code to join");
    }

    private void UpdatePlayerCount()
    {
        if (playerCountText == null || currentSession == null) return;
        int count = currentSession.Players.Count;
        playerCountText.text = $"Players: {count} / {currentSession.MaxPlayers}";

        if (isHost && startButton != null)
            startButton.interactable = count >= maxPlayers;
    }

    private void ShowWaitingPanel()
    {
        lobbyPanel.SetActive(false);
        waitingPanel.SetActive(true);
    }

    private void SetButtonsInteractable(bool value)
    {
        hostButton.interactable = value;
        joinButton.interactable = value;
    }

    private void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
    }
}
