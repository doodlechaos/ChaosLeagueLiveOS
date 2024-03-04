using Newtonsoft.Json;
using SpotifyAPI.Web;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using TMPro;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomRewardRedemptionStatus;
using TwitchLib.Api.Helix.Models.Polls.CreatePoll;
using TwitchLib.Api.Helix.Models.Predictions;
using TwitchLib.Api.Helix.Models.Predictions.CreatePrediction;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
// If type or namespace TwitchLib could not be found. Make sure you add the latest TwitchLib.Unity.dll to your project folder
// Download it here: https://github.com/TwitchLib/TwitchLib.Unity/releases
// Or download the repository at https://github.com/TwitchLib/TwitchLib.Unity, build it, and copy the TwitchLib.Unity.dll from the output directory
using TwitchLib.Unity;

using UnityEngine;

public class TwitchApi : MonoBehaviour
{
    private static Api _api;

    [SerializeField] private TwitchClient _twitchClient;
    [SerializeField] private TwitchPubSub _twitchPubSub;
    [SerializeField] private GoldDistributor _liveViewCount;

    [SerializeField] private Gradient _customRewardBackgroundColors;
    [SerializeField] private Color _lavaRewardBackgroundColor;
    [SerializeField] private Color _waterRewardBackgroundColor;

    private CancellationTokenSource _cts;

    public static bool IsRefreshing = false; 

    private static string _refreshToken = "";
    public static DateTime _expirationTime { get; private set; }

    private CustomReward[] customRewards; //Not doing anything with this yet?

    [SerializeField] private bool testbutton; 

    [HideInInspector] public string _state {get; private set;}

    private void Awake()
    {
        // Create new instance of Api
        _api = new Api();

    }

    private void OnValidate()
    {
        if (testbutton)
        {
            testbutton = false;
            RefreshAccessTokenIn15s(); 
        }
    }


    private void Start()
    {
        GenerateRandomState();

        AskForBotAuthorization();

        _cts = new CancellationTokenSource();
        _ = AutoRefreshToken(_cts);
    }

    private void GenerateRandomState()
    {
        string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890";
        _state = new string(Enumerable.Repeat(chars, 64)
                .Select(s => s[UnityEngine.Random.Range(0, s.Length)]).ToArray());
    }

    public async Task AutoRefreshToken(CancellationTokenSource cts)
    {
        while (true)
        {
            await Task.Delay(500);

            if (cts.IsCancellationRequested)
                return;

            if (AppConfig.IsPublicBuild())
                return;

            if (_expirationTime == null)
                continue;

            if (_api == null)
                continue;

            if (string.IsNullOrEmpty(_api.Settings.AccessToken))
                continue;

            if (DateTime.Now >= _expirationTime)
            {
                IsRefreshing = true;
                await RefreshAccessToken();
                await InitializeTwitchConnections(_api.Settings.AccessToken);
                IsRefreshing = false;
                await Task.Delay(2000);
            }

        }
    }

    private async Task RefreshAccessToken()
    {
        try
        {

            Debug.Log("Starting API Access Token Refresh");
            var resp = await _api.Auth.RefreshAuthTokenAsync(_refreshToken, AppConfig.GetClientSecret());

            if (resp == null)
            {
                Debug.LogError("Token Refresh response is null");
                return;
            }

            ParseTokenRefreshResponse(resp);

        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
            return;
        }

    }

    public void RefreshAccessTokenIn15s()
    {
        Debug.Log($"accessToken: {_api.Settings.AccessToken} _refreshToken: {_refreshToken} _tokenExpireTimer {_expirationTime} ");
        _expirationTime = DateTime.Now.AddSeconds(15);
    }

    public static void ParseTokenRefreshResponse(RefreshResponse resp)
    {
        _expirationTime = DateTime.Now.AddSeconds(resp.ExpiresIn - 600); //Doing it 10 minutes early now
        _refreshToken = resp.RefreshToken;
        _api.Settings.AccessToken = resp.AccessToken;
        string[] scopes = resp.Scopes;

        string scopesString = "";
        foreach (var scope in scopes)
            scopesString += scope + "\n";
        Debug.Log($"Succesfully refreshed token {resp.AccessToken} with scopes: {scopesString}");
    }


