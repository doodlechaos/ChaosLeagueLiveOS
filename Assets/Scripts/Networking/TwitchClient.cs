// If type or namespace TwitchLib could not be found. Make sure you add the latest TwitchLib.Unity.dll to your project folder
// Download it here: https://github.com/TwitchLib/TwitchLib.Unity/releases
// Or download the repository at https://github.com/TwitchLib/TwitchLib.Unity, build it, and copy the TwitchLib.Unity.dll from the output directory
using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;
using TwitchLib.Unity;
using UnityEngine;
using UnityEngine.UIElements;

public class TwitchClient : MonoBehaviour
{
    [SerializeField] private GameManager _gm;
    [SerializeField] private AutoPredictions _autoPredictions;
    [SerializeField] private TwitchPubSub _twitchPubSub;
    [SerializeField] private TileController _tileController;
    [SerializeField] private KingController _kingController;
    [SerializeField] private BidHandler _bidHandler;
    [SerializeField] private PipeReleaser _attackPipe;
    [SerializeField] private DynamicSpriteAsset _dynamicSpriteAsset;
    [SerializeField] private DefaultDefenseV2 _defaultDefenseV2;
    [SerializeField] private SpotifyDJ _spotifyDJ;

    private Client _client;

    public void Init(string channelName, string botAccessToken)
    {
        if (_client != null)
            _client.Disconnect();

        ConnectionCredentials credentials = new ConnectionCredentials(channelName, botAccessToken);

        // Create new instance of Chat Client
        _client = new Client();

        // Initialize the client with the credentials instance, and setting a default channel to connect to.
        _client.Initialize(credentials, channelName);

        // Bind callbacks to events
        _client.OnConnected += OnConnected;
        _client.OnJoinedChannel += OnJoinedChannel;
        _client.OnMessageReceived += OnMessageReceived;
        _client.OnError += OnError;

        // Connect
        _client.Connect();

        Debug.Log("Done Initializing Twitch Client");
    }

    private void OnConnected(object sender, OnConnectedArgs e)
    {
        Debug.Log("Connected twitch client");
    }

    private void OnJoinedChannel(object sender, OnJoinedChannelArgs e)
    {
        Debug.Log($"The bot {e.BotUsername} just joined the channel: {e.Channel}");
        _client.SendMessage(e.Channel, "[BOT] Chaos League bot connected to the channel! PogChamp");
    }


    public void OnError(object sender, OnErrorEventArgs e)
    {
        Debug.LogError("On Twitch Client Error: " + e.Exception.ToString());
    }

    public void OnMessageReceived(object sender, OnMessageReceivedArgs e)
    {
        string messageId = e.ChatMessage.Id; 
        string twitchId = e.ChatMessage.UserId;
        string twitchUsername = e.ChatMessage.Username;
        Color usernameColor = Color.white;

        ColorUtility.TryParseHtmlString(e.ChatMessage.ColorHex, out usernameColor);

        Debug.Log($"Found name color in message: {MyUtil.ColorToHexString(usernameColor)} {e.ChatMessage.ColorHex}"); 
        string rawIrcMsg = e.ChatMessage.RawIrcMessage;
        string rawMsg = e.ChatMessage.Message;
        bool isSubscriber = e.ChatMessage.IsSubscriber;
        bool isFirstMessage = e.ChatMessage.IsFirstMessage;
        int bits = e.ChatMessage.Bits;
        bool isAdmin = (twitchId == Secrets.CHANNEL_ID); //e.chatmessage.isMe doesn't work for some reason

        //Debug.Log($"Total emotes: {e.ChatMessage.EmoteSet.Emotes.Count} emote replaced message: {e.ChatMessage.EmoteReplacedMessage} rawIrcMsg: {rawIrcMsg}");
        List<Emote> emotes = e.ChatMessage.EmoteSet.Emotes;
        emotes.Sort((emote1, emote2) => emote1.StartIndex.CompareTo(emote2.StartIndex));

        StartCoroutine(HandleMessage(messageId, twitchId, twitchUsername, usernameColor, rawMsg, emotes, isSubscriber, isFirstMessage, bits, isAdmin));

        Debug.Log(JsonConvert.SerializeObject(e, formatting:Formatting.Indented).ToString());

        //If the message is a hype chat, give them the multiplier zone
        //e.ChatMessage.user
        Debug.Log($"Message received from {e.ChatMessage.Username}: {e.ChatMessage.Message}   id: {e.ChatMessage.Id} total bits: {e.ChatMessage.Bits} {e.ChatMessage.BitsInDollars} {e.ChatMessage.CheerBadge} isAdmin: {isAdmin}");

    }

