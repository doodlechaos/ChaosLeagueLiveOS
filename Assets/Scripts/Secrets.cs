using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static System.Net.WebRequestMethods;

public class Secrets
{
    public static string CHANNEL_NAME = "realjobexi";
    public static string CHANNEL_ID = "";

    public static string REDIRECT_URI_BOT_AUTH_PUBLIC = "http://localhost:3001/authCallback"; //"http://localhost:3001/receiveBotAuthCode";
    public static string REDIRECT_URI_BOT_AUTH_PRIVATE = "http://localhost:3001/receiveBotAuthCode"; 
    public static string REDIRECT_URI_INVITE = "http://localhost:3001/invited/";

    public static string TUNNEL_DOMAIN = "chaosleague-jobexi.ngrok.io"; 
    public static string CHAOS_LEAGUE_DOMAIN = "https://jobexileague.com";

}
