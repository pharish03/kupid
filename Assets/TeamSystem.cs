using UnityEngine;
using System.Collections.Generic;
public class TeamSystem : MonoBehaviour
{
    public struct Teams
    {
        public List<GameObject> pinkTeam;
        public List<GameObject> redTeam;
        public List<GameObject> deadPinkPlrs;
        public List<GameObject> deadRedPlrs;
    }
   
    [SerializeField] private int maxPlayersOnTeam;
    RoundManager roundManager;



    // for now teams will be random cuz its easier
    public Teams DesignateTeam(GameObject[] players) 
    {
        Teams teams = new Teams();
        teams.pinkTeam = new List<GameObject>();
        teams.redTeam = new List<GameObject>();
        foreach (GameObject p in players)
        {
            float randomInt = Random.Range(0.0f, 1.0f);
            if (randomInt >= 0.5) 
            { 
                if(teams.pinkTeam.Count < maxPlayersOnTeam)
                {
                    teams.pinkTeam.Add(p);
                    print("Added " + p.name + " to pink team.");
                } else
                {
                    print("Pink team full, adding " + p.name + "to red team.");
                    teams.redTeam.Add(p);
                }
              
            }
            else if (randomInt < 0.5 && teams.redTeam.Count < maxPlayersOnTeam)
            {
                if(teams.redTeam.Count < maxPlayersOnTeam)
                {
                    teams.redTeam.Add(p);
                    print("Added " + p.name + " to red team.");
                } else
                {
                    print("Red team full, adding " + p.name + "to pink team.");
                    teams.pinkTeam.Add(p);
                }

                   
            } 
        }
        return teams;
    }

    public void CheckWhosAlive(Teams teams)
    {
        // Runs every frame
        foreach (GameObject p in teams.pinkTeam)
        {
            // if(p.currentHealth <= 0){    Once we get the multiplayer logic implements, this will work (hopefully)
            //     teams.deadPinkPlrs.Add(p);
            //     teams.pinkTeam.Remove(p);
            // }
            if(teams.pinkTeam.Count <= 0){
                print("Red wins round");
                teams.pinkTeam.AddRange(teams.deadPinkPlrs);
                teams.deadPinkPlrs.Clear();
                roundManager.teamRedScore += 1;
                roundManager.inRound = false;
               break; 
            }

            //    if(p.currentHealth <= 0){  
            //      teams.deadRedPlrs.Add(p);
            //      teams.redTeam.Remove(p);
            //  }
            if(teams.redTeam.Count <= 0){
                print("Pink wins round");
                teams.redTeam.AddRange(teams.deadRedPlrs);
                teams.deadRedPlrs.Clear();
                roundManager.teamPinkScore += 1;
                roundManager.inRound = false;
               break; 
            }

        }
    }
}
