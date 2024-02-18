using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawLineMaster : MonoBehaviour
{
    public static DrawLineMaster Inst;

    [SerializeField] private GameObject _linePrefab;
    [SerializeField] private Transform _linesRoot;
    [SerializeField] private HoldingPen _holdingPen;

    private ObjectPool<Line> _linePool;

    private void Awake()
    {
        Inst = this;
        _linePool = new ObjectPool<Line>(LineFactory, OnLineTurnOn, OnLineTurnOff);
    }

    private Line LineFactory()
    {
        return Instantiate(_linePrefab, _linesRoot).GetComponent<Line>();
    }

    private void OnLineTurnOn(Line line)
    {
        line.gameObject.SetActive(true);
    }

    private void OnLineTurnOff(Line line)
    {
        line.gameObject.SetActive(false);
    }

    public Line DrawLine(Vector3 startPos, float startMargin, Vector3 endPos, float endMargin, float fadeDuration, Color color)
    {
        Line line = _linePool.GetObject();

        line.InitLine(startPos, startMargin, endPos, endMargin, fadeDuration, color);
        return line;
    }

    public void DestroyLine(Line line)
    {
        line.InitLine(_holdingPen.transform.position, 0, _holdingPen.transform.position, 0, 0, Color.white);
        _linePool.ReturnObject(line);
    }

}