    public IEnumerator HandleMessage(string messageId, string twitchId, string twitchUsername, Color usernameColor, string rawMsg, List<Emote> emotes, bool isSubscriber, bool isFirstMessage, int bits, bool isAdmin)
    {

        //Debug.LogError($"Handling message from: API_MODE: {AppConfig.inst.GetS("API_MODE")} ClientID: {AppConfig.GetClientID()} ClientSecret: {AppConfig.GetClientSecret()}");

        bool isMe = twitchId == Secrets.CHANNEL_ID;
        string sanitizedMsg = rawMsg.Replace("<", "").Replace(">", "");

        string rawEmotesRemoved = sanitizedMsg;
        string spriteInfusedMsg = sanitizedMsg;
        if(emotes != null && emotes.Count > 0)
        {
            Debug.Log("Found emotes: " +  emotes.Count);
            rawEmotesRemoved = RemoveTwitchEmotes(rawMsg, emotes);
            rawEmotesRemoved = rawEmotesRemoved.Replace("<", "").Replace(">", "");

            CoroutineResult<string> coRes = new CoroutineResult<string>();
            yield return _dynamicSpriteAsset.GetSpriteInfusedMsg(coRes, sanitizedMsg, emotes, isMe); //If sanitizing messes up the sprites that's unavoidable

            spriteInfusedMsg = coRes.Result;
        }

        //Debug.Log($"rawEmotesRemoved: {rawEmotesRemoved}\nspriteInfusedMsg: {spriteInfusedMsg}\n"); 

        //If the player doesn't have an active player handler, create one
        CoroutineResult<PlayerHandler> coResult = new CoroutineResult<PlayerHandler>();
        yield return _gm.GetPlayerHandler(twitchId, coResult);

        PlayerHandler ph = coResult.Result;

        if (ph == null)
        {
            Debug.LogError($"Failed to get or create player handler {twitchId} {twitchUsername} in twitch client handle message");
            yield break;
        }

        ph.pp.LastInteraction = DateTime.Now;
        ph.pp.TwitchUsername = twitchUsername;
        ph.pp.IsSubscriber = isSubscriber;
        ph.pp.NameColorHex = MyUtil.ColorToHexString(usernameColor);

        //Set the player handler customizations
        ph.SetCustomizationsFromPP();

        if (sanitizedMsg.StartsWith('!'))
        {
            if (isAdmin)
                ProcessAdminCommands(messageId, ph, sanitizedMsg, bits); 

            //If player is not spawned in bidding or gameplay tile in any form
            ProcessGlobalCommands(messageId, ph, sanitizedMsg, bits);
        }
        else
        {
            ph.SpeechBubble(spriteInfusedMsg);
            if (ph.IsKing())
            {
                MyTTS.inst.PlayerSpeech(rawEmotesRemoved, Amazon.Polly.VoiceId.Joey);
                if (rawEmotesRemoved.ToLower().Contains("zobm"))
                    _autoPredictions.KingWordSignal(); 
            }
        }

        //Process gameplay commands even if the message doesn't start with a '!', because we want to use TTS in quip battle
        ProcessGameplayCommands(messageId, ph, sanitizedMsg, rawEmotesRemoved);

        if (isFirstMessage && AppConfig.inst.GetB("EnableFirstMessageBonus"))
        {
            MyTTS.inst.Announce($"New player! Everyone welcome {twitchUsername} to the Chaos League.");
            _bidHandler.BidRedemption(ph, AppConfig.inst.GetI("FirstMessageBonusBid"), BidType.NewPlayerBonus); 
        }


    }

