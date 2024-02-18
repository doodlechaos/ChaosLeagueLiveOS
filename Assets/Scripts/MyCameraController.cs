using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyCameraController : MonoBehaviour
{
    [SerializeField] private Animation _KingFocusAnimation;
    [SerializeField] private bool _testKingCamMoveButton; 

    private void OnValidate()
    {
        if (_testKingCamMoveButton)
        {
            _testKingCamMoveButton = false;
            KingFocusCameraMove(); 
        }
    }

    public void KingFocusCameraMove()
    {
        _KingFocusAnimation.Play(); 
    }

}
