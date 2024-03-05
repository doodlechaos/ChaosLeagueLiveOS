using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathBall : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spikeCircle;

    //[SerializeField] private float _retractedDuration;

    [SerializeField] private MeshRenderer _loadingRing;

    [SerializeField] public Rigidbody2D _rb;

    private float _retractedDuration;
    private WaitForSeconds _retractedWait;

    private MaterialPropertyBlock _matPropBlock;

/*    private Vector2 _vel;
    private float _aVel; */

    public void InitDB(float retractedDuration)
    {
        _retractedDuration = retractedDuration;
        _retractedWait = new WaitForSeconds(retractedDuration);
        _spikeCircle.enabled = true;
        _rb.isKinematic = false;

        _matPropBlock = new MaterialPropertyBlock();
        _matPropBlock.SetFloat("_FillAmount", 1);
        _loadingRing.SetPropertyBlock(_matPropBlock);
    }

    public void DetectedPB(PlayerBall pb)
    {
        if (!_spikeCircle.enabled)
            return;

        //Destroy the player ball
        pb.ExplodeBall();
        AudioController.inst.PlaySound(AudioController.inst.DeathByContact, 0.95f, 1.05f); 

        StartCoroutine(TempRetractSpikes()); 
    }

    private IEnumerator TempRetractSpikes()
    {
        _spikeCircle.enabled = false;
        //Play spike retract sfx
        AudioController.inst.PlaySound(AudioController.inst.SpikesRetract, 0.95f, 1.05f);

        _rb.isKinematic = true;
/*        _vel = _rb.velocity;
        _aVel = _rb.angularVelocity;*/
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = 0; 
        float timer = 0; 
        while(timer <= _retractedDuration)
        {
            _matPropBlock.SetFloat("_FillAmount", timer / _retractedDuration);
            _loadingRing.SetPropertyBlock(_matPropBlock);
            timer += Time.deltaTime; 
            yield return null;
        }
        _rb.isKinematic = false;
/*        _rb.velocity = _vel;
        _rb.angularVelocity = _aVel;*/
        //Play spike deploy sfx
        _spikeCircle.enabled = true;
        AudioController.inst.PlaySound(AudioController.inst.SpikesExtend, 0.95f, 1.05f);

    }
}
