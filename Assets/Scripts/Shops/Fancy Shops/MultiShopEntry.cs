using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class MultiShopEntry : ShopEntry
{
    [SerializeField] private Transform _demoPlayer;
    [SerializeField] private float _demoPlayerRotateSpeed;
    [SerializeField] private TrailRenderer _demoTrail;
    [SerializeField] private SpriteRenderer _bubbleContainer;
    [SerializeField] private TextMeshPro _bubbleText;
    [SerializeField] private LineRenderer _bubbleStalk;
    [SerializeField] public List<string> BuyCommands; 


    private Gradient _gradient;
    private Color _purchaseColor;
       
    public void InitEntry1(int tier, bool isGolden, float goldenDiscount)
    {


        _bubbleText.SetText("Text Color!");
        Color color;
        int randomPrice;

        color = Color.HSVToRGB(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)); //can be anything
        randomPrice = Mathf.RoundToInt(Random.Range(4, 7) * 1500);
  
        if (isGolden)
            randomPrice = Mathf.RoundToInt(randomPrice * (1 - goldenDiscount));

        InitEntryBase(randomPrice, BuyCommands);

        _bubbleText.color = color;
        _purchaseColor = color;

    }

    public void InitEntry2(int tier, bool isGolden, float goldenDiscount)
    {

        Color color;
        int randomPrice = 1;
        color = Color.HSVToRGB(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)); //can be anything
        randomPrice = randomPrice + Mathf.RoundToInt(Random.Range(7, 20) * 1500);
  
        if (isGolden)
            randomPrice = Mathf.RoundToInt(randomPrice * (1 - goldenDiscount));

        InitEntryBase(randomPrice, BuyCommands);

        color = MyUtil.SetColorSaveAlpha(color, _bubbleContainer.color);

        _purchaseColor = color;

        _bubbleContainer.color = color;
        _bubbleText.SetText("");

        color.a = 0;
        _bubbleStalk.startColor = color;
        color.a = 1;
        _bubbleStalk.endColor = color;
    }

    public void InitEntry3(Gradient gradient, int tier, int goldCost)
    {
        InitEntryBase(goldCost, BuyCommands);

        _gradient = gradient;
        _demoTrail.colorGradient = gradient;

        float trailTime;
        trailTime = AppConfig.inst.GetF("TrailTierThreeTime");
      
        _demoTrail.time = trailTime;
    }

    public override void ReceivePlayer(PlayerBall pb)
    {
        AttemptPurchase(pb); 
    }

    public void AttemptPurchase(PlayerBall pb)
    {
        if (pb.Ph.pp.Gold < GoldCost)
        {
            Debug.Log("Player doesn't have enough gold");
            return;
        }

        pb.Ph.pp.Gold -= GoldCost;
        //Gold subtraction popup
        TextPopupMaster.Inst.CreateTextPopup(pb.GetPosition(), Vector3.up, $"-{MyUtil.AbbreviateNum4Char(GoldCost)} GOLD", MyColors.Gold);

        string json = GradientSerializer.SerializeGradient(_gradient);
        Debug.Log($"Setting {pb.Ph.pp.TwitchUsername} player JSON: {json}");
        pb.Ph.pp.TrailGradientJSON = json;
        pb.Ph.SetCustomizationsFromPP();

        StartCoroutine(PurchaseAnimation(pb));

        AudioController.inst.PlaySound(AudioController.inst.StorePurchase, 0.95f, 1.05f);
    }

    private void Update()
    {
        //Don't spin while game isn't active to avoid drift
        //if(_buyCommandText.gameObject.activeSelf)
        _demoPlayer.RotateAround(DisplayBackground.transform.position, DisplayBackground.transform.forward, _demoPlayerRotateSpeed);
    }


}
