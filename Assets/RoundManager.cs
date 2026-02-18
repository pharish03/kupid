using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    [SerializeField]private int roundsToWin;
    private int currentRound;
    public int teamPinkScore;
    public int teamRedScore;
    // I think we have to tag the object with a player tag?? So the game knows whats a player and whats not
    GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

    // Systems:
    SpawnSystem spawnSystem;
    TeamSystem teamSystem;
    Countdown countdown;

    // The main manager for the round system
    void Start()
    {

        // TODO: Figure out a way to ensure each player is assigned to a game object
        StartRoundLoop();
        // TODO: Start a 3 second countdown before each round.
      

        

    }

   private IEnumerator StartRoundLoop()
    {
        TeamSystem.Teams newTeams = teamSystem.DesignateTeam(players);
        while (teamRedScore < roundsToWin || teamPinkScore < roundsToWin)
        {
            
            spawnSystem.spawnTeams(newTeams.pinkTeam, newTeams.redTeam);
            countdown.isTimerOn = true;
            // Round Started
            yield return new WaitUntil(() => !countdown.isTimerOn); // Wait until timer ends or all players on a team die

            // TODO: Determine who won round, reset it and add the respective points
            spawnSystem.despawnTeams(newTeams.pinkTeam, newTeams.redTeam); // I have no idea if this function will be useful or not :/
    
            
        }
    }
}
