using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class PointColorMap : ScriptableObject
{
    [SerializeField] private List<Vector2> pointRanges;

    [SerializeField] private List<Gradient> meshColors;
    [SerializeField] private List<Gradient> labelColors;


    [SerializeField] private PBEffect testType;
    [SerializeField] private long testPoints;
    [SerializeField] private float demoT;
    [SerializeField] private Color demoMesh;
    [SerializeField] private Color demoLabel;

    [SerializeField] private Vector2 _logicMap;
    [SerializeField] private Gradient _logicAddGradient;
    [SerializeField] private Gradient _logicSubtractGradient;
    private void OnValidate()
    {
        (demoMesh, demoLabel) = GetColors(testPoints, testType);
    }

    public (Color meshColor, Color labelColor) GetColors(long points, PBEffect type)
    {
        if(type.HasFlag(PBEffect.Subtract) && points > 0)
            points *= -1;


        if (type.HasFlag(PBEffect.Zero))
            return (Color.red, Color.white);
        if (type.HasFlag(PBEffect.Multiply))
            return (Color.cyan, Color.white);
        if (type.HasFlag(PBEffect.Divide)  )
            return (MyColors.Orange, Color.black);

        float t = -1;
        for (int i = 0; i < pointRanges.Count; i++)
        {
            Vector2 pointRange = pointRanges[i];

            //If we're not within the point range, continue
            if (points < pointRange.x || points > pointRange.y)
                continue;

            if (points == pointRange.x)
            {
                t = 0;
                demoT = t;
                return (meshColors[i].Evaluate(t), labelColors[i].Evaluate(t));
            }

            if (points == pointRange.y)
            {
                t = 1;
                demoT = t;
                return (meshColors[i].Evaluate(t), labelColors[i].Evaluate(t));
            }

            t = (points - pointRange.x) / (pointRange.y - pointRange.x);
            demoT = t;
            return (meshColors[i].Evaluate(t), labelColors[i].Evaluate(t));
        }

        if (type.HasFlag(PBEffect.Explode))
            return (Color.grey, Color.white);

        //Default
        return (Color.white, Color.black);
    }

    public Color GetLogicColor(EffectorLogic logicType, int num)
    {
        Color _logicColor = Color.clear;
        if (logicType == EffectorLogic.increment)
            _logicColor = _logicAddGradient.Evaluate(num / _logicMap.y);
        else if (logicType == EffectorLogic.decrement)
            _logicColor = _logicSubtractGradient.Evaluate(num / _logicMap.y);
        else if (logicType == EffectorLogic.none)
            _logicColor = Color.grey;

        return _logicColor;
    }

}
