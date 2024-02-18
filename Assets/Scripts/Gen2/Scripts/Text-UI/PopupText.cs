using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PopupText : MonoBehaviour
{
    [SerializeField] private TextMeshPro _text;

    private TextPopupMaster _textPopupMaster;
    private float _t;
    private Vector3 _startPos;
    private Vector3 _targetPos;
    private float _holdScale;

    private Vector2 _travel;
    private Vector2 _fade;
    private Vector2 _scale;
    private float _speed;

    public void Initialize(TextPopupMaster master, 
                            string txt, 
                            Vector3 startPos, 
                            Vector3 targetPos, 
                            float holdScale, 
                            Color txtColor, 
                            bool bold,
                            Vector2 travel,
                            Vector2 fade,
                            Vector2 scale,
                            float speed)
    {
        _t = 0; 
        _textPopupMaster = master;

        _text.SetText(txt);

        _startPos = startPos;
        _targetPos = targetPos;
        _holdScale = holdScale;
        _travel = travel;
        _fade = fade;
        _scale = scale;
        _speed = speed;

        if (bold)
            _text.fontStyle = FontStyles.Bold;

        _text.color = txtColor; 
    }

    // Update is called once per frame
    void Update()
    {
        if (_t >= 1)
        {
            _textPopupMaster.ReturnPopupTextToPool(this); //ReturnPopUpTextToPool(tmp);
            return;
        }

        //Lerp Position
        _text.transform.position = Vector3.Lerp(_startPos, _targetPos, Mathf.Clamp01(MyUtil.Remap(_t, _travel.x, _travel.y, 0, 1)));

        //Fade alpha, but don't start until we reach the fade start and stop percentage by clamping the value
        _text.color = new Color(_text.color.r, _text.color.g, _text.color.b, Mathf.Clamp01(MyUtil.Remap(_t, _fade.x, _fade.y, 1, 0)));

        _text.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * _holdScale, Mathf.Clamp01(MyUtil.Remap(_t, _scale.x, _scale.y, 0, 1)));

        //Update the time
        _t += Time.deltaTime * _speed;
    }
}
