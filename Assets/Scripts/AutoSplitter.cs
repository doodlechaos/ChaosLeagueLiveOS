using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoSplitter : MonoBehaviour
{

    [SerializeField] private PBDetector _pbCollisionDetector;

    [SerializeField] private PipeReleaser _leftPipe;
    [SerializeField] private PipeReleaser _rightPipe;

    [SerializeField] private SpriteRenderer _arrow;

    private bool _sideToggle = true;

    public void DetectedPB(PlayerBall pb)
    {
        if (_sideToggle)
            _leftPipe.ReceivePlayer(pb);
        else
            _rightPipe.ReceivePlayer(pb);

        _sideToggle = !_sideToggle;

        //Point the arrow to the next side
        Vector3 rot = _arrow.transform.eulerAngles;
        rot.z = (_sideToggle) ? 180 : 0;
        _arrow.transform.eulerAngles = rot;

        AudioController.inst.PlaySound(AudioController.inst.AutoSplitterSwitch, 0.95f, 1.05f); 
    }
}
