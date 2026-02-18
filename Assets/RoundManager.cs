using UnityEngine;

public class RoundManager : MonoBehaviour
{
    private int roundsToWin;
    private int currentRound;
    public int teamPinkScore;
    public int teamRedScore;
    // The main manager for the round system
    void Start()
    {
        // When the game starts, designate teams, then spawn them in their respective spawns
        // Start a small countdown before each round ie.. (3..2..1.. FIGHT!)
        // If time runs out, its a draw, no one gains a point
        // After 20-30 seconds, power ups spawn in the middle. 
        // If all members of a team die, they lose; Other team gains a point. 
        // Game ends once one of the scores reaches the required roundsToWin

        //Systems needed:
        // * Spawn System
        // * PowerUp System
        // * TeamDesignator System

        // I'll try and have the main framework for this code done by Thursday. -Samir
    }


}