    public void AskForBotAuthorization()
    {
        bool isPublic = AppConfig.IsPublicBuild(); 
        string responseType = "code";
        string redirectURI = Secrets.REDIRECT_URI_BOT_AUTH_PRIVATE; 
        if (isPublic)
        {
            responseType = "token";
            redirectURI = Secrets.REDIRECT_URI_BOT_AUTH_PUBLIC;
        }
        string encodedState = WebUtility.UrlEncode(_state);


        Debug.Log($"Asking for authorization with {responseType} response type"); 

        string scopes = "chat:read chat:edit channel:moderate channel:read:subscriptions whispers:read whispers:edit moderation:read channel:read:redemptions channel:manage:redemptions channel:read:goals moderator:read:chat_settings " +
            "channel:manage:raids moderator:manage:announcements moderator:manage:chat_messages user:manage:chat_color channel:read:vips user:manage:whispers bits:read user:edit channel:read:hype_train channel:manage:polls " +
            "channel:manage:predictions channel:read:polls channel:read:predictions";
        string BotAuthURL = $"https://id.twitch.tv/oauth2/authorize?client_id={AppConfig.GetClientID()}&redirect_uri={redirectURI}&response_type={responseType}&scope={scopes}&state={encodedState}\r\n";

        Debug.Log($"Asking for bot authorization from url: {BotAuthURL}");
        Application.OpenURL(BotAuthURL);
    }

    public string GetOauthURLforInvite(string username)
    {
        string redirect_uri = $"https://{Secrets.TUNNEL_DOMAIN}/invited";
        string authURL = $"https://id.twitch.tv/oauth2/authorize?response_type=code&client_id={AppConfig.GetClientID()}&redirect_uri={redirect_uri}&state={username}";
        Debug.Log("OauthURL: " + authURL);
        return authURL;
    }

    public async static Task<string> TradeBOTAuthCodeForTokenResp(string code)
    {
        var resp = await _api.Auth.GetAccessTokenFromCodeAsync(code, AppConfig.GetClientSecret(), Secrets.REDIRECT_URI_BOT_AUTH_PUBLIC, clientId: AppConfig.GetClientID());
        SetApiAccess(resp.AccessToken);
        _refreshToken = resp.RefreshToken;
        _expirationTime = DateTime.Now.AddSeconds(resp.ExpiresIn - 60);

        return resp.AccessToken;
    }

    public static void SetApiAccess(string accessToken)
    {
        _api.Settings.AccessToken = accessToken;
        _api.Settings.ClientId = AppConfig.GetClientID();
    }

    public static async Task<User> TradeAuthCodeForUser(string code)
    {
        var httpClient = new HttpClient();
        string redirectURI = "http://localhost:3001/receiveBotAuthCode";

        var request = new HttpRequestMessage(HttpMethod.Post, "https://id.twitch.tv/oauth2/token");
        request.Content = new StringContent($"client_id={AppConfig.GetClientID()}&client_secret={AppConfig.GetClientSecret()}&code={code}&grant_type=authorization_code&redirect_uri={redirectURI}", Encoding.UTF8, "application/x-www-form-urlencoded");

        try
        {
            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();

            Debug.Log("Response body: " + responseBody);

            TwitchTokenResponse twitchTokenResponse = JsonConvert.DeserializeObject<TwitchTokenResponse>(responseBody);

            if (twitchTokenResponse == null)
            {
                Debug.Log("ERROR: twitch token response is null when trying to trade auth code for user in TwitchAPI");
                return null;
            }

            return await GetUserByToken(twitchTokenResponse.AccessToken);


        }
        catch (Exception e)
        {
            Debug.Log($"ERROR. Caught exception in TwitchAPI. Trying to trade invite auth code {code} for user. Exception: {e}");
            return null;
        }
    }

