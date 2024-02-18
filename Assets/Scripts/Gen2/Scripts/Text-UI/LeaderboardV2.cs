using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class LeaderboardV2 : MonoBehaviour
{
    [SerializeField] private GameManager _gm; 
    [SerializeField] private TextMeshPro _titleText;
    [SerializeField] private List<LeaderboardEntry> _rowEntries;

    List<(string displayName, string score)> entries = new List<(string displayName, string score)>();

    public void Awake()
    {
        StartCoroutine(UpdateLBTimer()); 
    }

    IEnumerator UpdateLBTimer()
    {
        while (true)
        {

            try
            {
                //Update leaderboard with who has the most points. Was somehow getting null values in the playerhandlers dictionary causing an error
                foreach (PlayerHandler ph in _gm.PlayerHandlers.Values.Where(x => x != null && x.pp != null).OrderByDescending(x => x.pp.SessionScore).Take(3).ToList())
                    entries.Add((ph.pp.TwitchUsername, MyUtil.AbbreviateNum4Char(ph.pp.SessionScore).ToString()));
            }
            catch (Exception e)
            {
                Debug.LogError("Exception in leaderboard1: " + e);
            }

            UpdateLB("Points Leaderboard", entries);
            yield return new WaitForSeconds(10);
            entries.Clear();

            try
            {
                //Update leaderboard with who has the most gold
                foreach (PlayerHandler ph in _gm.PlayerHandlers.Values.Where(x => x != null && x.pp != null).OrderByDescending(x => x.pp.ThroneCaptures).Take(3).ToList())
                    entries.Add((ph.pp.TwitchUsername, MyUtil.AbbreviateNum4Char(ph.pp.ThroneCaptures).ToString()));
            }
            catch (Exception e)
            {
                Debug.LogError("Exception in leaderboard2: " + e);
            }

            UpdateLB("Most Throne Captures", entries);
            yield return new WaitForSeconds(10);
            entries.Clear();

            try
            {
                //Update leaderboard with who has the most gold
                foreach (PlayerHandler ph in _gm.PlayerHandlers.Values.Where(x => x != null && x.pp != null).OrderByDescending(x => x.pp.TimeOnThrone).Take(3).ToList())
                    entries.Add((ph.pp.TwitchUsername, MyUtil.GetHourMinSecString(ph.pp.TimeOnThrone).ToString()));
            }
            catch (Exception e)
            {
                Debug.LogError("Exception in leaderboard2: " + e);
            }

            UpdateLB("Most Throne Time", entries);
            yield return new WaitForSeconds(10);
            entries.Clear();

            try
            {
                //Update leaderboard with who has the most gold
                foreach (PlayerHandler ph in _gm.PlayerHandlers.Values.Where(x => x != null && x.pp != null).OrderByDescending(x => x.pp.GetInviteIds().Length).Take(3).ToList())
                    entries.Add((ph.pp.TwitchUsername, ph.pp.GetInviteIds().Length.ToString()));
            }
            catch (Exception e)
            {
                Debug.LogError("Exception in leaderboard2: " + e);
            }

            UpdateLB("Biggest Pyramid Scheme", entries);
            yield return new WaitForSeconds(10);
            entries.Clear();
        }
    }

  

    public void UpdateLB(string title, List<(string _displayName, string _score)> data)
    {
        _titleText.SetText(title);

        for(int i = 0; i < _rowEntries.Count; i++)
        {
            if (data.Count - 1 < i)
                break;

            _rowEntries[i].SetEntryValues(i + 1, data[i]._displayName, data[i]._score);

        }
    }


}
