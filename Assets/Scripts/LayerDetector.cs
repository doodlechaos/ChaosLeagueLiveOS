using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LayerDetector : MonoBehaviour
{
    [SerializeField] private LayerMask _layerToDetect;

    [Serializable] public class MyEventType : UnityEvent<object> { }

    [SerializeField] MyEventType OnTriggerEnterCall;
    [SerializeField] MyEventType OnTriggerExitCall;


    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer != MyUtil.ConvertLayerMaskToLayerNumber(_layerToDetect))
            return;

        OnTriggerEnterCall.Invoke(collision);
    }

    public void OnTriggerExit2D(Collider2D collision)
    {
        //Debug.Log("entering collision");
        if (collision.gameObject.layer != MyUtil.ConvertLayerMaskToLayerNumber(_layerToDetect))
            return;

        OnTriggerExitCall.Invoke(collision);
    }
}
