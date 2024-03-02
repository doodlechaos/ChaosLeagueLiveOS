using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class DeathballJackpot : Game
{
    [SerializeField] private List<DeathBall> _Deathballs;
   
    

    private void Awake()
    {
        
    }
    
    public override void OnTilePreInit()
    {
        foreach (DeathBall plips in _Deathballs)
        {
            plips.transform.localPosition = new Vector3(0, 0, 0);
            Debug.Log("Did a Deathball Thing");
        }
        //_currInterval = 0;
    }

    public override void OnTileInitInPos()
    {
       
    }
            

    public override void StartGame()
    {
        //StartCoroutine(RunGame()); 
    }
        

    public override void ProcessGameplayCommand(string messageId, TwitchClient twitchClient, PlayerHandler ph, string msg, string rawEmotesRemoved)
    {

    }
    public override void CleanUpGame()
    {
        DoneWithGameplay = true;
               
        //throw new NotImplementedException();
              
    }    
        
}
