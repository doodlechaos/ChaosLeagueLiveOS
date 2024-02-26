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

    public override void OnTilePreInit()
    {
        int survivalReward = AppConfig.GetMult(_gt.RarityType);

        if (_gt.IsGolden)
            survivalReward *= AppConfig.inst.GetI("GoldenTileMultiplier");

        foreach (var effector in _gt.Effectors)
        {
            effector.ResetValue(false);
            effector.MultiplyCurrValue(survivalReward);
        }
    }

    public void Update()
    {
        List<int> indexesToRemove = new List<int>();

        for (int i = 0; i < _bottomTargets.Count(); i++)
        {
            bool removeTarget = MoveBottom(_bottomTargets[i].bottom, _bottomTargets[i].target, _bottomTargets[i].speed);
            if (removeTarget)
                indexesToRemove.Add(i);
        }

        // Order the list to have big indexes first, then small indexes after
        // as it would otherwise error out with 'index out of range'
        indexesToRemove = indexesToRemove.OrderBy(x => -x).ToList();

        foreach (var index in indexesToRemove)
        {
            _bottomTargets.RemoveAt(index);
        }
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
        foreach (var effector in _gt.Effectors)
        {
            if (effector.GetEffect().HasFlag(PBEffect.Subtract))
            {
                effector.SetEffect(PBEffect.Add);
                effector.MultiplyCurrValue(2.0f);

                GameObject bottom = effector.transform.Find("Bottom").gameObject;

                BoxCollider2D collider = bottom.GetComponent<BoxCollider2D>();
                if (collider != null)
                    collider.enabled = false;

                Vector3 position = bottom.transform.position;
                position.z = 1.34f;

                BottomTarget target = new BottomTarget(bottom, position, 0.05f);
                _bottomTargets.Add(target);
            }
            else if (effector.GetEffect().HasFlag(PBEffect.Add))
            {
                effector.SetEffect(PBEffect.Subtract | PBEffect.Explode);
                effector.MultiplyCurrValue(0.5f);

                GameObject bottom = effector.transform.Find("Bottom").gameObject;

                BoxCollider2D collider = bottom.GetComponent<BoxCollider2D>();
                if (collider != null)
                    collider.enabled = true;

                Vector3 position = bottom.transform.position;
                position.z = 0.58f;

                BottomTarget target = new BottomTarget(bottom, position, 0.05f);
                _bottomTargets.Add(target);
            }
        }
    }

    private bool MoveBottom(GameObject bottom, Vector3 target, float speed)
    {
        if (bottom.transform.position == target)
            return true;

        Vector3 nextPos = Vector3.MoveTowards(bottom.transform.position, target, speed);

        if (Vector3.Distance(nextPos, target) < 0.1f)
            return true;

        bottom.transform.position = nextPos;

        return false;
    }

    public override void CleanUpGame()
    {
        _timer.SetText(MyUtil.GetMinuteSecString(0));
        _bottomTargets.Clear();
        IsGameStarted = false;
    }
}
