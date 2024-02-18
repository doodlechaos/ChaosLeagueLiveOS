using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PBHologram : MonoBehaviour
{
    [SerializeField] private PlayerHandler Ph;

    [SerializeField] public MeshRenderer MainBody;
    [SerializeField] public MeshRenderer InviterIndicator;

    [SerializeField] public TextMeshPro UsernameText;
    [SerializeField] public TextMeshPro PointsText;

    public void InitPBHologram(PlayerHandler ph)
    {
        Ph = ph;
    }

    public void UpdateHologramPoints()
    {
        PointsText.SetText(MyUtil.AbbreviateNum4Char(Ph.pp.SessionScore));
    }

}