    public async Task InitializeTwitchConnections(string accessToken)
    {
        SetApiAccess(accessToken); 

        User user = await GetBroadcaster();

        if (user == null)
        {
            Debug.LogError("Failed to find user from access token.");
            return;
        }

        Secrets.CHANNEL_NAME = user.DisplayName;
        Secrets.CHANNEL_ID = user.Id;
        //Debug.Log($"Found Channel Name: {Secrets.CHANNEL_NAME}  Found Channel ID: {Secrets.CHANNEL_ID}");

        _twitchClient.Init(Secrets.CHANNEL_NAME, accessToken);

        _twitchPubSub.Init(Secrets.CHANNEL_ID, accessToken);

        await CreateCustomPointRewards();
        await LoadPointRewards(); 
        Debug.Log("Done Initializing All Twitch Connections");
    }

    public async Task CreateCustomPointRewards()
    {
        //var resp = await _api.Auth.ValidateAccessTokenAsync();
       // Debug.Log("validation resp: " + resp.UserId);

        CreateCustomRewardsRequest request;

        int[] costs = new int[] { 1, 2, 5, 10, 25, 50, 100, 200, 500, 1000, 2500, 5000, 10_000, 20_000, 50_000, 100_000 }; 
        for(int i = 0; i < costs.Length; i++)
        {
            int cost = costs[i];

            float t = i / (float)costs.Length;

            Color backgroundColor = _customRewardBackgroundColors.Evaluate(t);
            string colorString = MyUtil.ColorToHexString(backgroundColor);
            Debug.Log($"BackgroundColor: {colorString}");
            request = new CreateCustomRewardsRequest()
            {
                Title = $"Bid {cost} Spawn Ticket{((cost == 1) ? "" : 's')}",
                Cost = cost,
                Prompt = "Top bidders are guaranteed to spawn! Remaining bids are entered into a raffle. You earn free tickets by watching the stream.",
                IsEnabled = true,
                BackgroundColor = colorString,
                IsUserInputRequired = false,
                IsMaxPerStreamEnabled = false,
                IsGlobalCooldownEnabled = false,
            };
            try
            {
                var response = await _api.Helix.ChannelPoints.CreateCustomRewardsAsync(Secrets.CHANNEL_ID, request);
            }
            catch (Exception ex)
            {
                Debug.Log("Error while creating reward. (Likely because it has already been created)\n" + ex.Message);
            }

        }

        //Create custom reward to activate throne lava bucket
        request = new CreateCustomRewardsRequest()
        {
            Title = $"Activate Lava on Throne Tile",
            Cost = AppConfig.inst.GetI("ThroneLavaCost") * 3, //3 times as expensive as bits
            Prompt = $"Mimicks adding {AppConfig.inst.GetI("ThroneLavaCost")} bit cheer to the !lava trigger, for free!",
            IsEnabled = true,
            BackgroundColor = MyUtil.ColorToHexString(_lavaRewardBackgroundColor),
            IsUserInputRequired = false,
            IsMaxPerStreamEnabled = false,
            IsGlobalCooldownEnabled = false,
        };
        try
        {
            var response = await _api.Helix.ChannelPoints.CreateCustomRewardsAsync(Secrets.CHANNEL_ID, request);
            Debug.Log("Created custom lava reward"); 
        }
        catch (Exception ex)
        {
            Debug.Log("Error while creating lava custom reward.\n" + ex.Message);
        }

        //Create custom reward to activate throne lava bucket
        request = new CreateCustomRewardsRequest()
        {
            Title = $"Activate Water on Throne Tile",
            Cost = AppConfig.inst.GetI("ThroneWaterCost") * 3, //3 times as expensive as bits
            Prompt = $"Mimicks adding {AppConfig.inst.GetI("ThroneWaterCost")} bit cheer to the !water trigger, for free!",
            IsEnabled = true,
            BackgroundColor = MyUtil.ColorToHexString(_waterRewardBackgroundColor),
            IsUserInputRequired = false,
            IsMaxPerStreamEnabled = false,
            IsGlobalCooldownEnabled = false,
        };
        try
        {
            var response = await _api.Helix.ChannelPoints.CreateCustomRewardsAsync(Secrets.CHANNEL_ID, request);
            Debug.Log("Created custom water reward");
        }
        catch (Exception ex)
        {
            Debug.Log("Error while creating water custom reward.\n" + ex.Message);
        }

        Debug.Log("Done creating custom rewards"); 
    }

