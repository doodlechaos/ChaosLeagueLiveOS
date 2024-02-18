using UnityEngine;
using System;
using Amazon.Polly;
using Amazon.Runtime;
using Amazon;
using System.IO;
using Amazon.Polly.Model;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using TwitchLib.PubSub.Enums;

public class MyTTS : MonoBehaviour
{
    public AudioSource audioSource;

    [SerializeField] private AudioSource _lowPitchAudioSource;
    [SerializeField] private AudioSource _regularPitchAudioSource;
    [SerializeField] private AudioSource _highPitchAudioSource;

    public static MyTTS inst;

    [SerializeField] private int max_TTS_string_length = 400;


    [SerializeField] private string textToSpeak;
    [SerializeField] private bool speakButton;
    [SerializeField] private bool testUsePolly;
    [SerializeField] private string voiceId = "Joey";
    [SerializeField] private AudioPitch audioPitch;

    private Queue<(AudioClip clip, float pitch)> audioQ = new Queue<(AudioClip clip, float pitch)>();

    private AmazonPollyClient client;

    private List<Voice> _voiceOptions;

    private int audioFileCycler = 0;

    public enum AudioPitch { Low, Reg, High };

    private StringBuilder _sb = new StringBuilder();

    private Dictionary<string, SubGifter> _giftedSubs = new Dictionary<string, SubGifter>();
    [SerializeField] private float _aggregateGiftsDuration = 2f;

    public void Start()
    {
        inst = this;

        
        if (!string.IsNullOrEmpty(AppConfig.inst.GetS("AWS_ACCESS_KEY")))
        {
            string accessKey = AppConfig.inst.GetS("AWS_ACCESS_KEY");
            string secretKey = AppConfig.inst.GetS("AWS_SECRET_KEY");
            var credentials = new BasicAWSCredentials(accessKey, secretKey);

            client = new AmazonPollyClient(credentials, RegionEndpoint.USEast2);

            _ = GetVoiceOptions();
        }

        _lowPitchAudioSource.pitch = 0.5f;
        _regularPitchAudioSource.pitch = 1;
        _highPitchAudioSource.pitch = 1.5f; 
    }

    private async Task GetVoiceOptions()
    {
        var response = await client.DescribeVoicesAsync(new DescribeVoicesRequest() { Engine = Engine.Standard });

        if(response.HttpStatusCode != System.Net.HttpStatusCode.OK)
        {
            Debug.LogError("FAILED TO FETCH VOICE OPTIONS: httpstatuscode: " + response.ToString());
            return;
        }

        CLDebug.Inst.Log($"Successfully fetched {response.Voices.Count} voice options.");


        _voiceOptions = response.Voices;
    }

    private void OnValidate()
    {
        if (speakButton)
        {
            speakButton = false;
            if (testUsePolly)
                SpeechMaster(textToSpeak, VoiceId.FindValue(voiceId), audioPitch, false);
        }
    }

    private void Update()
    {
        if(_giftedSubs.Count > 0)
        {
            string[] keys = _giftedSubs.Keys.ToArray();
            foreach(string key in keys)
            {
                SubGifter gifter = _giftedSubs[key];
                gifter.timer -= Time.deltaTime;
                _giftedSubs[key] = gifter;

                if(gifter.timer <= 0)
                {
                    _giftedSubs.Remove(key);
                    Announce($"{gifter.username} gifted {gifter.count} {gifter.tier} sub{((gifter.count > 1) ? "s" : "")}. What a bro.");
                }
            }
        }

        if (audioQ.Count <= 0)
            return;

        if (audioSource.isPlaying)
            return;

        var tts = audioQ.Dequeue();
        audioSource.pitch = tts.pitch;
        audioSource.clip = tts.clip;
        audioSource.Play(); 
    }

    public void PlayerSpeech(string textToSpeak, VoiceId voiceId)
    {
        SpeechMaster(textToSpeak, voiceId, AudioPitch.Reg, addToQ:false);
    }

    public void Announce(string textToSpeak)
    {
        SpeechMaster(textToSpeak, VoiceId.Brian, AudioPitch.Reg, addToQ:true);
    }

    public void AggregateSubGift(string gifterUsername, int multiMonthDuration, SubscriptionPlan tier)
    {
        SubGifter sg;
        if(!_giftedSubs.TryGetValue(gifterUsername, out sg))
            sg = new SubGifter() { username = gifterUsername, count = 0, multimonthduration = multiMonthDuration, tier = tier};
        
        sg.count++;
        sg.timer = _aggregateGiftsDuration;
        _giftedSubs[gifterUsername] = sg;

    }

