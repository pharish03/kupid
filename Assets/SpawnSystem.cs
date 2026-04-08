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
        // Randomly offset spawn so both players don't stack on the exact same spot
        for (int i = 0; i < pinkTeam.Count; i++)
        {
            PlayerMovement pm = pinkTeam[i].GetComponent<PlayerMovement>();
            if (pm != null)
            {
                Vector3 offset = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
                pm.ResetPlayer(pinkSpawn.position + offset);
            }
        }

        for (int i = 0; i < redTeam.Count; i++)
        {
            PlayerMovement pm = redTeam[i].GetComponent<PlayerMovement>();
            if (pm != null)
            {
                Vector3 offset = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
                pm.ResetPlayer(redSpawn.position + offset);
            }
        }

        Debug.Log("Teams spawned.");
    }
}