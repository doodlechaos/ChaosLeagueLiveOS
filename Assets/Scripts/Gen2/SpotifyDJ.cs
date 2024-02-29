using System.Collections.Generic;
using UnityEngine;
using SpotifyAPI.Web;
using System.Net;
using System.Threading.Tasks;
using TMPro;
using System.Text;
using System;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using System.Collections;
using Unity.VisualScripting;
using System.Threading;

public class SpotifyDJ : MonoBehaviour
{
    private SpotifyClient _spotifyClient;
    [SerializeField] private TwitchClient _twitchClient;

    private HashSet<string> _safeSongIds = new HashSet<string>();

    [SerializeField] private TextMeshProUGUI _spotifyConnectionStatus;

    [SerializeField] private bool _testAuthorize;

    [SerializeField] private string _testInput;
    [SerializeField] private bool _testRefreshTokenIn15Sec;
    [SerializeField] private TextMeshPro _songDisplayText;

    public string RefreshToken;
    public string AccessToken;
    public int AccessTokenExpiresIn;
    public DateTime _expirationTime { get; private set; } 

    private string latestTrackId = string.Empty;

    private CancellationTokenSource _cts;

    private bool _isRefreshing = false;

    private Coroutine _animateTextCoroutine; 

    [HideInInspector] public string _state {get; private set;}

    private void Start()
    {
        GenerateRandomState();
        _cts = new CancellationTokenSource();
        _expirationTime = DateTime.Now.AddHours(9999); //Temp until the token parse sets this value
        _songDisplayText.color = _songDisplayText.color.WithAlpha(0); 

        _ = PollCurrentSongNameV2(_cts);
        _ = AutoRefreshToken();
    }

    private void OnValidate()
    {
        if (_testAuthorize)
        {
            _testAuthorize = false;
            GetAuthToken();
        }

        if (_testRefreshTokenIn15Sec)
        {
            _testRefreshTokenIn15Sec = false;
            _expirationTime = DateTime.Now.AddSeconds(15);

        }
    }

    private async Task AutoRefreshToken()
    {
        while (true)
        {
            try
            {
                await Task.Delay(500);

                if (string.IsNullOrEmpty(AccessToken))
                    continue;

                if (string.IsNullOrEmpty(RefreshToken))
                    continue;

                if (_expirationTime == null)
                    continue;

                if (DateTime.Now >= _expirationTime)
                {
                    await RefreshAccessToken();
                }
            } catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }
    }

