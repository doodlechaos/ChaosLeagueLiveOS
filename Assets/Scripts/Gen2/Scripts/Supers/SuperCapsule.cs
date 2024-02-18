using System.Collections;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using static UnityEngine.ParticleSystem;

public class SuperCapsule : MonoBehaviour
{
    private RebellionController _rm;
    public MultiplierZone Zone;

    [SerializeField] private TextMeshPro _labelText;
    [SerializeField] private MeshRenderer _pfpMeshRenderer;
    [SerializeField] private MeshRenderer _mainBody;
    [SerializeField] private Animation _animation;
    [SerializeField] private ParticleSystem _particleHype; 
    public PlayerHandler Ph;
    public int DollarEquivalent;

    private MaterialPropertyBlock _materialPropertyBlock;
    [SerializeField] private LineRenderer _lineToSCZone;

    private int _tilesRemaining; 

    public void Init(RebellionController scm,
        PlayerHandler ph,
        Texture pfp,
        MultiplierZone zone,
        int dollarEquivalent,
        string label,
        Vector2 dollarColorMap,
        Color bodyBackgroundColor,
        Color headerBackgroundColor,
        Color particlesColor)
    {
        _rm = scm;
        Zone = zone;

        Ph = ph;

        DollarEquivalent = dollarEquivalent;
        _tilesRemaining = dollarEquivalent; 

        _materialPropertyBlock = new MaterialPropertyBlock();

        _pfpMeshRenderer.material.mainTexture = pfp;

        Zone.IncrementMultiplier(dollarEquivalent);

        UpdateLabel(); 

        float t = Mathf.Clamp(dollarEquivalent, 1, dollarColorMap.y) / dollarColorMap.y;

        var ps_main = _particleHype.main;
        //Color mappedColor = _multiplierParticleMap.Evaluate(t);
        ps_main.startColor = particlesColor;

        MinMaxCurve newCurve = new MinMaxCurve();
        newCurve.constantMin = 1;
        newCurve.constantMax = Mathf.Lerp(1, 10, t);
        newCurve.mode = ParticleSystemCurveMode.TwoConstants;
        ps_main.startLifetime = newCurve;

        //Zone.SetColorParams(dollarColorMap, _dollarToColor, _dollarToTextColor); 

        _materialPropertyBlock.SetColor("_BodyBackgroundColor", bodyBackgroundColor);
        _materialPropertyBlock.SetColor("_HeaderBackgroundColor", headerBackgroundColor);
        _mainBody.SetPropertyBlock(_materialPropertyBlock);
        _lineToSCZone.material.color = MyUtil.SetColorSaveAlpha(bodyBackgroundColor, alpha:_lineToSCZone.material.color);

        //StartCoroutine(RunTimer(durationS));
    }

    public IEnumerator RunSpawnAnimation(Spline path, AnimationCurve speed, Transform capsuleStackStartPos)
    {
        float duration = speed.keys[speed.keys.Length - 1].time;
        Debug.Log("animation duration: " + duration); 
        float timer = 0;

        Vector3 rotateStartPos = path.Knots.ToList()[1].Position;

        while (timer <= duration)
        {
            float t = speed.Evaluate(timer);

            //Set the position of the last knot to the moving target
            var knot = path.Knots.Last();
            knot.Position = capsuleStackStartPos.position;
            path.SetKnot(path.Knots.Count() - 1, knot);

            float3 pos = path.EvaluatePosition(t);

            transform.position = pos;

            if(transform.position.z >= rotateStartPos.z)
            {
                float percentage = (rotateStartPos.z - transform.position.z) / (rotateStartPos.z - capsuleStackStartPos.position.z);

                float t2lerp = Mathf.Lerp(0, 1, percentage);

                float t2ease = EasingFunction.EaseInQuad(0, 1, percentage);

                //Debug.Log($"Percentage: {percentage} lerp: {t2lerp} ease: {t2ease}");


                float rotZ = Mathf.Lerp(0, -360, t2lerp);
                Vector3 angles = transform.eulerAngles;
                angles.z = rotZ;
                transform.eulerAngles = angles;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        transform.position = capsuleStackStartPos.position;
        transform.rotation = Quaternion.identity;

        _particleHype.Play();
    }


    private void Update()
    {
        if (Zone == null)
            return;

        //Draw a line from each capsule to its playerball
        _lineToSCZone.SetPosition(0, this.transform.position);
        _lineToSCZone.SetPosition(1, Zone.transform.position);
    }

/*    public IEnumerator RunTimer(int durationS)
    {
        int elapsedS = 0;
        while (elapsedS < durationS)
        {
            float t = elapsedS / (float)durationS;
            _materialPropertyBlock.SetFloat("_SliderPercentage", t);
            _mainBody.SetPropertyBlock(_materialPropertyBlock);

            yield return new WaitForSeconds(1);
            elapsedS++;
        }
        _rm.DestroyCapsule(this);
    }*/

    public void Decay()
    {
        _tilesRemaining--;

        UpdateLabel(); 

        if (_tilesRemaining < 0)
            _rm.DestroyCapsule(this); 
    }

    private void UpdateLabel()
    {
        float t = _tilesRemaining / (float)DollarEquivalent;
        _materialPropertyBlock.SetFloat("_SliderPercentage", 1 - t);
        _mainBody.SetPropertyBlock(_materialPropertyBlock);

        if(_tilesRemaining == 1)
            _labelText.SetText($"{_tilesRemaining} tile remains");
        else
            _labelText.SetText($"{_tilesRemaining} tiles remain");

        _labelText.color = Color.white;
    }

}