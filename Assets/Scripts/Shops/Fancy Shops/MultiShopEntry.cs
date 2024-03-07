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
    private Color _purchaseColor1;
    private Color _purchaseColor2;
       
    public void InitEntry(Gradient gradient, Color color1, Color color2, int Tier)
    {

        InitMultiEntryBase(250_000, BuyCommands);
        float trailTime;
        _gradient = gradient;
        _demoTrail.colorGradient = gradient;

        trailTime = 1;

        _demoTrail.time = trailTime;

        if (Tier == 1)
        {
            _bubbleText.SetText("Text Color!"); 
        }
        else
        {
            _bubbleText.SetText("Bubble Color!");
        }

        _bubbleText.color = color1;
        _purchaseColor1 = color1;
         
        color2 = MyUtil.SetColorSaveAlpha(color2, _bubbleContainer.color);

        _purchaseColor2 = color2;

        _bubbleContainer.color = color2;
  
        color2.a = 0;
        _bubbleStalk.startColor = color2;
        color2.a = 1;
        _bubbleStalk.endColor = color2;

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

        string hexString1 = MyUtil.ColorToHexString(_purchaseColor1);
        pb.Ph.pp.SpeechBubbleTxtHex = hexString1;

        pb.Ph.SetCustomizationsFromPP();

        string hexString2 = MyUtil.ColorToHexString(_purchaseColor2);
        pb.Ph.pp.SpeechBubbleFillHex = hexString2;

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
