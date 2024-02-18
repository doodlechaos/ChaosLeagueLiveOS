using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using TwitchLib.Api.Helix.Models.Chat.Emotes;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class DynamicSpriteAsset : MonoBehaviour
{

    [SerializeField] private bool createSpriteTexturePackerJSON;
    [SerializeField] private bool testAddSpriteToSpriteSheet;
    [SerializeField] private bool freeStoredEmotes; 

    [SerializeField] private TMP_SpriteAsset _dynamic_tmp_spriteAsset;
    [SerializeField] private Sprite _testSprite;

    [SerializeField] private Vector2 _testWriteLocation = new Vector2(0, 0); 

    [SerializeField] private Vector2 _twitchEmoteSizeLimit = new Vector2(112, 112);
    [SerializeField] private Vector2 _spriteSheetCellSize = new Vector2(128, 128);

    [SerializeField] private string _dynamicSpriteSheetFileName = "dynamic_sprite_sheet_4096.png"; 
    //private int _nextOpenIndex = 0;

    //List<string> emoteNamesReplaced = new List<string>();
    //List<string> downloadedEmoteIds = new List<string>();
    private StringBuilder _spriteInfusedMsg;

    private void Start()
    {
        _spriteInfusedMsg = new StringBuilder();

        //load the texture from streaming assets

        //_dynamic_tmp_spriteAsset.spriteSheet = 
        //Debug.Log($"Preloaded previous emote Ids in config. Count: {AppConfig.inst.downloadedEmoteIndexMap.Count}");
        StartCoroutine(LoadSpriteSheetImage()); 
    }

    IEnumerator LoadSpriteSheetImage()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, _dynamicSpriteSheetFileName);

        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture("file:///" + filePath))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to load dynamic emote sprite sheet: " + uwr.error);
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);

                _dynamic_tmp_spriteAsset.spriteSheet = texture;
                _dynamic_tmp_spriteAsset.material.mainTexture = texture;
                Debug.Log("Success loading dynamic sprite sheet from file"); 
            }
        }
    }

    private void OnDestroy()
    {
        var texture = _dynamic_tmp_spriteAsset.spriteSheet as Texture2D;
        string filePath = Path.Combine(Application.streamingAssetsPath, _dynamicSpriteSheetFileName);
        File.WriteAllBytes(filePath, texture.EncodeToPNG());
        Debug.Log("Saving dynamic sprite sheet to file."); 
    }

    private void OnValidate()
    {
        if (createSpriteTexturePackerJSON)
        {
            createSpriteTexturePackerJSON = false;
            CreateSpriteTexturePackerJSON();
        }

        if (testAddSpriteToSpriteSheet)
        {
            testAddSpriteToSpriteSheet = false;
            AddSpriteToAsset(_testSprite, "zinger", AppConfig.inst.downloadedEmoteIndexMap.Count); 
        }

        if (freeStoredEmotes)
        {
            freeStoredEmotes = false;
            FreeStoredEmotes();
        }
    }

    private void FreeStoredEmotes()
    {
        int i = 0; 
        //Preload the emote names that are already downloaded
        foreach (var tmp_spriteCharacter in _dynamic_tmp_spriteAsset.spriteCharacterTable)
        {
            tmp_spriteCharacter.name = $"free{i}";
            i++; 
        }

        Texture2D spriteSheet = _dynamic_tmp_spriteAsset.spriteSheet as Texture2D;
        for (int x = 0; x < spriteSheet.width; x++)
        {
            for (int y = 0; y < spriteSheet.height; y++)
            {
                spriteSheet.SetPixel(x, y, Color.clear);
            }
        }
        spriteSheet.Apply();
        OnDestroy(); 
    }

    private void CreateSpriteTexturePackerJSON()
    {
        SpriteTexturePackerJSONRoot spriteTexturePackerJSONRoot = new SpriteTexturePackerJSONRoot();

        List<FrameData> frames = new List<FrameData>(); 
        Texture2D spriteSheet = _dynamic_tmp_spriteAsset.spriteSheet as Texture2D;
        for (int x = 0; x < spriteSheet.width; x += (int)_spriteSheetCellSize.x)
        {
            for(int y = 0; y < spriteSheet.height; y += (int)_spriteSheetCellSize.y)
            {
                FrameData frame = new FrameData()
                {
                    filename = $"{x}-{y}.png",
                    frame = new Rectangle()
                    {
                        x = x,
                        y = y,
                        w = (int)_spriteSheetCellSize.x,
                        h = (int)_spriteSheetCellSize.y
                    },
                    rotated = false,
                    trimmed = false,
                    spriteSourceSize = new Rectangle()
                    {
                        x = x,
                        y = y,
                        w = (int)_spriteSheetCellSize.x,
                        h = (int)_spriteSheetCellSize.y
                    },
                    sourceSize = new Size()
                    {
                        w = (int)_spriteSheetCellSize.x,
                        h = (int)_spriteSheetCellSize.y
                    },
                    pivot = new Point()
                    {
                        x = 0,
                        y = 0,
                    }
                };
                frames.Add(frame);
            }
        }

        spriteTexturePackerJSONRoot.frames = frames;

        string json = JsonConvert.SerializeObject(spriteTexturePackerJSONRoot, Formatting.Indented);

        File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "spriteTexturePackerGen.txt"), json);

    }

    public IEnumerator GetSpriteInfusedMsg(CoroutineResult<string> coRes, string rawMsg, List<TwitchLib.Client.Models.Emote> emotes, bool isMe = false)
    {
        _spriteInfusedMsg.Clear();

        if (emotes.Count <= 0)
        {
            coRes.Complete(rawMsg);
            yield break;
        }

        emotes.Sort((e1, e2) => e1.StartIndex.CompareTo(e2.StartIndex));

        //Download the emotes only if the broadcaster uses it
        if (isMe)
        {
            foreach (var emote in emotes)
            {
                if (!AppConfig.inst.downloadedEmoteIndexMap.ContainsKey(emote.Id))
                {
                    int index = AppConfig.inst.downloadedEmoteIndexMap.Count;
                    AppConfig.inst.downloadedEmoteIndexMap.Add(emote.Id, index);

                    yield return DownloadEmoteFromURL(emote.ImageUrl, emote.Id, index);
                }
            }
        }

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

            //If we're in the range of an emote, append the sprite insertion and skip to the end
            if (currEmote.StartIndex + highSurrogatesFound <= i && i <= currEmote.EndIndex + highSurrogatesFound)
            {
                int emoteIndex = -1;
                AppConfig.inst.downloadedEmoteIndexMap.TryGetValue(currEmote.Id, out emoteIndex);
                if(emoteIndex == -1)
                    _spriteInfusedMsg.Append($"<sprite=\"EmojiOne\" index=12>"); //If it's a emote we don't have downloaded, do the default ??'s emoji
                else
                    _spriteInfusedMsg.Append($"<sprite=\"dynamicSpriteAsset\" index={emoteIndex}>"); ///name=\"Emote{currEmote.Id}\">");

                i = currEmote.EndIndex + highSurrogatesFound;

                currEmoteIndex++;
                if (currEmoteIndex < emotes.Count)
                    currEmote = emotes[currEmoteIndex];
            }
            else
            {
                //If we're not in an emote, just copy the character
                _spriteInfusedMsg.Append(rawMsg[i]);
            }
        }
       

        coRes.Complete(_spriteInfusedMsg.ToString());
    }

    public IEnumerator DownloadEmoteFromURL(string emoteURL, string emoteId, int index)
    {
        Debug.Log($"Downloading emoteId: {emoteId}");
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(emoteURL))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
                Debug.Log(www.error);
            else
            {
                Texture2D emoteTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                Rect rect = new Rect(0, 0, emoteTexture.width, emoteTexture.height);
                Vector2 pivot = new Vector2(0f, 0f);  // This sets the pivot to the center.
                Sprite emote = Sprite.Create(emoteTexture, rect, pivot);

                AddSpriteToAsset(emote, emoteId, index); 
            }
        }
        
    }

    private void AddSpriteToAsset(Sprite sprite, string Id, int index)
    {
        sprite = ScaleSpriteToSize(sprite, (int)_spriteSheetCellSize.x, (int)_spriteSheetCellSize.y); 

        Texture2D texture = _dynamic_tmp_spriteAsset.spriteSheet as Texture2D;

        _dynamic_tmp_spriteAsset.spriteCharacterTable[index].name = $"Emote{Id}";

        Vector2 writeLocation = GetNextOpenSpriteSheetPixelCoordinate(index);

        texture.SetPixels((int)writeLocation.x, (int)writeLocation.y, (int)sprite.rect.width, (int)sprite.rect.height, sprite.texture.GetPixels());

        texture.Apply();

        _dynamic_tmp_spriteAsset.spriteSheet = texture;
        _dynamic_tmp_spriteAsset.UpdateLookupTables();

    }

    private Vector2 GetNextOpenSpriteSheetPixelCoordinate(int index)
    {
        Texture2D spriteSheet = _dynamic_tmp_spriteAsset.spriteSheet as Texture2D;

        // Calculate total columns (width divided by cell width) and total rows (height divided by cell height)
        int totalCols = (int)(spriteSheet.width / _spriteSheetCellSize.x);
        int totalRows = (int)(spriteSheet.height / _spriteSheetCellSize.y);

        // Calculate column and row based on the next open index
        int nextOpenIndex = index; 
        int col = (nextOpenIndex / totalCols);   // Index divided by total number of rows gives the column
        int row = totalRows - 1 - (nextOpenIndex % totalRows);   // Index modulo total number of rows gives the row

        return new Vector2(col * _spriteSheetCellSize.x, row * _spriteSheetCellSize.y);
    }

    public Sprite ScaleSpriteToSize(Sprite sprite, int targetWidth, int targetHeight)
    {
        if (sprite == null)
            return null;

        // Create a new texture with target dimensions
        Texture2D scaledTexture = new Texture2D(targetWidth, targetHeight, TextureFormat.ARGB32, false);
        //scaledTexture.alphaIsTransparency = true;

        // Calculate scale factors
        float scaleX = (float)sprite.texture.width / targetWidth;
        float scaleY = (float)sprite.texture.height / targetHeight;

        // Sample and set pixels on the new texture
        for (int y = 0; y < scaledTexture.height; y++)
        {
            for (int x = 0; x < scaledTexture.width; x++)
            {
                Color pixelColor = sprite.texture.GetPixel((int)(x * scaleX), (int)(y * scaleY));
                scaledTexture.SetPixel(x, y, pixelColor);
            }
        }
        scaledTexture.Apply();

        // Create a new sprite with the scaled texture
        return Sprite.Create(scaledTexture, new Rect(0.0f, 0.0f, targetWidth, targetHeight), new Vector2(0f, 0f), sprite.pixelsPerUnit);
    }

    public class SpriteTexturePackerJSONRoot
    {
        public List<FrameData> frames { get; set; }
    }

    [Serializable]
    public class FrameData
    {
        public string filename { get; set; }
        public Rectangle frame { get; set; }
        public bool rotated { get; set; }
        public bool trimmed { get; set; }
        public Rectangle spriteSourceSize { get; set; }
        public Size sourceSize { get; set; }
        public Point pivot { get; set; }
    }
    [Serializable]
    public class Rectangle
    {
        public int x { get; set; }
        public int y { get; set; }
        public int w { get; set; }
        public int h { get; set; }
    }
    [Serializable]
    public class Size
    {
        public int w { get; set; }
        public int h { get; set; }
    }
    [Serializable]
    public class Point
    {
        public double x { get; set; }
        public double y { get; set; }
    }

}