    private async Task PollCurrentSongNameV2(CancellationTokenSource cts)
    {
        while (true)
        {
            try
            {
                await Task.Delay(5000);

                if (cts.IsCancellationRequested)
                    return;

                if (_isRefreshing)
                    continue;

                //Don't poll if we're less than 10 seconds away from the next refresh
                if (DateTime.Now >= _expirationTime.AddSeconds(-10))
                    continue;

                if (AccessToken == null || AccessToken == "" || _spotifyClient == null)
                    continue;

                var req = new PlayerCurrentPlaybackRequest(PlayerCurrentPlaybackRequest.AdditionalTypes.Track);

                var currPlayback = await _spotifyClient.Player.GetCurrentPlayback(req);

                if (currPlayback != null && currPlayback.IsPlaying &&
                    currPlayback.Item is FullTrack track && latestTrackId != track.Id)
                {
                    _songDisplayText.SetText($"!song: {track.Artists[0].Name} - {track.Name}");
                    latestTrackId = track.Id;
                    if(_animateTextCoroutine != null)
                        StopCoroutine(_animateTextCoroutine);
                    _animateTextCoroutine = StartCoroutine(DisplayNewSongTextAnimation(1, 5, 5));
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Exception caught in poll current song name: " + ex.Message);
            }

        }
    }

    public void GetAuthToken()
    {
        string clientID = AppConfig.inst.GetS("SpotifyClientID");
        string redirectUri = "http://localhost:3001/spotifyToken"; //$"{AppConfig.inst.GetS("HostToListenOn")}:{AppConfig.inst.GetS("localHostPort")}{AppConfig.inst.GetS("SpotifyRedirectPath")}";
        Debug.Log("redirectUri: " + redirectUri);
        string encodedRedirectUri = WebUtility.UrlEncode(redirectUri);
        string encodedState = WebUtility.UrlEncode(_state);

        var authorizationRequest = $"https://accounts.spotify.com/authorize?response_type=code&client_id={clientID}&redirect_uri={encodedRedirectUri}&scope=app-remote-control user-modify-playback-state user-read-currently-playing user-read-playback-state&state={encodedState}";
        Debug.Log("authorization Request: " + authorizationRequest);
        Application.OpenURL(authorizationRequest);
    }

    private async Task RefreshAccessToken()
    {
        Debug.Log($"Starting to refresh access token");
        _isRefreshing = true; 
        try
        {
            using var client = new HttpClient();

            var requestBody = new FormUrlEncodedContent(new[]
            {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", RefreshToken),
            new KeyValuePair<string, string>("client_id", AppConfig.inst.GetS("SpotifyClientID")),
            new KeyValuePair<string, string>("client_secret", AppConfig.inst.GetS("SpotifyClientSecret")),
        });

            var response = await client.PostAsync("https://accounts.spotify.com/api/token", requestBody);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            TokenResponse tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseBody);

            await ParseTokenResponse(tokenResponse);

        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    public async Task ParseTokenResponse(TokenResponse tokenResponse)
    {
        AccessToken = tokenResponse.access_token;
        if (tokenResponse.refresh_token != null && tokenResponse.refresh_token.Length > 0)
            RefreshToken = tokenResponse.refresh_token;

        AccessTokenExpiresIn = tokenResponse.expires_in - 120; //Do 60 seconds early as cushion for 
        _expirationTime = DateTime.Now.AddSeconds(tokenResponse.expires_in - 120); 

        Debug.Log($"Spotify Access Token: {AccessToken}\nRefresh Token: {RefreshToken} \nExpires In: {AccessTokenExpiresIn}");
        await InitSpotifyClient();
    }

    public async Task InitSpotifyClient()
    {
        _spotifyClient = new SpotifyClient(AccessToken);

        if (_safeSongIds.Count <= 0)
        {
            _safeSongIds = await GetTrackIsrcsFromPlaylistAsync(AppConfig.inst.GetS("SpotifySafePlaylistURL"));

            _spotifyConnectionStatus.SetText($"Finished Initializing Spotify Client. Safe song Ids found: {_safeSongIds.Count}");
        }
    }

    public async Task<HashSet<string>> GetTrackIsrcsFromPlaylistAsync(string playlistURL)
    {
        HashSet<string> isrcSet = new HashSet<string>();

        string[] split = playlistURL.Split("/playlist/");

        if(split.Length < 2)
        {
            Debug.LogError("Failed to get playlist id from playlist url. Correct form is https://open.spotify.com/playlist/5gdz9X9y9hpBOCjYo6TI31");
            return null;
        }

        string playlistId = split[1]; 

        var getPlaylist = await _spotifyClient.Playlists.GetItems(playlistId);

        int count = 0;

        await foreach (var item in _spotifyClient.Paginate(getPlaylist))
        {
            var track = item.Track as FullTrack;

            count++;
            _spotifyConnectionStatus.SetText($"{count} item name: {track.Name} id: {track.Id}");

            isrcSet.Add(track.ExternalIds["isrc"]);
        }
        return isrcSet;
    }

    public async Task SearchAndPlay(string messageId, string query, PlayerHandler ph)
    {
        if (_spotifyClient == null)
        {
            _twitchClient.ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Admin has not connected spotify client.");
            return;
        }
        //Debug.Log("attempting to search and play");

        query = query.Replace("by", "");
        query = query.Replace("By", "");

        var searchRequest = new SearchRequest(SearchRequest.Types.Track, query);
        var searchResponse = await _spotifyClient.Search.Item(searchRequest);

        for (int i = 0; i < searchResponse.Tracks.Items.Count && i < 50; i++)
        {
            var track = searchResponse.Tracks.Items[i];
            if (!_safeSongIds.Contains(track.ExternalIds["isrc"]))
                continue;

            //Debug.Log($"Found matching id: {track.ExternalIds["isrc"]}");
            await PlaySongAsync(track.Uri);
            StringBuilder artistSb = new StringBuilder();
            foreach (var artist in track.Artists)
            {
                artistSb.Append(artist.Name);
                artistSb.Append(", ");
            }
            //MyTTS.inst.Announce($"{ph.pp.TwitchUsername} changed the song to {track.Name} by {artistSb}");
            _twitchClient.ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"As ruler of the throne, you changed the song to {track.Name} by {artistSb}");
            return;
        }
        _twitchClient.ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"Unable to find song. See valid options in this playlist: {AppConfig.inst.GetS("SpotifySafePlaylistURL")}");
    }

    private async Task PlaySongAsync(string trackUri)
    {
        var request = new PlayerAddToQueueRequest(trackUri);
        await _spotifyClient.Player.AddToQueue(request);
        await _spotifyClient.Player.SkipNext();
    }

    public async Task SkipSong()
    {
        await _spotifyClient.Player.SkipNext();
    }


    IEnumerator DisplayNewSongTextAnimation(float spinDuration, float holdDuration, float fadeDuration)
    {
        _songDisplayText.color = _songDisplayText.color.WithAlpha(1); 

        float timer = 0;
        while (timer < spinDuration)
        {
            float t = timer / spinDuration;
            float rot = Mathf.Lerp(0, 360, t);
            _songDisplayText.transform.eulerAngles = new Vector3(rot, 0, 0);
            timer += Time.deltaTime;

            yield return null;
        }

        yield return new WaitForSeconds(holdDuration);

        timer = 0;
        while(timer < fadeDuration)
        {
            Color col = _songDisplayText.color;
            _songDisplayText.color = col.WithAlpha(Mathf.Lerp(1, 0.05f, timer / fadeDuration));

            timer += Time.deltaTime;
            yield return null;
        }

    }

    private void OnApplicationQuit()
    {
        _cts?.Cancel(); 
    }

    private void GenerateRandomState()
    {
        string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890";
        _state = new string(Enumerable.Repeat(chars, 64)
                .Select(s => s[UnityEngine.Random.Range(0, s.Length)]).ToArray());
    }
}

[Serializable]
public class TokenResponse
{
    public string access_token { get; set; }

    public string token_type { get; set; }

    public int expires_in { get; set; }

    public string scope { get; set; }

    public string refresh_token { get; set; }

}
