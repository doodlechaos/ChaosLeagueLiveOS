using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class BucketSwitcherGame : Game
{
    private class BottomTarget
    {
        public GameObject bottom;
        public Vector3 target;
        public float speed;

        public BottomTarget(GameObject _bottom, Vector3 _target, float _speed)
        {
            bottom = _bottom;
            target = _target;
            speed = _speed;
        }
    }

    [SerializeField] private TextMeshPro _timer;
    [SerializeField] private int _secondsBeforeSwitch = 5;

    [HideInInspector] private List<BottomTarget> _bottomTargets = new List<BottomTarget>();

    private int _currState = 0;

    public override void OnTilePreInit()
    {
        int survivalReward = AppConfig.GetMult(_gt.RarityType);

        if (_gt.IsGolden)
            survivalReward *= AppConfig.inst.GetI("GoldenTileMultiplier");

        foreach (var effector in _gt.Effectors)
            effector.MultiplyCurrValue(survivalReward);
    }

    public void Update()
    {
        List<int> indexesToRemove = new List<int>();

        for (int i = 0; i < _bottomTargets.Count(); i++)
        {
            bool hasMoved = MoveBottom(_bottomTargets[i].bottom, _bottomTargets[i].target, _bottomTargets[i].speed);
            if (!hasMoved)
                indexesToRemove.Add(i);
        }

        // Order the list to have big indexes first, then small indexes after
        // as it would otherwise error out with 'index out of range'
        indexesToRemove = indexesToRemove.OrderBy(x => -x).ToList();

        foreach (var index in indexesToRemove)
            _bottomTargets.RemoveAt(index);
    }

    public override void StartGame()
    {
        StartCoroutine(RunGame());
    }

    private IEnumerator RunGame()
    {
        int seconds = 0;

        // for some reason, the first frame (presumably) doesn't have IsGameStarted set to true
        while (IsGameStarted || seconds == 0)
        {
            if (seconds < _secondsBeforeSwitch) {
                _timer.SetText(MyUtil.GetMinuteSecString(_secondsBeforeSwitch - seconds));
                yield return new WaitForSeconds(1);
                seconds++;

                continue;
            }

            SwitchEffectors();
            seconds = 0;
        }
    }

    private void SwitchEffectors()
    {
        _currState = (_currState + 1) % 4;

        for (int i = 0; i < _gt.Effectors.Length; i++)
        {
            PBEffector effector = _gt.Effectors[i];

            int workState = _currState;

            int rowIndex = i/8;
            workState += rowIndex;

            // Wrap the value to be 0, 1, 2 or 3
            workState = workState % 4;

            if (i % 4 == workState)
            {
                effector.SetEffect(PBEffect.Subtract | PBEffect.Explode);
                effector.MultiplyCurrValue(0.5f);

                GameObject bottom = effector.transform.Find("Bottom").gameObject;

                ResetBottomCollider(effector);

                Vector3 position = bottom.transform.position;
                position.z = 0.54f;

                AddInAnimationList(bottom, position);
            }
            else if (effector.GetEffect().HasFlag(PBEffect.Subtract))
            {
                effector.SetEffect(PBEffect.Add);
                effector.MultiplyCurrValue(2.0f);

                GameObject bottom = effector.transform.Find("Bottom").gameObject;

                ResetBottomCollider(effector);

                Vector3 position = bottom.transform.position;
                position.z = 1.2f;
                AddInAnimationList(bottom, position);
            }
        }
    }

    private bool MoveBottom(GameObject bottom, Vector3 target, float speed)
    {
        if (bottom.transform.position == target)
            return false;

        Vector3 nextPos = Vector3.MoveTowards(bottom.transform.position, target, speed);
        bottom.transform.position = nextPos;

        return true;
    }

    public override void CleanUpGame()
    {
        _currState = 0;
        for (int i = 0; i < _gt.Effectors.Length; i++)
        {
            int workState = 0;
            int rowIndex = i/8;
            workState += rowIndex;

            workState = workState % 4;

            PBEffector effector = _gt.Effectors[i];
            effector.ResetValue(false);

            if (i % 4 == workState)
                effector.SetEffect(PBEffect.Subtract | PBEffect.Explode);
            else
                effector.SetEffect(PBEffect.Add);

            ResetBottomCollider(effector);
        }

        _timer.SetText(MyUtil.GetMinuteSecString(0));
        _bottomTargets.Clear();
        IsGameStarted = false;
    }

    private void ResetBottomCollider(PBEffector effector)
    {
        GameObject bottom = effector.transform.Find("Bottom").gameObject;

        if (bottom == null)
            return;

        BoxCollider2D collider = bottom.GetComponent<BoxCollider2D>();
        if (collider == null)
            return;

        if (effector.GetEffect().HasFlag(PBEffect.Add))
            collider.enabled = false;
        else if (effector.GetEffect().HasFlag(PBEffect.Subtract))
            collider.enabled = true;
    }

    private void AddInAnimationList(GameObject bottom, Vector3 target)
    {
        BottomTarget bottomTarget = new BottomTarget(bottom, target, 0.05f);
        _bottomTargets.Add(bottomTarget);
    }
}
