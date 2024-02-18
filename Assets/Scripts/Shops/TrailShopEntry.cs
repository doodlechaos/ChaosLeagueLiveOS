using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrailShopEntry : ShopEntry
{
    [SerializeField] private Transform _demoPlayer;
    [SerializeField] private float _demoPlayerRotateSpeed = 1;
    [SerializeField] private TrailRenderer _demoTrail;
    [SerializeField] public List<string> BuyCommands; 
    private Gradient _gradient;

    public void InitEntry(Gradient gradient, int tier, int goldCost)
    {
        InitEntryBase(goldCost, BuyCommands); 

        _gradient = gradient;
        _demoTrail.colorGradient = gradient;

        float trailTime; 
        if (tier >= 3)
            trailTime = AppConfig.inst.GetF("TrailTierThreeTime");
        else if (tier >= 2)
            trailTime = AppConfig.inst.GetF("TrailTierTwoTime");
        else
            trailTime = AppConfig.inst.GetF("TrailTierOneTime");

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
