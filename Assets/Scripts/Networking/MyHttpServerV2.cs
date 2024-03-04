using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Web;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Collections;
using TMPro;

public class MyHttpServerV2 : MonoBehaviour
{
    [SerializeField] private AutoNgrokService _autoNgrokService;

    [SerializeField] private GameManager _gameManager;
    [SerializeField] private TwitchApi _twitchApi;
    [SerializeField] private SpotifyDJ _spotifyDJ;

    private HttpListener _listener;
    
    public void Start()
    {
        StartListener();
    }

    public void RestartListener()
    {
        StartCoroutine(CRestartListener()); 
    }

    //The port will stay listening while the ngrok tunnel is running. To restart the httpserver, I must shut off and restart the ngrok tunnel as well
    public IEnumerator CRestartListener()
    {
        _autoNgrokService.KillAllNgrokProcesses();
        yield return new WaitForSeconds(1);

        StopListener();
        // Introduce a brief delay to allow socket resources to release
        yield return new WaitForSeconds(1); 

        StartListener();
        yield return new WaitForSeconds(1);
        _autoNgrokService.StartNgrokTunnel();
    }

    public void StopListener()
    {
        if (_listener != null)
        {
            _listener.Stop();
            _listener.Close();
            _listener.Abort();

            _listener = null;  // Clear reference for re-creation
            Debug.Log("Stopping listener on " + AppConfig.inst.GetS("HostToListenOn") + ":" + AppConfig.inst.GetI("localHostPort").ToString() + "/");
        }
    }

    public void StartListener()
    {
        try
        {
            if (_listener == null)
            {
                _listener = new HttpListener();
                string listenerURL = AppConfig.inst.GetS("HostToListenOn") + ":" + AppConfig.inst.GetI("localHostPort").ToString() + "/";
                _listener.Prefixes.Add(listenerURL);
            }

            if (_listener.IsListening)
                return;

            _listener.Start();
            Debug.Log("Starting listening on: " + AppConfig.inst.GetS("HostToListenOn") + ":" + AppConfig.inst.GetI("localHostPort").ToString() +
                            "\n islistening: " + _listener.IsListening);
            Receive();
        } catch(Exception e)
        {
            Debug.LogException(e);
        }

    }


    private void Receive()
    {
        _listener.BeginGetContext(new AsyncCallback(ListenerCallback), _listener);
    }


    private async void ListenerCallback(IAsyncResult result)
    {
        if (!_listener.IsListening)
            return;

        try
        {
            var context = _listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            byte[] responseData = await GetResponseData(request);

            // write response
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = "text/html; charset=UTF-8";
            response.Headers.Add("Access-Control-Allow-Origin: *");
            response.Headers.Add("Access-Control-Allow-Methods: GET,POST");
            response.Headers.Add("ngrok-skip-browser-warning", "69420");
            response.OutputStream.Write(responseData, 0, responseData.Length);
            response.OutputStream.Close();
        }
        catch(Exception e)
        {
            Debug.LogError($"Exception caught in HttpServer Listener Callback: {e}");
        }

        Receive();
        
    }

