using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public abstract class ShopEntry : PlayerReceiveable
{
    [SerializeField] private MeshRenderer _entryBackground;
    [SerializeField] public MeshRenderer DisplayBackground;
    [SerializeField] private TextMeshPro _buyCommandText;
    [SerializeField] private TextMeshPro _goldCostText;

    public int GoldCost;
    
    public void InitEntryBase(int goldCost, List<string> buyCommandText)
    {
        _goldCostText.SetText($"Cost:\n{MyUtil.AbbreviateNum4Char(goldCost)} Gold");
        _buyCommandText.SetText(buyCommandText[0]);
        GoldCost = goldCost; 
    }

    public void InitMultiEntryBase(int goldCost, List<string> buyCommandText)
    {
        _goldCostText.SetText($"Total Cost:\n{MyUtil.AbbreviateNum4Char(goldCost)} Gold");
        _buyCommandText.SetText(buyCommandText[0]);
        GoldCost = goldCost;
    }


    public void HideCommandText()
    {
        _buyCommandText.gameObject.SetActive(false);
    }

    public void ShowCommandText()
    {
        _buyCommandText.gameObject.SetActive(true);
    }


    public override Vector3 GetReceivePosition()
    {
        return DisplayBackground.transform.position;
    }



    public IEnumerator PurchaseAnimation(PlayerBall pb)
    {
        //Play buy animation, spin, then fly off screen.
        pb.SpinY();

        if (pb.Ph.IsKing())
            yield break;

        yield return new WaitForSeconds(1.5f);

        pb.Ph.ReceivableTarget = pb.Ph.GetGameManager().HoldingPen; 
    }
}
