using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;

public class SpeechBubbleControllerV2 : MonoBehaviour
{
    private GameManager _gm;

    [SerializeField] private bool testButton;
    [SerializeField] private float ContainerAngle;
    [SerializeField] private float separationFromBall = 1;
    [SerializeField] private float tailEndWidth = 1;
    public float currSeparationFromBall;

    [SerializeField] public float spawnInTime = 1;
    [SerializeField] public float holdTime = 3;
    [SerializeField] public float fadeTime = 1;

    //[SerializeField] private float _bubbleTextZpos = -0.2f;

    public float stateTimer;

    public enum BubbleState { spawning, holding, fading, idle }
    public BubbleState currState = BubbleState.idle;

    public PlayerBall assignedPlayerBall;

    public LineRenderer lineRenderer;
    public Rigidbody2D rb2d;
    public SpriteRenderer BubbleContainer;
    public TextMeshPro BubbleText;
    public Vector2 padding = new Vector2(0.1f, 0.1f);

    private Vector3 BubbleContainerStartScale;

    private bool isFirstInit = true;

    public void InitSpeechBubbleController(GameManager gm, PlayerBall pb)
    {
        _gm = gm;

        assignedPlayerBall = pb;

        if (isFirstInit)
        {
            BubbleContainerStartScale = BubbleContainer.transform.localScale;
            isFirstInit = false;
        }


        SetState(BubbleState.idle);
        SetBubblePosition();

    }

    private void OnValidate()
    {
        if (testButton)
        {
            ActivateSpeechBubble("test message 123");
        }
    }

    // Update is called once per frame
    void Update()
    {
        //If we're idle, don't update anything
        if (currState == BubbleState.idle)
            return;

        SetBubblePosition();

        if (currState == BubbleState.spawning)
        {
            float t = stateTimer / spawnInTime;
            t = Mathf.Clamp01(t);
            BubbleContainer.transform.localScale = Vector3.Lerp(Vector3.zero, BubbleContainerStartScale * assignedPlayerBall._rb2D.transform.localScale.x, t); // new Vector3(t, t, 1);
            currSeparationFromBall = Mathf.Lerp(0, separationFromBall, t);
            lineRenderer.SetPositions(new Vector3[] { BubbleContainer.transform.position, rb2d.position });

            if (t >= 1)
            {
                SetState(BubbleState.holding);
            }
        }
        else if (currState == BubbleState.holding)
        {
            float t = stateTimer / holdTime;
            if (t >= 1 && !assignedPlayerBall.LockSpeechBubbleAngle)
            {
                SetState(BubbleState.fading);
            }
        }
        else if (currState == BubbleState.fading)
        {
            float t = stateTimer / fadeTime;

            SetAlpha(t);

            if (t >= 1)
                SetState(BubbleState.idle);
            
        }
        stateTimer += Time.deltaTime;
    }

    private void SetAlpha(float t)
    {
        //Fade the bubble fill color alpha
        Color fillColor = BubbleContainer.color; //fillMaterial.color;
        fillColor.a = Mathf.Lerp(0, _gm.BubbleFillHoldAlpha, 1 - t);
        BubbleContainer.color = fillColor;
        //Update the stalk color alpha
        lineRenderer.endColor = fillColor;

        //Fade the text color alpha
        Color textColor = BubbleText.color;
        textColor.a = Mathf.Lerp(0, _gm.BubbleTextHoldAlpha, 1 - t);
        BubbleText.color = textColor;
    }

    public void SetState(BubbleState _state)
    {
        if (_state == BubbleState.idle)
        {
            currSeparationFromBall = 0;
            BubbleContainer.transform.localScale = Vector3.zero; //Vector3.Lerp(Vector3.zero, BubbleContainerStartScale * assignedPlayerBall._rb2D.transform.localScale.x, t); // new Vector3(t, t, 1);
            currSeparationFromBall = 0; //Mathf.Lerp(0, separationFromBall, t);
            lineRenderer.SetPositions(new Vector3[] { BubbleContainer.transform.position, rb2d.position });

            SetAlpha(1);

        }
        if (_state == BubbleState.spawning)
        {
            //Reset the fade alpha for the fill
            Color currFillColor = BubbleContainer.color;
            currFillColor.a = _gm.BubbleFillHoldAlpha;
            BubbleContainer.color = currFillColor;
            //Reset the tail color
            Color currTailColor = lineRenderer.endColor;
            currTailColor.a = _gm.BubbleFillHoldAlpha;
            lineRenderer.endColor = currTailColor;

            //Reset the fade alpha for the text
            //Fade the text color alpha
            Color textColor = BubbleText.color;
            textColor.a = _gm.BubbleTextHoldAlpha;
            BubbleText.color = textColor;
        }

        currState = _state;
        stateTimer = 0;
    }

    public void ActivateSpeechBubble(string _text)
    {
        SetBubblePosition();
        SetState(BubbleState.spawning);
        BubbleText.SetText(_text);
    }


    private void SetBubblePosition()
    {
        if (assignedPlayerBall.IsExploding)
            return;
        SetBubbleContainer();
        SetTextBoundsWithinContainer();
    }

    private void SetBubbleContainer()
    {
        if (assignedPlayerBall == null)
            return;

        float parentScaleMag = assignedPlayerBall._rb2D.transform.localScale.x;
        Vector3 camPos = rb2d.transform.position - Camera.main.transform.position;
        float zAngle = Quaternion.LookRotation(camPos, Vector3.forward).eulerAngles.z + 90;

        if (assignedPlayerBall.LockSpeechBubbleAngle)
            zAngle = assignedPlayerBall.LockedSpeechBubbleAngle;

        float x = (BubbleContainer.transform.localScale.x + (currSeparationFromBall + parentScaleMag)) / 2 * Mathf.Cos(Mathf.Deg2Rad * zAngle);
        float y = (BubbleContainer.transform.localScale.y + (currSeparationFromBall + parentScaleMag)) / 2 * Mathf.Sin(Mathf.Deg2Rad * zAngle);
        Vector2 offset = new Vector3(x, y);
        BubbleContainer.transform.position = new Vector3((rb2d.transform.position + (Vector3)offset).x, (rb2d.transform.position + (Vector3)offset).y, BubbleContainer.transform.position.z); //Maintain the prefab container z position
        lineRenderer.SetPositions(new Vector3[] { BubbleContainer.transform.position, rb2d.transform.position });
        lineRenderer.startWidth = parentScaleMag * tailEndWidth;
    }

    private void SetTextBoundsWithinContainer()
    {
        RectTransform rt = BubbleText.rectTransform; //.GetComponent<RectTransform>();

        rt.position = new Vector3(BubbleContainer.transform.position.x, BubbleContainer.transform.position.y, rt.position.z);
        rt.sizeDelta = new Vector2(BubbleContainer.transform.localScale.x - padding.x, BubbleContainer.transform.localScale.y - padding.y);
    }
}