    private async Task<byte[]> GetResponseData(HttpListenerRequest request)
    {

        byte[] responseData = new byte[0];

        if (request.HttpMethod == "OPTIONS" || request.RawUrl == "/favicon.ico")
        {
            return responseData; 
        }
        if (request.RawUrl == "/logs")
        {
            return Encoding.UTF8.GetBytes(
                $"<h1>Logs</h1>" +
                $"<pre>fake log contents here</pre>"
                );
        }

        string rawPostData = null;

        NameValueCollection parsedPostData = new NameValueCollection();

        // Tikfinity webhook content type: application/x-www-form-urlencoded
        // ChaosBot content type: application/x-www-form-urlencoded
        if (request.HttpMethod == "POST" && request.HasEntityBody)
        {
            var body = request.InputStream;
            var encoding = request.ContentEncoding;
            var reader = new StreamReader(body, encoding);
            rawPostData = reader.ReadToEnd();
            reader.Close();
            body.Close();
            Debug.Log("content type: " + request.ContentType + "rawPostData: " + rawPostData);

            parsedPostData = HttpUtility.ParseQueryString(rawPostData);
        }

        Debug.Log($"In ThreadId: {Thread.CurrentThread.ManagedThreadId} Process HttpRequest: URL=" + request.RawUrl + " Data=" + (rawPostData ?? ""));

        StringBuilder output = new StringBuilder();
        foreach (string key in parsedPostData.AllKeys)
        {
            output.AppendLine($"{key}: {parsedPostData[key]}");
        }
        Debug.Log(output.ToString());

        if (request.Url.LocalPath.Contains('@')) // Receive NGROK signal /TODO: Change this path to specify [/updatePlayerFromDB]
        {
            string username = request.Url.LocalPath.Substring(request.Url.LocalPath.IndexOf('@'));
            username = username.Replace("@", "");
            Debug.Log("Found test username: " + username);

            // If we made it all the way here, SUCCESS
            return Encoding.UTF8.GetBytes(
                        //   $"<h1>Success Clicking Referal Link. Going to Oauth now!</h1>" +
                        //   $"<p>The following usernames were found and linked to your Chaos League profile. </p> " +
                        //   $"<p>When you chat in the livestream with those accounts, your actions will still be linked to your Chaos League profile. </p>" +
                        $"<head>" +
                        $"<meta http-equiv=\"refresh\" content=\"0;URL={_twitchApi.GetOauthURLforInvite(username)}\">" +
                        $"</head>" //+
                                    //  $"<body>" +
                                    //    $"<h2>Redirecting...</h1>" +
                                    //    $"<p>You will be redirected to a different page in 4 seconds.</p>" +
                                    //  $"</body>"
                        );

        }
        if (request.Url.LocalPath == "/invited")
        {
            try
            {
                string code = request.QueryString.Get("code");
                string referrer = request.QueryString.Get("state");
                Debug.Log($"starting invite with code: [{code}] and referrer: [{referrer}]");
                Debug.Log("Here at Line 204");
                //If the name of the referrer was not passed through correctly, just redirect them
                if (string.IsNullOrEmpty(referrer))
                {
                    Debug.Log("Referrer name not found"); 
                    return Encoding.UTF8.GetBytes(

                            $"<head>" +
                            $"<p> Referrer name not found </p>" + 
                            $"<meta http-equiv=\"refresh\" content=\"0;URL=https://www.twitch.tv/{Secrets.CHANNEL_NAME}\">" +
                            $"</head>" //+

                        );
                }
                Debug.Log("Here at Line 218");
                var invitedUser = await TwitchApi.TradeAuthCodeForUser(code);
                Debug.Log("Here at Line 220");
                Debug.Log("invited user: " + invitedUser.Login);
                var referrerUser = await TwitchApi.GetUserByUsername(referrer);
                Debug.Log("Here at Line 223");
                if (referrerUser != null)
                {
                    Debug.Log("referrerUser user: " + referrerUser.Login);

                    //Need to pass this userID
                    UnityMainThreadDispatcher.Instance().Enqueue(() => StartCoroutine(_gameManager.HandleInviteSignal(invitedUser, referrerUser)));
                    // If we made it all the way here, SUCCESS
                    return Encoding.UTF8.GetBytes(

                                $"<head>" +
                                $"<meta http-equiv=\"refresh\" content=\"0;URL=https://www.twitch.tv/{Secrets.CHANNEL_NAME}\">" +
                                $"</head>" //+

                                );
                }
                Debug.Log("Here at Line 239");
                // Show quick tiny message that they failed to find the referring user, then redirect them to the stream
                return Encoding.UTF8.GetBytes(

                            $"<head>" +
                            $"<p>Failed to find referring user: [{referrer}]</p>" +
                            $"<meta http-equiv=\"refresh\" content=\"0;URL=https://www.twitch.tv/{Secrets.CHANNEL_NAME}\">" +
                            $"</head>" //+

                            );
                
            }
            
            catch (Exception e)
            {
                Debug.Log("Caught error in httpserver invite: " + e.Message);
            }

        }

        if(request.Url.LocalPath == "/authCallback")
        {
            if (!request.IsLocal)
                return UnauthorisedResponse();

            Debug.Log("Inside /authCallback"); 

            return Encoding.UTF8.GetBytes(@"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>OAuth Callback</title>
                    <script type='text/javascript'>
                        // JavaScript to extract token and send to /receiveToken
                        if (window.location.hash) {
                            let hash = window.location.hash.substring(1);
                            let params = new URLSearchParams(hash);
                            let accessToken = params.get('access_token');
                            let state = params.get('state');
                            window.location.href = 'http://localhost:3001/receiveBotAuthCode?access_token=' + accessToken + '&state=' + state;
                        }
                    </script>
                </head>
                <body>
                    Processing authentication...
                </body>
                </html>");
        }

        if (request.Url.LocalPath == "/receiveBotAuthCode")
        {
            if (!request.IsLocal)
                return UnauthorisedResponse();

            string twitchState = request.QueryString.Get("state");
            if (_twitchApi._state != twitchState)
                return UnauthorisedResponse();

            Debug.Log($"inside receivebotauthcode. request RawUrl: {request.RawUrl}");

            //How can I extract the access_token from the fragment in the request here?

            string code = request.QueryString.Get("code");
            string accessToken = request.QueryString.Get("access_token");
            Debug.Log($"accessToken in receivebotauthcode [{accessToken}]"); 
            //var tokenResponse = await _discordOauthHandler.TradeAuthCodeForTokenResponse(code);
            if(string.IsNullOrEmpty(accessToken))
                accessToken = await TwitchApi.TradeBOTAuthCodeForTokenResp(code);

            await UnityMainThreadDispatcher.Instance().EnqueueAsync(async () => await _twitchApi.InitializeTwitchConnections(accessToken));

            // If we made it all the way here, SUCCESS
            return Encoding.UTF8.GetBytes(
                        $"<h1>Retreived Access Token in Local Http Server. You can close this window.</h1>");

        }
        if (request.Url.LocalPath == "/spotifyToken") // Receive NGROK signal /TODO: Change this path to specify [/updatePlayerFromDB]
        {
            if (!request.IsLocal)
                return UnauthorisedResponse();

            string spotifyState = request.QueryString.Get("state");
            if (_spotifyDJ._state != spotifyState)
                return UnauthorisedResponse();

            string code = request.QueryString.Get("code");//parsedPostData["code"];
            Debug.Log($"Received spotify code: {code}. Now using this code to request access token");
            using (var client = new HttpClient())
            {
                string redirectUri = "http://localhost:3001/spotifyToken";

                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
                tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{AppConfig.inst.GetS("SpotifyClientID")}:{AppConfig.inst.GetS("SpotifyClientSecret")}")));
                tokenRequest.Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("redirect_uri", redirectUri)
                });

                var tokenResponseMsg = await client.SendAsync(tokenRequest);
                string tokenJson = await tokenResponseMsg.Content.ReadAsStringAsync();

                TokenResponse tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(tokenJson);

                await UnityMainThreadDispatcher.Instance().EnqueueAsync(async () => { await _spotifyDJ.ParseTokenResponse(tokenResponse); });
                return responseData;
            }

        }
        return responseData;

    }

    public byte[] UnauthorisedResponse()
    {
        return Encoding.UTF8.GetBytes(@"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>OAuth Callback</title>
                </head>
                <body>
                    Unauthorized.
                </body>
                </html>");
    }

    public void OnDestroy()
    {
        if (_listener != null)
        {
            _listener.Stop();
            Debug.Log("Stopping listener on " + AppConfig.inst.GetS("HostToListenOn") + ":" + AppConfig.inst.GetI("localHostPort").ToString() + "/");
        }
    }


}

