using UnityEngine;
using System.Collections.Generic;
public class TeamSystem : MonoBehaviour
{
    public struct Teams
    {
        public List<GameObject> pinkTeam;
        public List<GameObject> redTeam;
    }
   
    [SerializeField] private int maxPlayersOnTeam;



    // for now teams will be random cuz its easier
    public Teams DesignateTeam(GameObject[] players) 
    {
        Teams teams = new Teams();
        teams.pinkTeam = new List<GameObject>();
        teams.redTeam = new List<GameObject>();
        foreach (GameObject p in players)
        {
            float randomInt = Random.Range(0.0f, 1.0f);
            if (randomInt >= 0.5 && teams.pinkTeam.Count < maxPlayersOnTeam) 
            { 
                teams.pinkTeam.Add(p);
                print("Added " + p.name + " to pink team.");
            }
            else if (randomInt < 0.5 && teams.redTeam.Count < maxPlayersOnTeam)
            {
                teams.redTeam.Add(p);
                print("Added " + p.name + " to red team.");
            } else
            {
                print("Both teams full");
            }
        }
        return teams;
    }
}
