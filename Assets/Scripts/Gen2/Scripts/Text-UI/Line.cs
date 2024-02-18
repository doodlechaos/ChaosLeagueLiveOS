using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Line : MonoBehaviour
{

    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float lineZ = 0.25f;

    private float _fadeDuration;
    private float _fadeTimer = 0; 

    public void InitLine(Vector3 startPos, float startMargin, Vector3 endPos, float endMargin, float fadeDuration, Color color)
    {
        _fadeTimer = 0;
        _fadeDuration = fadeDuration;
        lineRenderer.material.color = color;

        Vector3 direction = (endPos - startPos).normalized;
        startPos = startPos + (direction * startMargin);
        endPos = endPos - (direction * endMargin);

        startPos.z = lineZ;
        endPos.z = lineZ;

        lineRenderer.SetPositions(new Vector3[] { startPos, endPos }); 
    }

    // Update is called once per frame
    void Update()
    {
        if(_fadeTimer <= _fadeDuration)
        {
            float t = _fadeTimer / _fadeDuration;

            Color lineColor = lineRenderer.material.color;
            lineColor.a = 1 - t;
            lineRenderer.material.color = lineColor;

            _fadeTimer += Time.deltaTime;
        }
        else
        {
            DrawLineMaster.Inst.DestroyLine(this); 
        }
    }
}
