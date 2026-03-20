using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SpawnSystem : NetworkBehaviour
{
    public Transform pinkSpawn;
    public Transform redSpawn;

    // Server calls this to teleport players to spawn points
    public void SpawnTeams(List<GameObject> pinkTeam, List<GameObject> redTeam)
    {
        for (int i = 0; i < pinkTeam.Count; i++)
        {
            PlayerMovement pm = pinkTeam[i].GetComponent<PlayerMovement>();
            if (pm != null)
                pm.ResetPlayer(pinkSpawn.position);
        }

        for (int i = 0; i < redTeam.Count; i++)
        {
            PlayerMovement pm = redTeam[i].GetComponent<PlayerMovement>();
            if (pm != null)
                pm.ResetPlayer(redSpawn.position);
        }

        Debug.Log("Teams spawned.");
    }
}