    public async Task LoadPointRewards()
    {
        try
        {
            var response = await _api.Helix.ChannelPoints.GetCustomRewardAsync(Secrets.CHANNEL_ID);
            customRewards = response.Data;

            Debug.Log($"Successfully loaded {customRewards.Length} custom rewards into global list."); 
        }
        catch (Exception ex)
        {
            Debug.Log("Error loading custom rewards.\n" + ex.Message);
        }
    }
    public static async Task<User> GetBroadcaster()
    {

        var response = await _api.Helix.Users.GetUsersAsync();

        if (response.Users.Length <= 0)
        {
            Debug.LogError("Failed to find user");
            return null;
        }

        Debug.Log("Found user with id: " + response.Users[0].Id);

        return response.Users[0];
    }
    public static async Task<User> GetUserByUsername(string username)
    {
        await WaitIfRefreshingAccessToken();

        if (string.IsNullOrEmpty(username))
        {
            Debug.Log($"Failed to find user: [{username}] because username is null or empty. Returning null.");
            return null; 
        }

        try
        {
            var response = await _api.Helix.Users.GetUsersAsync(logins: new List<string> { username });
            if (response.Users.Length <= 0)
            {
                Debug.Log($"Failed to find user: [{username}]. Returning null.");
                return null;
            }

            Debug.Log("Found user with id: " + response.Users[0].Id);

            return response.Users[0];
        }
        catch(Exception ex)
        {
            Debug.Log($"Failed to find user: [{username}] due to exception in API call. Returning null. Excaption: {ex}");
            return null;
        }



    }

    public static async Task<User> GetUserById(string twitchId)
    {
        await WaitIfRefreshingAccessToken();

        var response = await _api.Helix.Users.GetUsersAsync(ids: new List<string> { twitchId });

        if (response.Users.Length <= 0)
        {
            Debug.LogError($"Failed to find user from twitchId: {twitchId}. Returning null.");
            return null;
        }

        Debug.Log("Found user with id: " + response.Users[0].Id);

        return response.Users[0];
    }


    public async Task<TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream> GetStream()
    {
        await WaitIfRefreshingAccessToken();

        var response = await _api.Helix.Streams.GetStreamsAsync(userIds: new List<string> { Secrets.CHANNEL_ID});
        if (response.Streams.Length <= 0)
        {
            Debug.Log("Failed to find stream");
            return null;
        }
        return response.Streams[0]; 
    }

    public static async Task<User> GetUserByToken(string accessToken)
    {
        await WaitIfRefreshingAccessToken();

        var response = await _api.Helix.Users.GetUsersAsync(accessToken:accessToken);
        if (response.Users.Length <= 0)
        {
            Debug.LogError($"Failed to find user from token {accessToken}");
            return null;
        }
        return response.Users[0];
    }

    public static async Task RejectRewardRedemption(string rewardID, List<string> redemptionIDs)
    {
        await WaitIfRefreshingAccessToken();

        try
        {
            var canceled = new UpdateCustomRewardRedemptionStatusRequest() { Status = TwitchLib.Api.Core.Enums.CustomRewardRedemptionStatus.CANCELED };

            Debug.Log($"Calling UpdateRedemptionStatusAsync: {Secrets.CHANNEL_ID} {rewardID} {redemptionIDs[0]} {canceled} \nclientid: {_api.Settings.ClientId} \naccesstoken: {_api.Settings.AccessToken}"); 
            var response = await _api.Helix.ChannelPoints.UpdateRedemptionStatusAsync(broadcasterId:Secrets.CHANNEL_ID, rewardId:rewardID, redemptionIds:redemptionIDs, request:canceled);
            Debug.Log("reject reward response: " + response.Data.ToString());
        }
        catch (Exception ex)
        {
            Debug.LogError("Failled to reject reward. \n " + ex.Message);
        }

    }

