using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LeaderboardEntry : MonoBehaviour
{
    public TextMeshPro RankNumText;
    public TextMeshPro DisplayNameText;
    public TextMeshPro ScoreValueText;


    public void SetEntryValues(int _rankNum, string _displayName, string _score)
    {
        RankNumText.SetText($"{_rankNum}.");
        DisplayNameText.SetText(_displayName);
        ScoreValueText.SetText(_score);

        StartCoroutine(SpinX(1f)); 
    }


    IEnumerator SpinX(float duration)
    {
        float timer = 0;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            float rot = Mathf.Lerp(0, 360, t);
            transform.eulerAngles = new Vector3(rot, 0, 0);

            yield return null;
        }
    }

}