    public void SpeechMaster(string textToSpeak, VoiceId voiceID, AudioPitch pitch, bool addToQ)
    {
        if(textToSpeak.Length > max_TTS_string_length)
        {
            CLDebug.Inst.Log($"String length {textToSpeak.Length} is greater than max allowed {max_TTS_string_length}. Skipping TTS in speech Master.");
            return;
        }

        if (!string.IsNullOrEmpty(AppConfig.inst.GetS("AWS_ACCESS_KEY")))
            _ = SpeechPolly(textToSpeak, voiceID, pitch, addToQ);
        else
            SpeechLocal(textToSpeak, voiceID, pitch, addToQ);
    }

    private async Task SpeechPolly(string textToSpeak, VoiceId voiceId, AudioPitch pitch, bool addToQ)
    {
        try
        {
            var request = new SynthesizeSpeechRequest()
            {
                Text = textToSpeak, //$"<speak> <prosody pitch=\"{pitch}%\"> <amazon:effect vocal-tract-length=\"{vocal_tract_length_percent}%\">{textToSpeak} </amazon:effect> </prosody> </speak>",
                Engine = Engine.Standard,
                VoiceId = VoiceId.FindValue(voiceId),
                OutputFormat = OutputFormat.Mp3,
                TextType = TextType.Text //TextType.Ssml
            };

            var response = await client.SynthesizeSpeechAsync(request);

            audioFileCycler = (audioFileCycler + 1) % 10;
            string _polyAudioFilePath = $"{Application.persistentDataPath}/audio{audioFileCycler}.mp3";

            Debug.Log("_polyAudioFilepath: " + _polyAudioFilePath);
            WriteIntoFile(response.AudioStream, _polyAudioFilePath);


            using (var www = UnityWebRequestMultimedia.GetAudioClip(_polyAudioFilePath, AudioType.MPEG))
            {
                var result = www.SendWebRequest();

                while (!result.isDone) await Task.Yield();
                   
                if(www.result == UnityWebRequest.Result.ConnectionError || www.responseCode != 200)
                {
                    www.Dispose();
                    Debug.Log("error Inside of MyTTS unity web request multimedia. Cancelling SpeechPolly"); 
                    return;
                }

                var clip = DownloadHandlerAudioClip.GetContent(www);

                if(clip.length > 30)
                {
                    CLDebug.Inst.Log("AWS Polly audio clip over 30 seconds long. Not playing on audio source to avoid annoyance. Length: " + clip.length);
                    return;
                }

                float pitchVal = 1;
                if (pitch == AudioPitch.Low)
                    pitchVal = 0.5f;
                else if (pitch == AudioPitch.Reg)
                    pitchVal = 1f;
                else if (pitch == AudioPitch.High)
                    pitchVal = 1.5f;

                if (addToQ)
                    audioQ.Enqueue((clip, pitchVal));
                else
                {
                    if (pitch == AudioPitch.Low)
                        _lowPitchAudioSource.PlayOneShot(clip); 
                    else if(pitch == AudioPitch.Reg)
                        _regularPitchAudioSource.PlayOneShot(clip);
                    else
                        _highPitchAudioSource.PlayOneShot(clip);
                }
            }

        }
        catch (Exception e)
        {
            CLDebug.Inst.LogError("found error in MyTTS: " + e);
        }
    }

    private void SpeechLocal(string textToSpeak, VoiceId voiceId, AudioPitch pitch, bool addToQ)
    {
        float pitchVal = 1;
        if (pitch == AudioPitch.Low)
            pitchVal = 0.5f;
        else if (pitch == AudioPitch.Reg)
            pitchVal = 1f;
        else if (pitch == AudioPitch.High)
            pitchVal = 1.5f;

        //Speaker.Instance.Speak(textToSpeak, pitch: pitchVal); //TODO, need open source local TTS
        Debug.Log("TODO: Need local open source TTS");
    }

    private void WriteIntoFile(Stream stream, string filePath)
    {


        using (var fileStream = new FileStream(path: filePath, FileMode.Create))
        {
            byte[] buffer = new byte[8 * 1024];
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                fileStream.Write(buffer, 0, bytesRead);
            }
        }
    }

}

public struct SubGifter
{
    public float timer; 
    public string username;
    public int count;
    public int multimonthduration;
    public SubscriptionPlan tier; 
}