    public static async Task StartPoll(string title, List<Choice> choices, int durationSeconds)
    {
        await WaitIfRefreshingAccessToken();

        CreatePollRequest pollRequest = new CreatePollRequest()
        {
            BroadcasterId = Secrets.CHANNEL_ID, //"493342634", //TODO: Replace with Secrets.ChannelID
            Title = title,
            Choices = choices.ToArray(),
            DurationSeconds = durationSeconds
        };

        await _api.Helix.Polls.CreatePollAsync(pollRequest);
    }

    public static async Task<TwitchLib.Api.Helix.Models.Polls.Choice[]> GetPollResults()
    {
        await WaitIfRefreshingAccessToken();

        var results = await _api.Helix.Polls.GetPollsAsync(Secrets.CHANNEL_ID);
        if (results.Data == null || results.Data.Length <= 0)
        {
            Debug.LogError("Failed to get poll results");
            return null;
        }

        return results.Data.First().Choices; 
    }

    public static async Task<Prediction> StartPrediction(PredictionObj predictionObj)
    {
        await WaitIfRefreshingAccessToken();

        try
        {
            CreatePredictionRequest request = new CreatePredictionRequest()
            {
                BroadcasterId = Secrets.CHANNEL_ID, 
                Title = predictionObj.Title.TruncateString(45),
                Outcomes = predictionObj.GetOutcomes(),
                PredictionWindowSeconds = predictionObj.PredictionWindowSec
            };

            Debug.Log($"Starting prediction request: {request.BroadcasterId} {request.Title} {request.Outcomes[0].Title} {request.Outcomes[1].Title} {request.PredictionWindowSeconds}"); 

            var predictionResponse = await _api.Helix.Predictions.CreatePredictionAsync(request);

            if(predictionResponse == null || predictionResponse.Data.Length <= 0)
            {
                Debug.LogError("Failed to create prediction");
                return null;
            }

            Prediction prediction = predictionResponse.Data[0];
            return prediction; 

        }
        catch(Exception e)
        {

            Debug.LogError(e);
            await CancelAllPredictions();

            return null;
        }
    }

    public static async Task FinishPrediction(string predictionID, string winningOutcomeID)
    {
        await WaitIfRefreshingAccessToken();

        await _api.Helix.Predictions.EndPredictionAsync(Secrets.CHANNEL_ID, predictionID, TwitchLib.Api.Core.Enums.PredictionEndStatus.RESOLVED, winningOutcomeID); 
    }

    public static async Task CancelPrediction(string predictionID)
    {
        await WaitIfRefreshingAccessToken();

        await _api.Helix.Predictions.EndPredictionAsync(Secrets.CHANNEL_ID, predictionID, TwitchLib.Api.Core.Enums.PredictionEndStatus.CANCELED); 
    }

    public static async Task CancelAllPredictions()
    {
        await WaitIfRefreshingAccessToken(); 

        var predictions = await _api.Helix.Predictions.GetPredictionsAsync(Secrets.CHANNEL_ID);
        foreach(var prediction in predictions.Data)
        {
            if (prediction.Status != TwitchLib.Api.Core.Enums.PredictionStatus.RESOLVED)
                await CancelPrediction(prediction.Id); 
        }
    }

    public static async Task WaitIfRefreshingAccessToken()
    {
        while (IsRefreshing)
        {
            // Wait for a short time before checking the flag again to avoid tight looping
            Debug.Log("Waiting for refreshing."); 
            await Task.Delay(100);
        }
    }

    [Serializable]
    public class PollResponseData
    {
        public string id;
        public string broadcaster_id;
        public string broadcaster_name;
        public string broadcaster_login;
        public string title;

        public TwitchLib.Api.Helix.Models.Polls.Choice[] choices;

        public string status;
        public string started_at;
        public string ended_at;
    }

}
