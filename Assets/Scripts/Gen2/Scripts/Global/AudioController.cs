using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    public static AudioController inst;

    public AudioSource DefenseBrickTakeDamage; 
    public AudioSource PayItForwardExplosion;
    public AudioSource DeathByLava;
    public AudioSource DeathByContact;
    public AudioSource DeathScream;
    public AudioSource LavaDump;
    public AudioSource BrickRelease;
    public AudioSource NewKingThroned;
    public AudioSource AddPoints;
    public AudioSource MultiplyPoints;
    public AudioSource BattlePerchEarn;
    [Space(20)]
    public AudioSource Beheading;
    public AudioSource ReleasePlayer;
    public AudioSource TileSpinStart;
    public AudioSource TomatoSplat;
    //public AudioSource CommunityPointBidSlideIn;
    public AudioSource BidQSwitchSide;
    public AudioSource AuctionPosOpen;
    public AudioSource RaffleSpotOpen;
    public AudioSource ThreeTwoOneGo;
    public AudioSource CommunityPointParticlePop;
    public AudioSource BitsParticlePop;
    public AudioSource NewInviteFlipIndicator;
    public AudioSource AutoSplitterSwitch;
    public AudioSource Switch2;
    public AudioSource SingleBitDing;
    public AudioSource StorePurchase;
    public AudioSource SpawnNewSuperCapsule;
    public AudioSource RebellionCapsuleImpact;
    public AudioSource RebellionEnd;
    public AudioSource SpikesExtend;
    public AudioSource SpikesRetract;
    public AudioSource CollectGold;
    public AudioSource ButtonDown;
    public AudioSource ButtonUp;
    public AudioSource MechanicalPivotMove;

    // Start is called before the first frame update
    void Awake()
    {
        inst = this; 
    }

    public void PlaySound(AudioSource audioSource, float pitchMin, float pitchMax)
    {
        if(audioSource == null)
        {
            CLDebug.Inst.Log("Missing audio source. Cancelling playsound.");
            return;
        }


        try
        {
            float pitch = Random.Range(pitchMin, pitchMax);
            pitch = Mathf.Clamp(pitch, -3, 3);
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(audioSource.clip);
        } catch (System.Exception e)
        {
            CLDebug.Inst.LogError(e.ToString());
        }

    }


}
