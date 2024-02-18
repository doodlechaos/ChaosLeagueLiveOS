using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OscillatorV2 : MonoBehaviour
{
    [SerializeField] private GameTile _gt;

    [SerializeField] private AnimationCurve _movementCurve;

    [SerializeField] private Rigidbody2D _obj;
    [SerializeField] private Rigidbody2D _reverseObj;

    [SerializeField] private Transform _objNoPhys;
    [SerializeField] private Transform _reverseObjNoPhys;

    [SerializeField] private MeshRenderer _targetPos1;
    [SerializeField] private MeshRenderer _targetPos2;

    [SerializeField] private float _intervalDuration;

    [SerializeField] private float _t_offset = 0;

    private float _timer = 0;
    private bool _enabled = false; 

    private void Awake()
    {
        _targetPos1.enabled = false;
        _targetPos2.enabled = false;
        SetPos(true, _t_offset);
    }

    public void ToggleOnOff(bool toggle)
    {
        _enabled = toggle; 
    }

    private void FixedUpdate()
    {
        //If we're not in gameplay or bidding, don't rotate
        if (_gt.TileState == TileState.Inactive)
            return;

        if (!_enabled)
            return;

        _timer = (_timer + Time.fixedDeltaTime) % _intervalDuration;

        float t = _timer / _intervalDuration;
        t = (t + _t_offset) % 1;

        SetPos(false, t); 
    }

    private void SetPos(bool force, float t)
    {
        Vector3 nextPos = Vector3.Lerp(_targetPos1.transform.position, _targetPos2.transform.position, _movementCurve.Evaluate(t));

        Vector3 nextPosRev = Vector3.Lerp(_targetPos2.transform.position, _targetPos1.transform.position, _movementCurve.Evaluate(t));

        if (force)
        {
            if(_obj != null)
                _obj.transform.position = new Vector3(nextPos.x, nextPos.y, _obj.transform.position.z);
            if(_reverseObj != null)
                _reverseObj.transform.position = new Vector3(nextPosRev.x, nextPosRev.y, _reverseObj.transform.position.z);
            if(_objNoPhys != null)
                _objNoPhys.transform.position = new Vector3(nextPos.x, nextPos.y, _objNoPhys.transform.position.z);
            if(_reverseObjNoPhys != null)
                _reverseObjNoPhys.transform.position = new Vector3(nextPosRev.x, nextPosRev.y, _reverseObjNoPhys.transform.position.z);
            return;
        }

        if (_obj != null && _obj.simulated)
            _obj.MovePosition(nextPos);

        if (_reverseObj != null && _reverseObj.simulated)
            _reverseObj.MovePosition(nextPosRev);

        if (_objNoPhys != null)
            _objNoPhys.position = nextPos;

        if (_reverseObjNoPhys != null)
            _reverseObjNoPhys.position = nextPosRev;
    }

}
