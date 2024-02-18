using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;

// {get; set;} is required for dynamodb to automatically create playerprofile from entry

[Serializable]
[Table("PlayerProfiles")] 

public class PlayerProfile
{
    [PrimaryKey] 
    public string TwitchID { get; set; }
    public string TwitchUsername { get; set; }
    public string InvitedByID { get; set; }
    public string InvitesJSON { get; set; }

    public bool IsSubscriber { get; set; }

    public string NameColorHex { get; set; }
    public string CrownJSON {  get; set; }
    public string TrailGradientJSON { get; set; }
    public string SpeechBubbleFillHex { get; set; }
    public string SpeechBubbleTxtHex { get; set; }
    public string CurrentVoiceID { get; set; }
    [Ignore]
    public string[] PurchasedVoiceIDs { get; set; }

    // POINTS

    public int ThroneCaptures { get; set; }
    public int TimeOnThrone { get; set; }
    public int TotalTicketsSpent { get; set; }

    public int CurrentBid { get; set; }
   
    
    public int LifeTimeScore { get; set; }
    public int Gold { get; set; }
    public int SeasonScore { get; set; }
    public long SessionScore { get; set; }
    public DateTime LastInteraction { get; set; }

    public string[] GetInviteIds() 
    {
        if(string.IsNullOrEmpty(InvitesJSON))
            return Array.Empty<string>();
        return JsonConvert.DeserializeObject<string[]>(InvitesJSON);
    }

    private void SetInviteIds(string[] inviteIds)
    {
        InvitesJSON = JsonConvert.SerializeObject(inviteIds);
    }

    public void AddInvite(string id)
    {
        List<string> currentInvites = GetInviteIds().ToList();
        //Don't add if it's already in the list
        if (currentInvites.Contains(id))
            return;
        currentInvites.Add(id);
        SetInviteIds(currentInvites.ToArray());
    }
}