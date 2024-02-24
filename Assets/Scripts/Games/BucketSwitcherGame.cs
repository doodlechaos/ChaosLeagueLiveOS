using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BucketSwitcherGame : Game
{
    [SerializeField] private TextMeshPro _timer;
    [SerializeField] private int _secondsBeforeSwitch = 5;

    public override void OnTilePreInit()
    {
        //Debug.Log("On tile init visuals in Danger Zone");
        int survivalReward = AppConfig.GetMult(_gt.RarityType);

        if (_gt.IsGolden)
            survivalReward *= AppConfig.inst.GetI("GoldenTileMultiplier");

        foreach (var effector in _gt.Effectors)
            effector.MultiplyCurrValue(survivalReward);
    }

    public override void StartGame()
    {
        StartCoroutine(RunGame());
    }

    public IEnumerator RunGame()
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

            foreach (var effector in _gt.Effectors)
            {
                if (effector.GetEffect().HasFlag(PBEffect.Subtract))
                {
                    effector.SetEffect(PBEffect.Add);
                    effector.MultiplyCurrValue(2.0f);
                    effector.transform.Find("Bottom").gameObject.SetActive(false);
                }
                else if (effector.GetEffect().HasFlag(PBEffect.Add))
                {
                    effector.SetEffect(PBEffect.Subtract | PBEffect.Explode);
                    effector.MultiplyCurrValue(0.5f);
                    effector.transform.Find("Bottom").gameObject.SetActive(true);
                }
            }

            seconds = 0;
        }
    }

    public override void CleanUpGame()
    {
        IsGameStarted = false;
    }
}
