using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SettingsOverlay : MonoBehaviour
{
    [SerializeField] private GameManager _gm;
    [SerializeField] private SpotifyDJ _spotifyDJ; 
    public MenuBar menuBar;
    public ScrollRect emptyScrollView;
    public GameObject emptyToggleEntry;
    public GameObject emptyInputFieldEntry;
    [SerializeField] private GameObject _originRoot;

    public Button MenuButtonOrigin;

    public Transform AudioCausesRoot;
    public Transform AudioScrollViewContentRoot;
    public Transform PagesRoot; 

    public GameObject MasterVolumeSlider;

    [Space(50)]
    [Header("App Config Listener GameObject Links")]
    //Listener GameObject Links
    public MyTTS enableKingTTS;

    [SerializeField] private TextMeshProUGUI _twitchTokenExpireTimerText;
    [SerializeField] private TextMeshProUGUI _spotifyTokenExpireTimerText;
    [SerializeField] private TextMeshPro _mainInstructionsText;
    [SerializeField] private TextMeshPro _inviteRewardDescription; 

    public void Start()
    {
        Slider masterVolSlider = MasterVolumeSlider.GetComponentInChildren<Slider>();
        masterVolSlider.value = AppConfig.inst.volumes.ContainsKey("master") ? AppConfig.inst.volumes["master"] : 1;
        AudioListener.volume = masterVolSlider.value;
        masterVolSlider.onValueChanged.AddListener((value) => { AudioListener.volume = value; AppConfig.inst.volumes["master"] = value; });

        _mainInstructionsText.SetText(AppConfig.inst.configData["mainInstructions"].Value.ToString());

        _inviteRewardDescription.enabled = (bool)AppConfig.inst.configData["EnablePyramidSchemeInvites"].Value; 

        GenerateUIForConfigValues();
        menuBar.OnButtonPressed(menuBar.buttons[0]);

        Invoke("GenerateAudioTabSliders", 0.5f); //Add a 1 second delay so there's time to load the config
        /*
                showSuperchatMultiplier.SetActive(AppConfig.inst.GetB("showSuperChatInstructions"));
                showSuperStickers.SetActive(AppConfig.inst.GetB("showSuperStickers"));

                AppConfig.inst.configData["showSuperChatInstructions"].PropertyChanged += (object value, PropertyChangedEventArgs e) => showSuperchatMultiplier.SetActive((bool)value);
                AppConfig.inst.configData["showSuperStickers"].PropertyChanged += (object value, PropertyChangedEventArgs e) => showSuperStickers.SetActive((bool)value);*/

        AppConfig.inst.configData["mainInstructions"].PropertyChanged += (object value, PropertyChangedEventArgs e) => _mainInstructionsText.SetText((string)value);
        AppConfig.inst.configData["EnablePyramidSchemeInvites"].PropertyChanged += (object value, PropertyChangedEventArgs e) => _inviteRewardDescription.enabled = (bool)value;
    }

    public void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            _originRoot.SetActive(!_originRoot.activeSelf);

        //If the settings origin isn't active, don't update anything
        if (!_originRoot.gameObject.activeSelf)
            return;

        _twitchTokenExpireTimerText.SetText($"Twitch Token Expire Timer: {TwitchApi._expirationTime.ToLocalTime()}");
        _spotifyTokenExpireTimerText.SetText($"Spotify Token Expire Timer: {_spotifyDJ._expirationTime.ToLocalTime()}");
    }
    private void GenerateUIForConfigValues()
    {
        //Create a Menu Button for each category
        string[] menuHeaders = Enum.GetNames(typeof(UIMenu));
        for (int i = 3; i < menuHeaders.Length; i++)
        {
            GameObject newMenuButton = Instantiate(MenuButtonOrigin.gameObject, MenuButtonOrigin.transform.parent);
            newMenuButton.name = menuHeaders[i] + "_Button";
            newMenuButton.transform.position = MenuButtonOrigin.transform.position;
            newMenuButton.transform.localScale = MenuButtonOrigin.transform.localScale;
            newMenuButton.GetComponentInChildren<TextMeshProUGUI>().SetText(menuHeaders[i]);
            //newMenuButton.GetComponent<RectTransform>().Translate(new Vector3(0, -(i) * MenuButtonOrigin.transform.GetComponent<RectTransform>().rect.height, 0));

            menuBar.buttons.Add(newMenuButton.GetComponent<Button>());

            //Create the page root
            GameObject newPageRoot = new GameObject(menuHeaders[i] + "_PageRoot");
            newPageRoot.transform.SetParent(PagesRoot);
            newPageRoot.transform.localScale = Vector3.one; 
            GameObject newScrollView = Instantiate(emptyScrollView.gameObject, newPageRoot.transform);
            newScrollView.name = "scrollView";
            newScrollView.transform.position = emptyScrollView.transform.position;
            menuBar.pageRoots.Add(newPageRoot);
        }


        //bool = toggle
        //string = input field string
        //float = input field float
        //int = input field int

        var keys = AppConfig.inst.configData.Keys;
        foreach (var key in keys)
        {
            ConfigItem cfgEntry = AppConfig.inst.configData[key];

            //Get the page root
            Transform parent = menuBar.pageRoots[(int)cfgEntry.Menu].transform;
            if (parent == null)
                continue;

            //Get the content root of the scroll view
            VerticalLayoutGroup vlg = parent.GetComponentInChildren<VerticalLayoutGroup>();

            if (vlg == null)
                continue;

            Transform contentRoot = vlg.transform; 
            

            var valueType = cfgEntry.Value.GetType();
            //CLDebug.Inst.Log("valueType: " + valueType);
            if(valueType == typeof(string) || valueType == typeof(int) || valueType == typeof(long) || valueType == typeof(double))
            {
                //Create Text Input Field
                GameObject inputFieldEntryRoot = Instantiate(emptyInputFieldEntry, contentRoot);
                inputFieldEntryRoot.GetComponentInChildren<TextMeshProUGUI>().SetText(cfgEntry.UIDisplayText); //Important: The Label TextMeshProUGUI needs to be the first child for depth first search
                TMP_InputField inputField = inputFieldEntryRoot.GetComponentInChildren<TMP_InputField>();
                inputField.text = cfgEntry.Value.ToString();
                if (valueType == typeof(int))
                    inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
                else if (valueType == typeof(float) || valueType == typeof(double))
                    inputField.contentType = TMP_InputField.ContentType.DecimalNumber;
                else
                    inputField.contentType = TMP_InputField.ContentType.Standard;

                inputField.onValueChanged.AddListener((value) => { AppConfig.inst.SetV(key, value); });
            }
            else if(valueType == typeof(bool))
            {
                //Create bool toggle ui
                GameObject toggleEntryRoot = Instantiate(emptyToggleEntry, contentRoot);
                toggleEntryRoot.GetComponentInChildren<TextMeshProUGUI>().SetText(cfgEntry.UIDisplayText);
                Toggle toggle = toggleEntryRoot.GetComponent<Toggle>();
                toggle.isOn = AppConfig.inst.GetB(key);
                toggle.onValueChanged.AddListener((value) => { AppConfig.inst.SetV(key, value); }); 
            }


        }

    }

    private void GenerateAudioTabSliders()
    {
        for(int i = 0; i < AudioCausesRoot.childCount; i++)
        {
            Transform audioCause = AudioCausesRoot.GetChild(i);
            AudioSource audioSource = audioCause.GetComponent<AudioSource>();

            string audioCauseName = audioCause.name;

            //If the config doesn't contain a volume for this cause, create one and set the volume to the current audio source volume
            if (!AppConfig.inst.volumes.ContainsKey(audioCauseName))
            {
                AppConfig.inst.volumes.Add(audioCauseName, audioSource.volume);
            }
            float volume = AppConfig.inst.volumes[audioCauseName];

            GameObject newAudioControls = Instantiate(MasterVolumeSlider.gameObject);
            newAudioControls.gameObject.transform.SetParent(AudioScrollViewContentRoot);
            newAudioControls.transform.position = MasterVolumeSlider.transform.position;
            newAudioControls.transform.localScale = MasterVolumeSlider.transform.localScale;
            //newAudioControls.transform.Translate(new Vector3(0, -i * 80 - 180, 0));

            newAudioControls.GetComponentInChildren<TextMeshProUGUI>().text = audioCauseName;
            Slider slider = newAudioControls.GetComponentInChildren<Slider>();
            slider.value = volume;

            //Set the audio source volume, and change the config file volume whenever we move the slider
            slider.onValueChanged.AddListener((value) => { audioSource.volume = value; AppConfig.inst.volumes[audioCauseName] = value; });

            audioSource.volume = volume;
        }
    }

    public void OnSaveAllPlayersToDBButtonPress()
    {
        StartCoroutine(_gm.SaveAllPlayerProfilesToDB());
    }

}

