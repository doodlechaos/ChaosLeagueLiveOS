using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawArrowMaster : MonoBehaviour
{
    public static DrawArrowMaster Inst;

    [SerializeField] private GameObject _arrowPrefab;
    [SerializeField] private Transform _arrowsRoot;
    [SerializeField] private HoldingPen _holdingPen;

    private ObjectPool<Arrow> _arrowPool;

    private void Awake()
    {
        Inst = this;
        _arrowPool = new ObjectPool<Arrow>(ArrowFactory, OnArrowTurnOn, OnArrowTurnOff);
    }

    private Arrow ArrowFactory()
    {
        return Instantiate(_arrowPrefab, _arrowsRoot).GetComponent<Arrow>();
    }

    private void OnArrowTurnOn(Arrow arrow)
    {
        arrow.gameObject.SetActive(true);
    }

    private void OnArrowTurnOff(Arrow arrow)
    {
        arrow.gameObject.SetActive(false);
    }

    public Arrow DrawArrow(Transform startPoint, float startMargin, Transform endPoint, float endMargin, bool animateDraw, Color color)
    {
        Arrow arrow = _arrowPool.GetObject();

        arrow.InitArrow(this, startPoint, startMargin, endPoint, endMargin, animateDraw, color);
        return arrow; 
    }

    public void DestroyArrow(Arrow arrow)
    {
        arrow.InitArrow(this, _holdingPen.transform, _holdingPen.transform);
        _arrowPool.ReturnObject(arrow); 
    }

}
