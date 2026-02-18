
using UnityEngine;

public class SpawnSystem : MonoBehaviour
{

    public Transform pinkSpawn;
    public Transform redSpawn;
    

    // You're supposed to spawn objects a diff way with multiplayer netcode, but the dependency won't work for me right now
    public void spawnTeams(GameObject[] pinkTeam, GameObject[] redTeam)
    {
        for(int i = 0; i < pinkTeam.Length; i++)
        {
          Instantiate(pinkTeam[i], pinkSpawn.position, pinkSpawn.rotation);
        }
        print("Spawned pink team");
        for (int i = 0; i < redTeam.Length; i++)
        {
            Instantiate(redTeam[i], redSpawn.position, redSpawn.rotation);
        }
        print("Spawned red team");
    }

    public void despawnTeams(GameObject[] pinkTeam, GameObject[] redTeam)
    {
        for (int i = 0; i < pinkTeam.Length; i++)
        {
            Destroy(pinkTeam[i]);
        }
        print("Pink team despawned");
        for (int i = 0; i < redTeam.Length; i++)
        {
            Destroy(redTeam[i]);
        }
        print("Red team despawned");
    }
}