    public void ReplyToPlayer(string messageId, string username, string message)
    {
        if (string.IsNullOrEmpty(messageId))
        {
            PingReplyPlayer(username, message);
            return;
        }
        _client.SendReply(Secrets.CHANNEL_NAME, messageId, $"[BOT] {message}"); 
    }
    public void PingReplyPlayer(string twitchUsername, string message)
    {
        _client.SendMessage(Secrets.CHANNEL_NAME, $"[BOT] @{twitchUsername} {message}"); 
    }

    private void ProcessAdminCommands(string messageId, PlayerHandler ph, string msg, int bits)
    {
        string commandKey = msg.ToLower();
        if (commandKey.StartsWith("!adminbits"))
        {
            StartCoroutine(ProcessAdminGiveBits(messageId, ph, msg)); 
            return;
        }
        else if (commandKey.StartsWith("!adminskipgameplay"))
        {
            _tileController.GameplayTile?.ForceEndGameplay();
            return;
        }


    }

    private void ProcessGlobalCommands(string messageId, PlayerHandler ph, string msg, int bits)
    {
        string commandKey = msg.ToLower();

        if(commandKey.StartsWith("!commands") || commandKey.StartsWith("!help"))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"More info and a list of all commands are located below on my stream page panels.");
            return;
        }

        else if (commandKey.StartsWith("!wiki"))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"https://chaosleaguewiki.github.io");
            return;
        }
        else if (commandKey.StartsWith("!patreon"))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"https://www.patreon.com/doodlechaos");
            return;
        }
        else if (commandKey.StartsWith("!discord"))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Join the discord to chat with other players and share your thoughts on the game: https://discord.gg/tCjGjF68ds");
            return;
        }

        else if (commandKey.StartsWith("!invite") || commandKey.StartsWith("!recruit") || commandKey.StartsWith("!pyramidscheme") || commandKey.StartsWith("!invitelink") || commandKey.StartsWith("!getinvitelink") || commandKey.StartsWith("!getreferrallink"))
        {           
            string url = $"{Secrets.CHAOS_LEAGUE_DOMAIN}/@{ph.pp.TwitchUsername}";
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Share to start your pyramid scheme. Every player that joins the stream with your invite link earns you 25% of the gold they earn (yes it compounds)! \n{url}");
            return;
        }
        else if (commandKey.StartsWith("!coinflip") || commandKey.StartsWith("!flipcoin"))
        {
            string coinMsg = (UnityEngine.Random.Range(0f, 1f) < 0.5f) ? "The RNG Gods declare... HEADS" : "The RNG Gods declare... TAILS";
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, coinMsg);
            return;
        }

        else if (commandKey.StartsWith("!attack"))
        {
            if (ph.pb != null)
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You can't attack while your ball is already spawned");
                return;
            }
            if (ph.GetState() == PlayerHandlerState.BiddingQ)
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You can't attack while bidding for a tile");
                return;
            }
            if (ph.GetState() == PlayerHandlerState.Gameplay)
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You can't attack while participating in a tile.");
                return;
            }

            PlayerBall pb = ph.GetPlayerBall();
            ph.SetState(PlayerHandlerState.Gameplay); //Prevent bug where players could enter bidding Q while king if timed correctly
            ph.ReceivableTarget = null; //Prevent bug where players would move to raffle after attacking and get stuck
            _attackPipe.ReceivePlayer(pb); 
        }

        else if (commandKey.StartsWith("!defend"))
        {

            //Parse the points from the command
            string[] parts = msg.Split(' ');
            if (parts.Length < 2)
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Failed to parse defend points amount. Correct format is: !defend [amount]");
                return;
            }

            long pointsToDefend;
            if (!long.TryParse(parts[1], out pointsToDefend))
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Failed to parse defend points amount. Correct format is: !defend [amount]");
                return;
            }

            if (pointsToDefend <= 0)
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Defend amount must be at least 1 point.");
                return;
            }

            if (ph.pp.SessionScore <= 0)
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"You don't have any points to defend with.");
                return;
            }

            //If the user tries to use more points than they have, just clamp it
            if (ph.pp.SessionScore < pointsToDefend)
                pointsToDefend = ph.pp.SessionScore;

            if (_defaultDefenseV2 == null)
                return;

            _defaultDefenseV2.AddBonusDefense(pointsToDefend, ph);
        }

        else if (commandKey.StartsWith("!toll"))
        {
            if (!ph.IsKing())
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You must hold the throne to change the toll.");
                return;
            }

            string[] parts = msg.Split(' ');
            if (parts.Length < 2)
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to parse number. Correct format is: !toll [amount]");
                return;
            }

            int desiredTollRate;
            if (!int.TryParse(parts[1], out desiredTollRate))
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to parse number. Correct format is: !toll [amount]");
                return;
            }

            desiredTollRate = Mathf.Clamp(desiredTollRate, 0, AppConfig.inst.GetI("maxToll"));

            _kingController.UpdateTollRate(desiredTollRate);
        }

