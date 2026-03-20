using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class TeamSystem : NetworkBehaviour
{
    public struct Teams
    {
        public List<GameObject> pinkTeam;
        public List<GameObject> redTeam;
    }

    [SerializeField] private int maxPlayersOnTeam = 2;

    public Teams DesignateTeam(GameObject[] players)
    {
        Teams teams = new Teams();
        teams.pinkTeam = new List<GameObject>();
        teams.redTeam = new List<GameObject>();

        // Shuffle players first for fair random assignment
        List<GameObject> shuffled = new List<GameObject>(players);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        // Assign evenly: first half pink, second half red
        for (int i = 0; i < shuffled.Count; i++)
        {
            if (teams.pinkTeam.Count < maxPlayersOnTeam)
                teams.pinkTeam.Add(shuffled[i]);
            else
                teams.redTeam.Add(shuffled[i]);
        }

        Debug.Log($"Pink team: {teams.pinkTeam.Count} | Red team: {teams.redTeam.Count}");

        // Tag each player so arrows and other systems know their team
        foreach (GameObject p in teams.pinkTeam)
            SetTeamTagClientRpc(new NetworkObjectReference(p.GetComponent<NetworkObject>()), "PinkTeam");
        foreach (GameObject p in teams.redTeam)
            SetTeamTagClientRpc(new NetworkObjectReference(p.GetComponent<NetworkObject>()), "RedTeam");

        return teams;
    }

    // Runs on all clients so every machine knows each player's team tag
    [ClientRpc]
    private void SetTeamTagClientRpc(NetworkObjectReference playerRef, string tag)
    {
        if (playerRef.TryGet(out NetworkObject netObj))
            netObj.gameObject.tag = tag;
    }

    // Fixed: no longer iterates pinkTeam to check if pinkTeam is empty
    public void CheckWhosAlive(Teams teams, RoundManager roundManager)
    {
        if (!roundManager.inRound.Value) return;

        bool pinkAllDead = teams.pinkTeam.TrueForAll(p => p.GetComponent<PlayerMovement>()?.IsDead ?? true);
        bool redAllDead = teams.redTeam.TrueForAll(p => p.GetComponent<PlayerMovement>()?.IsDead ?? true);

        if (pinkAllDead || redAllDead)
        {
            roundManager.inRound.Value = false;
        }
    }
}