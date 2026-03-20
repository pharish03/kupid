using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class RoundManager : NetworkBehaviour
{
    public int roundsToWin = 6;

    [SerializeField] private GameObject PinkUI;
    [SerializeField] private GameObject RedUI;

    // NetworkVariables sync automatically to all clients
    public NetworkVariable<int> teamPinkScore = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> teamRedScore = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> inRound = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [Header("Systems")]
    [SerializeField] SpawnSystem spawnSystem;
    [SerializeField] TeamSystem teamSystem;
    [SerializeField] Countdown countdown;

    public TeamSystem.Teams currentTeams;

    [Header("Match Settings")]
    public int requiredPlayers = 4; // Set to 2 for testing locally

    public override void OnNetworkSpawn()
    {
        // Hook up UI updates when scores change
        teamPinkScore.OnValueChanged += (oldVal, newVal) => UpdateScoreUI();
        teamRedScore.OnValueChanged += (oldVal, newVal) => UpdateScoreUI();

        // Only the server runs the round loop
        if (IsServer)
        {
            StartCoroutine(WaitForPlayersAndStart());
        }
    }

    // Wait until all 4 players are connected before starting
    private IEnumerator WaitForPlayersAndStart()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton.ConnectedClientsList.Count >= requiredPlayers);
        yield return new WaitForSeconds(1f); // small buffer

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        StartCoroutine(StartRoundLoop(players));
    }

    private IEnumerator StartRoundLoop(GameObject[] plrs)
    {
        currentTeams = teamSystem.DesignateTeam(plrs);

        while (teamRedScore.Value < roundsToWin && teamPinkScore.Value < roundsToWin)
        {
            // Respawn and reset all players
            spawnSystem.SpawnTeams(currentTeams.pinkTeam, currentTeams.redTeam);

            // Reset health on all players
            foreach (var p in currentTeams.pinkTeam)
            {
                p.GetComponent<PlayerMovement>()?.ResetPlayer(spawnSystem.pinkSpawn.position);
            }
            foreach (var p in currentTeams.redTeam)
            {
                p.GetComponent<PlayerMovement>()?.ResetPlayer(spawnSystem.redSpawn.position);
            }

            countdown.remainingTime = 120;
            countdown.isTimerOn = true;
            inRound.Value = true;

            // Wait until timer ends or a team is wiped
            yield return new WaitUntil(() => !countdown.isTimerOn || CheckRoundOver());

            inRound.Value = false;

            // Award point
            AwardRoundPoint();

            yield return new WaitForSeconds(3f); // pause between rounds
        }

        AnnounceWinnerClientRpc(teamPinkScore.Value >= roundsToWin ? "Pink" : "Red");
    }

    void Update()
    {
        // Only server checks alive status each frame
        if (!IsServer || !inRound.Value) return;
        teamSystem.CheckWhosAlive(currentTeams, this);
    }

    private bool CheckRoundOver()
    {
        return currentTeams.pinkTeam.TrueForAll(p => p.GetComponent<PlayerMovement>()?.IsDead ?? true)
            || currentTeams.redTeam.TrueForAll(p => p.GetComponent<PlayerMovement>()?.IsDead ?? true);
    }

    private void AwardRoundPoint()
    {
        bool pinkAllDead = currentTeams.pinkTeam.TrueForAll(p => p.GetComponent<PlayerMovement>()?.IsDead ?? true);
        bool redAllDead = currentTeams.redTeam.TrueForAll(p => p.GetComponent<PlayerMovement>()?.IsDead ?? true);

        if (redAllDead && !pinkAllDead)
            teamPinkScore.Value++;
        else if (pinkAllDead && !redAllDead)
            teamRedScore.Value++;
        // Both dead = draw, no point awarded
    }

    private void UpdateScoreUI()
    {
        // Hook up your UI text components here
        Debug.Log($"Pink: {teamPinkScore.Value} | Red: {teamRedScore.Value}");
    }

    [ClientRpc]
    private void AnnounceWinnerClientRpc(string winningTeam)
    {
        Debug.Log($"{winningTeam} team wins the match!");
        // Show your win screen here
    }
}