/*        else if (commandKey.StartsWith("!givepoints"))
        {
            StartCoroutine(ProcessGivePointsCommand(messageId, ph, msg));
        }*/

        else if (commandKey.StartsWith("!givegold"))
        {
            StartCoroutine(ProcessGiveGoldCommand(messageId, ph, msg)); 
        }

        else if (commandKey.StartsWith("!tomato"))
        {
            StartCoroutine(ProcessThrowTomato(messageId, ph, msg));
        }

        else if (commandKey.StartsWith("!stats") || commandKey.StartsWith("!mystats") || commandKey.StartsWith("!points"))
        {
            StartCoroutine(ProcessStatsCommand(messageId, ph, msg));
        }

        else if (commandKey.StartsWith("!cancelbid") || commandKey.StartsWith("!unbid"))
        {
            _bidHandler.ClearFromQ(ph, updateQ:true, unbid:true);
        }

        else if (commandKey.StartsWith("!song"))
        {
            if (!ph.IsKing())
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You must hold the throne to use !song");
                return;
            }
            string[] split = commandKey.Split("!song");
            if (split.Length < 2)
            {
                Debug.Log($"!song command failed. split.length: {split.Length}");
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to parse song name from command. ");
                return;
            }

            _ = _spotifyDJ.SearchAndPlay(messageId, split[1], ph);
        }

        else if (commandKey.StartsWith("!playlist"))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Music Options: {AppConfig.inst.GetS("SpotifySafePlaylistURL")}"); 
        }

        else if (ph.IsKing() && (commandKey.StartsWith("!skipsong") || commandKey.StartsWith("!skip song") || commandKey.StartsWith("!nextsong") || commandKey.StartsWith("!next song")))
        {
            _ = _spotifyDJ.SkipSong();
        }
        
        else if (commandKey.StartsWith("!lava"))
        {
            if(bits <= 0)
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You must include cheer bits in your message to load the lava bucket. Ex: '!lava [bit cheer]'");
                return;
            }
            //Handled in pub sub
        }
        
        else if (commandKey.StartsWith("!water"))
        {
            if (bits <= 0)
            {
                ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You must include cheer bits in your message to load the water bucket. Ex: '!water [bit cheer]'");
                return;
            }
            //Handled in pub sub
        }
    }


    private IEnumerator ProcessAdminGiveBits(string messageId, PlayerHandler ph, string msg)
    {
        //Get a user from the message
        if (!MyUtil.GetUsernameFromString(msg, out string targetUsername))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to find target username.");
            yield break;
        }

        long bitsAmount;
        if (!MyUtil.GetFirstLongFromString(msg, out bitsAmount))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to parse bits amount.");
            yield break;
        }

        if (bitsAmount <= 0)
            yield break;

        //Find if the player handler is cached and able to receive points
        CoroutineResult<PlayerHandler> coResult = new CoroutineResult<PlayerHandler>();
        yield return _gm.GetPlayerByUsername(targetUsername, coResult);
        PlayerHandler targetPlayer = coResult.Result;

        if (targetPlayer == null)
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Failed to find player with username: {targetUsername}");
            yield break;
        }

        MyUtil.ExtractQuotedSubstring(msg, out string quote); 

        yield return _twitchPubSub.HandleOnBitsReceived(targetPlayer.pp.TwitchID, targetPlayer.pp.TwitchUsername, quote, (int)bitsAmount); 
    }
    private IEnumerator ProcessGivePointsCommand(string messageId, PlayerHandler ph, string msg)
    {
        ReplyToPlayer(messageId, ph.pp.TwitchUsername, "This command has been disabled to combat alt account abuse.");
        yield break;

/*        if (!MyUtil.GetUsernameFromString(msg, out string targetUsername))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to find target username. Correct format is: !givepoints [amount] @username");
            yield break;
        }

        long desiredPointsToGive;
        if (!MyUtil.GetFirstLongFromString(msg, out desiredPointsToGive))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to parse point amount. Correct format is: !givepoints [amount] @username");
            yield break;
        }

        if (desiredPointsToGive <= 0)
            yield break;

        if (ph.pp.SessionScore <= 0)
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You have no points to give.");
            yield break;
        }

        //Find if the player handler is cached and able to receive points
        CoroutineResult<PlayerHandler> coResult = new CoroutineResult<PlayerHandler>();
        yield return _gm.GetPlayerByUsername(targetUsername, coResult);
        PlayerHandler targetPlayer = coResult.Result;


        if (targetPlayer == null)
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Failed to find player with username: {targetUsername}");
            yield break;
        }

        //Can't give points to yourself
        if (targetPlayer.pp.TwitchID == ph.pp.TwitchID)
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"You can't give points to yourself.");
            yield break;
        }

        //If the user tries to use more points than they have, just clamp it
        if (ph.pp.SessionScore < desiredPointsToGive)
            desiredPointsToGive = ph.pp.SessionScore;

        //Clamp givepoints limit to 10,000
        if (desiredPointsToGive > AppConfig.inst.GetI("GivePointsLimit"))
            desiredPointsToGive = AppConfig.inst.GetI("GivePointsLimit");

        ph.SubtractPoints(desiredPointsToGive, canKill: false, createTextPopup: true);

        TextPopupMaster.Inst.CreateTravelingIndicator(MyUtil.AbbreviateNum4Char(desiredPointsToGive), desiredPointsToGive, ph, targetPlayer, 0.1f, Color.green, ph.PfpTexture, TI_Type.GivePoints);
*/
    }
    private IEnumerator ProcessGiveGoldCommand(string messageId, PlayerHandler ph, string msg)
    {
        ReplyToPlayer(messageId, ph.pp.TwitchUsername, "This command has been disabled (temporarily?) to combat alt account abuse.");
        yield break;
        /*
        if (!MyUtil.GetUsernameFromString(msg, out string targetUsername))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to find target username. Correct format is: !givegold [amount] @username");
            yield break;
        }

        long desiredGoldToGive;
        if (!MyUtil.GetFirstLongFromString(msg, out desiredGoldToGive))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to parse point amount. Correct format is: !givegold [amount] @username");
            yield break;
        }

        if (desiredGoldToGive <= 0)
            yield break;

        if (ph.pp.Gold <= 0)
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You have no gold to give.");
            yield break;
        }

        //Find if the player handler is cached and able to receive points
        CoroutineResult<PlayerHandler> coResult = new CoroutineResult<PlayerHandler>();
        yield return _gm.GetPlayerByUsername(targetUsername, coResult);
        PlayerHandler targetPlayer = coResult.Result;

        if (targetPlayer == null)
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Failed to find player with username: {targetUsername}");
            yield break;
        }

        //Can't give points to yourself
        if (targetPlayer.pp.TwitchID == ph.pp.TwitchID)
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"You can't give gold to yourself.");
            yield break;
        }

        //If the user tries to use more points than they have, just clamp it
        if (ph.pp.Gold < desiredGoldToGive)
            desiredGoldToGive = ph.pp.Gold;

        //Clamp givepoints limit to 10,000
        if (desiredGoldToGive > AppConfig.inst.GetI("GivePointsLimit"))
            desiredGoldToGive = AppConfig.inst.GetI("GivePointsLimit");

        ph.SubtractGold((int)desiredGoldToGive, createTextPopup: true);

        TextPopupMaster.Inst.CreateTravelingIndicator(MyUtil.AbbreviateNum4Char(desiredGoldToGive), desiredGoldToGive, ph, targetPlayer, 0.1f, MyColors.Gold, ph.PfpTexture, TI_Type.GiveGold);
        */
    }
    private IEnumerator ProcessThrowTomato(string messageId, PlayerHandler ph, string msg)
    {
        if (!MyUtil.GetUsernameFromString(msg, out string targetUsername))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to find target username. Correct format is: !tomato [amount] @username");
            yield break;
        }

        long desiredTomatoAmount;
        if (!MyUtil.GetFirstLongFromString(msg, out desiredTomatoAmount))
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to parse point amount. Correct format is: !tomato [amount] @username");
            yield break;
        }

        if (desiredTomatoAmount <= 0)
            yield break;

        if (ph.pp.SessionScore <= 0)
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You have no points to spend on a tomato.");
            yield break;
        }


        CoroutineResult<PlayerHandler> coResult = new CoroutineResult<PlayerHandler>();
        yield return _gm.GetPlayerByUsername(targetUsername, coResult);
        PlayerHandler targetPlayer = coResult.Result;

        if (targetPlayer == null)
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Failed to find player with username: {targetUsername}");
            yield break;
        }

        if(targetPlayer.pb == null)
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Can't throw tomatos at a player who isn't spawned in.");
            yield break;
        }

        ph.ThrowTomato(desiredTomatoAmount, targetPlayer); 

    }
    private IEnumerator ProcessStatsCommand(string messageId, PlayerHandler ph, string msg)
    {
        PlayerHandler phToLookup = ph;

        int indxOfAtSymbol = msg.IndexOf('@');
        if (indxOfAtSymbol != -1 && msg.Length > indxOfAtSymbol + 1)
        {
            string username = msg.Substring(indxOfAtSymbol + 1);

            CoroutineResult<PlayerHandler> coResult = new CoroutineResult<PlayerHandler>();
            yield return _gm.GetPlayerByUsername(username, coResult);
            phToLookup = coResult.Result;
        }

        if (phToLookup == null)
        {
            ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Failed to find player in database. Correct command format is: !stats @username");
            yield break;
        }

        PlayerProfile pp = phToLookup.pp; 
        string statString = $"(@{phToLookup.pp.TwitchUsername}) [Gold: {pp.Gold:N0}] [Points: {pp.SessionScore:N0}] [Throne Captures: {pp.ThroneCaptures}] [Total Throne Time: {MyUtil.FormatDurationDHMS(pp.TimeOnThrone)}] [Players invited: {pp.GetInviteIds().Length}] [Tickets Spent: {pp.TotalTicketsSpent:N0}]";

        ReplyToPlayer(messageId, ph.pp.TwitchUsername, statString);

    }
    private void ProcessGameplayCommands(string messageId, PlayerHandler ph, string rawMsg, string rawEmotesRemoved)
    {
        if (_tileController.GameplayTile == null)
            return;

        //When you're eliminated, your ball's state will be set to idle, so you won't be able to continue making gameplay commands
        if (ph.GetState() == PlayerHandlerState.Gameplay || ph.GetState() == PlayerHandlerState.King)
        {
            _tileController.GameplayTile.ProcessGameplayCommand(messageId, this, ph, rawMsg, rawEmotesRemoved);
        }
    }

    private string RemoveTwitchEmotes(string rawMsg, List<Emote> emotes)
    {
        if(emotes == null || emotes.Count <= 0)
            return rawMsg;

        StringBuilder noEmotesSb = new StringBuilder();
        int currEmoteIndex = 0;
        var currEmote = emotes[currEmoteIndex];
        int highSurrogatesFound = 0;
        for (int i = 0; i < rawMsg.Length; i++)
        {

            // NOTE: This is necessary because twitch doesn't correctly count the startindex and endindex when emojis are mixed in
            // If the character is a high surrogate (first part of a surrogate pair), 
            // increment the index to skip the low surrogate (second part of the surrogate pair)
            if (char.IsHighSurrogate(rawMsg[i]))
            {
                Debug.Log($"Found high surrogate at index {i}");
                highSurrogatesFound++;
            }

            //If we're in the range of an emote, skip to the end
            if (currEmote.StartIndex + highSurrogatesFound <= i && i <= currEmote.EndIndex + highSurrogatesFound)
            {
                i = currEmote.EndIndex + highSurrogatesFound;

                currEmoteIndex++;
                if (currEmoteIndex < emotes.Count)
                    currEmote = emotes[currEmoteIndex];
            }
            else
            {
                //If we're not in an emote, just copy the character
                noEmotesSb.Append(rawMsg[i]);
            }
        }

        return noEmotesSb.ToString();
    }
}
