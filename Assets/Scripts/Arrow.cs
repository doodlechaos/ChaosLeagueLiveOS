using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    private DrawArrowMaster _dam;

    [SerializeField] private LineRenderer _mainLine;
    [SerializeField] private LineRenderer _arrowHead;

    [SerializeField] private float _arrowHeadLength = 1;
    [Range(0, 1)]
    [SerializeField] private float _mainLineWidth = 0.2f;
    [Range(0, 1)]
    [SerializeField] private float _arrowHeadWidth = 1f;
    [SerializeField] private float _zDepth = 0.25f;

    public Transform StartPoint;
    public Transform EndPoint;
    private float _startMargin; 
    private float _endMargin;

    private Vector3[] _mainlinePositions = new Vector3[2]; 
    private Vector3[] _arrowHeadPositions = new Vector3[2];

    //TEMP
    [SerializeField] private bool testInitButton;
    [SerializeField] private Transform testStartPos;
    [SerializeField] private float testStartMargin;
    [SerializeField] private Transform testEndPos;
    [SerializeField] private float testEndMargin;
    [SerializeField] private bool testanimateDraw;

    private void OnValidate()
    {
        if (testInitButton)
        {
            testInitButton = false;
            InitArrow(null, testStartPos, testStartMargin, testEndPos, testEndMargin, testanimateDraw, Color.grey); 
        }
    }

    public void InitArrow(DrawArrowMaster dam, Transform startPoint, Transform endPoint)
    {
        InitArrow(dam, startPoint, 0, endPoint, 0, false, Color.gray);
    }

    public void InitArrow(DrawArrowMaster dam, Transform startPoint, float startMargin, Transform endPoint, float endMargin, bool animateDraw, Color color)
    {
        _dam = dam;

        StartPoint = startPoint;
        _startMargin = startMargin;

        EndPoint = endPoint;
        _endMargin = endMargin;

        _mainLine.startWidth = _mainLineWidth;
        _mainLine.endWidth = _mainLineWidth;

        _arrowHead.startWidth = 0;
        _arrowHead.endWidth = _arrowHeadWidth;

        _mainLine.material.color = color;
        _arrowHead.material.color = color;
    }

    // Update is called once per frame
    void Update()
    {
        if (StartPoint == null || EndPoint == null)
            return;

        Vector3 direction = (EndPoint.position - StartPoint.position).normalized;

        _mainlinePositions[0] = StartPoint.position + (direction * _startMargin);
        _mainlinePositions[1] = EndPoint.position - (direction * _endMargin) - (direction * _arrowHeadLength);

        _mainlinePositions[0].z = _zDepth;
        _mainlinePositions[1].z = _zDepth;

        _mainLine.SetPositions(_mainlinePositions);

        _arrowHeadPositions[0] = _mainlinePositions[1] + direction * _arrowHeadLength; 
        _arrowHeadPositions[1] = _mainlinePositions[1];

        _arrowHeadPositions[0].z = _zDepth;
        _arrowHeadPositions[1].z = _zDepth;

        _arrowHead.SetPositions(_arrowHeadPositions);
    }

    public void DestroyArrow()
    {
        _dam.DestroyArrow(this); 
    }
}
