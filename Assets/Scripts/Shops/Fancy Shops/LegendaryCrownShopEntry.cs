using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LegendaryCrownShopEntry : ShopEntry
{
    [SerializeField] private Crown _crown; 
    [SerializeField] public List<string> BuyCommands;

    private List<Color> colors = new List<Color>();
    public void InitEntry(int tier, int goldCost)
    {
        InitEntryBase(goldCost, BuyCommands);

        colors = _crown.GetRandomColorList(tier);
        _crown.UpdateCustomizations(colors); 
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

        string json = CrownSerializer.GetJSONFromColorList(colors); 
        Debug.Log($"Setting {pb.Ph.pp.TwitchUsername} player JSON: {json}");
        pb.Ph.pp.CrownJSON = json;

        StartCoroutine(PurchaseAnimation(pb));

        AudioController.inst.PlaySound(AudioController.inst.StorePurchase, 0.95f, 1.05f);
    }

    private void Update()
    {
        //Don't spin while game isn't active to avoid drift
        //if(_buyCommandText.gameObject.activeSelf)
        _crown.transform.RotateAround(_crown.transform.position, _crown.transform.forward, 0.05f);
    }

}
