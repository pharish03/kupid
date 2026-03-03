using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public int roundsToWin;
    [SerializeField] private GameObject PinkUI;
    [SerializeField] private GameObject RedUI;
    private int currentRound;
    public int teamPinkScore;
    public int teamRedScore;
    public bool inRound;


    private Coroutine mainCorutine;

    // I think we have to tag the object with a player tag?? So the game knows whats a player and whats not

    // Systems:
    [Header("Systems")]
    [SerializeField] SpawnSystem spawnSystem;
    [SerializeField] TeamSystem teamSystem;
    [SerializeField] Countdown countdown;
    public TeamSystem.Teams currentTeams;

    // The main manager for the round system
    void Start()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        // TODO: Figure out a way to ensure each player is assigned to a game object
        mainCorutine = StartCoroutine(StartRoundLoop(players));
        // TODO: Start a 3 second countdown before each round.
      

        

    }

    void Update()
    {
        if(inRound)
        {
            teamSystem.CheckWhosAlive(currentTeams);
        }
    }


   private IEnumerator StartRoundLoop(GameObject[] plrs)
    {
        
        currentTeams = teamSystem.DesignateTeam(plrs);
        while (teamRedScore < roundsToWin || teamPinkScore < roundsToWin)
        {
          
            countdown.remainingTime = 10;
            spawnSystem.spawnTeams(currentTeams.pinkTeam, currentTeams.redTeam);
            countdown.isTimerOn = true;
            // Round Started
            inRound = true;
            yield return new WaitUntil(() => !countdown.isTimerOn || !inRound); // Wait until timer ends or all players on a team die
            inRound = false;
            spawnSystem.despawnTeams(currentTeams.pinkTeam, currentTeams.redTeam);



        }

        

    }
}
