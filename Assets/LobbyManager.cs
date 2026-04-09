using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
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
    [SerializeField] private int maxPlayers = 2;
    [SerializeField] private GameObject playerPrefab;

    private ISession currentSession;
    private bool isHost;
    private GameObject canvasRoot;

    private async void Start()
    {
        hostButton.onClick.AddListener(() => _ = HostGame());
        joinButton.onClick.AddListener(() => _ = JoinGame());
        startButton.onClick.AddListener(StartGame);
        leaveButton.onClick.AddListener(() => _ = LeaveGame());

        startButton.gameObject.SetActive(false);
        waitingPanel.SetActive(false);
        lobbyPanel.SetActive(true);

        canvasRoot = lobbyPanel.transform.root.gameObject;

        DontDestroyOnLoad(gameObject);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

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

            // Register message handler — when host sends LoadGame, everyone (including host) loads
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                "LoadGame", OnLoadGameMessage);

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
            // Ensure NetworkManager is clean before joining
            if (NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();

            await System.Threading.Tasks.Task.Delay(500); // wait for shutdown

            currentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);

            // Register message handler — client will load scene when host says so
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                "LoadGame", OnLoadGameMessage);

            // Register spawn handler — host tells client to spawn at a position
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                "SpawnPlayer", OnSpawnPlayerMessage);

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

    private void OnLoadGameMessage(ulong senderId, FastBufferReader reader)
    {
        HideLobbyUI();
        SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);

        if (isHost)
            StartCoroutine(SpawnPlayersAfterLoad());
    }

    private IEnumerator SpawnPlayersAfterLoad()
    {
        // Wait for scene to fully load
        yield return new WaitForSeconds(1.5f);

        GameObject pinkSpawnObj = GameObject.Find("PinkSpawn");
        GameObject redSpawnObj = GameObject.Find("RedSpawn");

        Vector3 pinkBase = pinkSpawnObj != null ? pinkSpawnObj.transform.position : new Vector3(0, 10, 0);
        Vector3 redBase  = redSpawnObj  != null ? redSpawnObj.transform.position  : new Vector3(5, 10, 0);

        Vector3[] spawnPos = new Vector3[]
        {
            SnapToGround(pinkBase),
            SnapToGround(redBase)
        };

        var clients = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
        for (int i = 0; i < clients.Count; i++)
        {
            Vector3 pos = spawnPos[i % spawnPos.Length];
            GameObject player = Instantiate(playerPrefab, pos, Quaternion.identity);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clients[i], true);
        }
    }

    // Raycast down from a spawn marker to place the player exactly on the ground surface
    private Vector3 SnapToGround(Vector3 from)
    {
        // Cast from well above the marker downward
        Vector3 rayOrigin = new Vector3(from.x, from.y + 5f, from.z);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 30f))
        {
            // Offset up by half the capsule height (1 unit) so the player stands on the surface
            return hit.point + Vector3.up * 1.1f;
        }
        // No ground found — use the marker position as-is
        Debug.LogWarning($"SnapToGround: no ground found below {from}, using raw position");
        return from;
    }

    private void OnSpawnPlayerMessage(ulong senderId, FastBufferReader reader)
    {
        // Clients don't need to do anything — NGO handles replication
    }

    private void StartGame()
    {
        if (!isHost) return;

        // Send load message to all clients
        var writer = new FastBufferWriter(0, Allocator.Temp);
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll("LoadGame", writer);
        writer.Dispose();

        // Host loads too
        HideLobbyUI();
        SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        StartCoroutine(SpawnPlayersAfterLoad());
    }

    private void HideLobbyUI()
    {
        if (canvasRoot != null)
            canvasRoot.SetActive(false);
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

        NetworkManager.Singleton?.Shutdown();
        isHost = false;
        if (canvasRoot != null) canvasRoot.SetActive(true);
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
