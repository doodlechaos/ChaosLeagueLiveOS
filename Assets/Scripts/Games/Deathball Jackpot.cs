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
       /* _Deathballs[0].transform.localPosition = new Vector3(0, 4.75f, 0);
        _Deathballs[1].transform.localPosition = new Vector3(0, -4.75f, 0);
        _Deathballs[2].transform.localPosition = new Vector3(4.75f, 0, 0);
        _Deathballs[3].transform.localPosition = new Vector3(-4.75f, 0, 0);
        _Deathballs[4].transform.localPosition = new Vector3(0, 3, 0);
        _Deathballs[5].transform.localPosition = new Vector3(0, -3, 0);
        _Deathballs[6].transform.localPosition = new Vector3(3, 0, 0);
        _Deathballs[7].transform.localPosition = new Vector3(-3, 0, 0);
        _Deathballs[8].transform.localPosition = new Vector3(0, 0, 0);

        foreach (DeathBall plips in _Deathballs)
        {
            plips.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
        //_currInterval = 0; */
    }

    public override void OnTileInitInPos()
    {
        
    }


    public override void StartGame()
    {
        
    }


    public override void ProcessGameplayCommand(string messageId, TwitchClient twitchClient, PlayerHandler ph, string msg, string rawEmotesRemoved)
    {

    }
    public override void CleanUpGame()
    {
        DoneWithGameplay = true;

       /* _Deathballs[0].transform.localPosition = new Vector3(0, 4.75f, 0);
        _Deathballs[1].transform.localPosition = new Vector3(0, -4.75f, 0);
        _Deathballs[2].transform.localPosition = new Vector3(4.75f, 0, 0);
        _Deathballs[3].transform.localPosition = new Vector3(-4.75f, 0, 0);
        _Deathballs[4].transform.localPosition = new Vector3(0, 3, 0);
        _Deathballs[5].transform.localPosition = new Vector3(0, -3, 0);
        _Deathballs[6].transform.localPosition = new Vector3(3, 0, 0);
        _Deathballs[7].transform.localPosition = new Vector3(-3, 0, 0);
        _Deathballs[8].transform.localPosition = new Vector3(0, 0, 0);

        foreach (DeathBall plips in _Deathballs)
        {
            plips.transform.localRotation = Quaternion.Euler(0, 0, 0);
        } */

        //throw new NotImplementedException();

    }    
        
}
