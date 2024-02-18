using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class TextPopupMaster : MonoBehaviour
{
    public static TextPopupMaster Inst;

    [SerializeField] private GameManager _gm; 

    [SerializeField] private GameObject _travelingIndicatorPrefab;
    [SerializeField] private GameObject _popupTextPrefab;

    [SerializeField] private Transform _travelingIndicatorsRoot;
    [SerializeField] private Transform _popupTextsRoot;

    [SerializeField] private float _holdScale;
    [SerializeField] private float _defaultSpeedScale;
    [SerializeField] private float _travelDistance;
    [SerializeField] private float _defaultRandomSpreadDist = 0.5f; 

    [Tooltip("For lerping from 0 to 1, the percentage that the travel time starts and finishes at")]
    [SerializeField] private Vector2 _travel;
    [Tooltip("For lerping from 0 to 1, the percentage that the fade time starts and finishes at")]
    [SerializeField] private Vector2 _fade;
    [Tooltip("For lerping from 0 to 1, the percentage that the scale time starts and finishes at")]
    [SerializeField] private Vector2 _scale;

    private ObjectPool<PopupText> _popupTextPool;// = new List<TextMeshPro>();
    private ObjectPool<TI_Text> _TITextPool;// = new List<TravelingIndicator>();

    private Vector3 defaultTI_Scale; 

    //private List<(float t,  Vector3 startPos, Vector3 targetPos, float holdScale, TextMeshPro)> _activeTexts = new List<(float t, Vector3 startPos, Vector3 targetPos, float holdScale, TextMeshPro)>();


    private void Awake()
    {
        Inst = this;

        defaultTI_Scale = _travelingIndicatorPrefab.transform.localScale;

        _popupTextPool = new ObjectPool<PopupText>(PopupTextFactory, TurnOnPopupText, TurnOffPopupText);
        _TITextPool = new ObjectPool<TI_Text>(TI_Factory, TurnOnTIText, TurnOffTIText); 
    }

    public void CreateTextPopup(Vector3 startPos, Vector3 dirVector, string text,Color color)
    {
        CreateTextPopup(startPos, _holdScale, Vector3.zero, dirVector, text, color, false, _travelDistance, _defaultRandomSpreadDist, _defaultSpeedScale);
    }
    public void CreateTextPopup(Vector3 startPos, Vector3 dirVector, string text, Color color, float randomSpreadDist)
    {
        CreateTextPopup(startPos, _holdScale, Vector3.zero, dirVector, text, color, false, _travelDistance, randomSpreadDist, _defaultSpeedScale);
    }
    public void CreateTextPopup(Vector3 startPos, Vector3 dirVector, string text, Color color, float randomSpreadDist, float speed)
    {
        CreateTextPopup(startPos, _holdScale, Vector3.zero, dirVector, text, color, false, _travelDistance, randomSpreadDist, speed);
    }

    public void CreateTextPopup(Vector3 startPos, 
                                float scale,
                                Vector3 eulerAngles, 
                                Vector3 dirVector, 
                                string text, 
                                Color color, 
                                bool bold, 
                                float travelDistance,
                                float randomSpreadDist,
                                float speedScale)
    {
        PopupText popupTxt = _popupTextPool.GetObject(); //GetPooledPopupText();

        float randomSpread = Random.Range(-randomSpreadDist, randomSpreadDist);
        Vector3 targetPos = startPos + dirVector * travelDistance + new Vector3(randomSpread, randomSpread, 0);


        popupTxt.Initialize(this, text, startPos, targetPos, scale, color, bold, _travel, _fade, _scale, speedScale);
        popupTxt.transform.position = startPos;
        popupTxt.transform.eulerAngles = eulerAngles;

        popupTxt.transform.localScale = Vector3.zero;


        //_activeTexts.Add((0, startPos, targetPos, scale, tmp));
    }

    public void CreateTravelingIndicator(string text, long value, TravelingIndicatorIO origin, TravelingIndicatorIO target, float speed, Color color, Texture avatarTex, TI_Type ti_type = TI_Type._Default)
    {
        CreateTravelingIndicator(text, value, origin.Get_TI_IO_Position(), target, speed, defaultTI_Scale, color, avatarTex, false, ti_type);
    }
    public void CreateTravelingIndicator(string text, long value, Vector3 origin, TravelingIndicatorIO target, float speed, Color color, Texture avatarTex, TI_Type ti_type = TI_Type._Default)
    {
        CreateTravelingIndicator(text, value, origin, target, speed, defaultTI_Scale, color, avatarTex, false, ti_type);
    }

    public void CreateTravelingIndicator(string text, long value, Vector3 origin, TravelingIndicatorIO target, float speed, Vector3 scale, Color color, Texture avatarTex, bool isLob, TI_Type ti_type = TI_Type._Default)
    {
        TI_Text ti = _TITextPool.GetObject(); //GetPooledTIText();
        ti.transform.position = origin;
        ti.InitializeNewTI(this, origin, target, value, text, color, speed, defaultTI_Scale, avatarTex, isLob, ti_type);
    }

    private PopupText PopupTextFactory()
    {
        // If no deactivated object found, instantiate a new one and add it to the pool
        GameObject newObj = Instantiate(_popupTextPrefab, _popupTextsRoot); 

        newObj.SetActive(false);
        newObj.transform.position = _gm.HoldingPen.Get_TI_IO_Position();
        return newObj.GetComponent<PopupText>();
    }

    private void TurnOnPopupText(PopupText pot)
    {
        pot.gameObject.SetActive(true);
    }

    private void TurnOffPopupText(PopupText pot)
    {
        pot.transform.position = _gm.HoldingPen.Get_TI_IO_Position();
        pot.gameObject.SetActive(false);
    }

    private TI_Text TI_Factory()
    {
        // If no deactivated object found, instantiate a new one and add it to the pool
        GameObject newObj = Instantiate(_travelingIndicatorPrefab); 
        newObj.transform.SetParent(_travelingIndicatorsRoot);
        newObj.SetActive(false);
        newObj.transform.position = _gm.HoldingPen.Get_TI_IO_Position();

        return newObj.GetComponent<TI_Text>(); 
    }

    private void TurnOnTIText(TI_Text ti)
    {
        ti.gameObject.SetActive(true);
    }

    private void TurnOffTIText(TI_Text ti)
    {
        ti.transform.position = _gm.HoldingPen.Get_TI_IO_Position();
        ti.gameObject.SetActive(false); 
    }

    public void ReturnTITextToPool(TI_Text ti)
    {
        _TITextPool.ReturnObject(ti);
    }

    public void ReturnPopupTextToPool(PopupText pt)
    {
        _popupTextPool.ReturnObject(pt);
    }
}
