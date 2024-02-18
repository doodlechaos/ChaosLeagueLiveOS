using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

public class MyHttpClient : MonoBehaviour
{
    private HttpClient _client;
    private StringBuilder _sb;

    public void Awake()
    {
        _client = new HttpClient();
        _sb = new StringBuilder();

    }
/*    public async Task<bool> IsYTAccountAtLeasetOneWeekOld(string youtubeID)
    {
        try
        {
            _sb.Clear();
            _sb.Append("https://www.youtube.com/channel/");
            _sb.Append(youtubeID);
            _sb.Append("/about");
            string url = _sb.ToString();

            //CLDebug.Inst.Log(url);

            string pageSource = await _client.GetStringAsync(url);

            //CLDebug.Inst.Log(pageSource);


            // Define the regular expression pattern to match the joined date
            //string pattern = "\"joinedDateText\":{\"runs\":\\[{\"text\":\"Joined \",\"text\":\"([A-Za-z]{3} \\d{1,2}, \\d{4})\"\\}\\]}";
            //string pattern = "\"joinedDateText\":{ \"runs\":\\[{ \"text\":\"Joined \"},{ \"text\":\"([A-Za-z]{3} \\d{1,2}, \\d{4})\"}\\]}";
            string pattern = @"\b(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s\d{1,2},\s\d{4}\b";

            Match match = Regex.Match(pageSource, pattern);


            if (match.Success)
            {
                string joinedDateString = match.Groups[0].Value;
                CLDebug.Inst.Log(joinedDateString);
                DateTime joinedDate = DateTime.Parse(joinedDateString);
                DateTime currentDate = DateTime.Now;

                // Check if the joined date is within a week of the current date
                if ((currentDate - joinedDate).TotalDays <= 7)
                {
                    CLDebug.Inst.Log($"Joined Date: {joinedDateString} (within a week)");
                    return false;
                }

                CLDebug.Inst.Log($"Joined Date: {joinedDateString} (more than a week ago)");
                return true;
                
            }
            else
                CLDebug.Inst.Log("Joined date not found.");
        }
        catch (Exception ex)
        {
            CLDebug.Inst.LogError($"An error occurred while checking youtube account age: {ex.Message}");
        }
        return false;

    }*/
}
