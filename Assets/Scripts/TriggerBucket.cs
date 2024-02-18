using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerBucket : MonoBehaviour
{

    [SerializeField] private bool _openPassthrough;
    [SerializeField] private MeshRenderer _bottomMeshRenderer;
    [SerializeField] private BoxCollider2D _bottomCol2D;

    private void OnValidate()
    {
        OpenBottom(_openPassthrough); 
    }

    private void Awake()
    {
        OpenBottom(_openPassthrough); 
    }

    private void OpenBottom(bool open)
    {

        _bottomMeshRenderer.enabled = !open;
        _bottomCol2D.enabled = !open;
        _bottomCol2D.isTrigger = open; //Have to make it a trigger so when the tile inits it doesn't turn into a collider
    }
    
}
