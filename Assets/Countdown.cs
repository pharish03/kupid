using UnityEngine;
using System.Collections;
using TMPro;
public class Countdown : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] float remainingTime;
    public bool isTimerOn;
    void Update()
    {
        if (isTimerOn)
        {
            if (remainingTime > 0)
            {
                remainingTime -= Time.deltaTime;
            }
            else if (remainingTime < 0)
            {
                remainingTime = 0;
                isTimerOn = false;
                // TODO: Tell the round manager to count the round as a draw and start a new round
            }


            int minutes = Mathf.FloorToInt(remainingTime / 60);
            int seconds = Mathf.FloorToInt(remainingTime % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
      
    }

    public void setCountdown(float newTime){
        remainingTime = newTime;
    }
}
