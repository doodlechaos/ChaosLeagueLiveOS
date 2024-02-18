using EZCameraShake;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using static UnityEngine.ParticleSystem;

/*public class SuperChatMultiplier : MonoBehaviour
{
    [SerializeField] private TileController _tileController;

    private List<SuperCapsule> _activeSuperCapsules = new List<SuperCapsule>();
    private List<SuperchatZone> _activeSuperchatZones = new List<SuperchatZone>();

    [SerializeField] private GameObject _superCapsulePrefab;
    [SerializeField] private GameObject _superchatZonePrefab;


    [SerializeField] private TextMeshPro _multiplierText;

    private ObjectPool<SuperCapsule> _superCapsulePool;
    private ObjectPool<SuperchatZone> _superchatZonePool;

    [SerializeField] private Transform _capsuleSpawnPos;

    //[SerializeField] private ParticleSystem _particleHype;

    [SerializeField] private Texture defaultPfp;
    [SerializeField] private Transform _capsulesRoot;
    [SerializeField] private Transform _zonesRoot;
    [SerializeField] private Transform _capsuleStackStartPos;

    [SerializeField] private float _capsuleMoveSpeed = 0.1f;

    public int Multiplier = 1;

    [SerializeField] private Vector3 _capsulePileSpacing;

    [SerializeField] private Vector2 _multiplierColorMap;
    [SerializeField] private Gradient _multiplierParticleMap;
    [SerializeField] private Gradient _multiplierTextMap;

    [SerializeField] private Vector2 _multiplierShakeMap;
    [SerializeField] private AnimationCurve _camShakeMagnitude;
    [SerializeField] private AnimationCurve _camShakeRoughness;
    [SerializeField] private AnimationCurve _camShakeFadeIn;
    [SerializeField] private AnimationCurve _camShakeFadeOut;
    [SerializeField] private AnimationCurve _capsuleImpactAudioPitch;


    public float TISpeed = 0.1f;

    private Queue<SuperCapsule> _capsuleSpawnQ = new Queue<SuperCapsule>();

    private void Awake()
    {
        //TODO: Init supercapsule pool
        _superCapsulePool = new ObjectPool<SuperCapsule>(SuperCapsuleFactory, CapsuleTurnOn, CapsuleTurnOff);
        _superchatZonePool = new ObjectPool<SuperchatZone>(SuperchatZoneFactory, SuperchatZoneTurnOn, SuperchatZoneTurnOff);
        UpdateMultiplier();
        StartCoroutine(CapsuleSpawner());
    }

    private SuperCapsule SuperCapsuleFactory()
    {
        return Instantiate(_superCapsulePrefab, _capsulesRoot).GetComponent<SuperCapsule>(); ;
    }

    private void CapsuleTurnOn(SuperCapsule sc)
    {
        sc.gameObject.SetActive(true);
    }
    private void CapsuleTurnOff(SuperCapsule sc)
    {
        sc.gameObject.SetActive(false);
    }

    private SuperchatZone SuperchatZoneFactory()
    {
        return Instantiate(_superchatZonePrefab, _zonesRoot).GetComponent<SuperchatZone>(); 
    }
    private void SuperchatZoneTurnOn(SuperchatZone sz)
    {
        sz.gameObject.SetActive(true);
    }
    private void SuperchatZoneTurnOff(SuperchatZone sz)
    {
        sz.gameObject.name = "pooledZone"; 
        //Can't turn off Zone parent gameobject, otherwise OnTriggerExit2D won't trigger on the PBEffectors
    }

    public void ReceiveSuperChat(PlayerHandler ph StreamEvent se)
    {
        SuperCapsule capsule = _superCapsulePool.GetObject();
        capsule.transform.position = _capsuleSpawnPos.position;

        Color bodyBackgroundColor = MyUtil.GetColorFromHex(se.body_background_colour, Color.white);
        Color headerBackgroundColor = MyUtil.GetColorFromHex(se.header_background_colour, Color.white);
        Color labelTextColor = MyUtil.GetColorFromHex(se.body_text_colour, Color.black);

        if (ph.isKing)
            MyTTS.inst.Announce("New Superchat from " + ph.pp.GetDisplayName());
        else
            MyTTS.inst.Announce("New Superchat from " + ph.pp.GetDisplayName() + ". " + se.censoredSpriteFormatMsg);

        int dollarAmount = Mathf.CeilToInt(se.dollarAmount);

        //Prevent them from despawning for the duration of the superchat, if they do despawn, then the texture will be lost on the super capsule
        int superCapsuleDuration = AppConfig.inst.GetI("CapsuleSecondsPerDollar") * dollarAmount;
        ph.InactivityTimer = -superCapsuleDuration;

        capsule.Init(this, ph, GetZoneFor(ph), labelTextColor, bodyBackgroundColor, headerBackgroundColor, dollarAmount, "$" + dollarAmount, superCapsuleDuration);
        _capsuleSpawnQ.Enqueue(capsule);
    }

    public IEnumerator SpawnNewCapsule(SuperCapsule capsule)
    {
        AudioController.inst.PlaySound(AudioController.inst.NewSuperChat, 0.9f, 1.1f);

        yield return capsule.RunSpawnAnimation();

        float t = capsule.DollarEquivalent / _multiplierShakeMap.y;
        CameraShaker.Instance.ShakeOnce(_camShakeMagnitude.Evaluate(t), _camShakeRoughness.Evaluate(t), _camShakeFadeIn.Evaluate(t), _camShakeFadeOut.Evaluate(t));
        float pitch = _capsuleImpactAudioPitch.Evaluate(t);
        AudioController.inst.PlaySound(AudioController.inst.CapsuleImpact, pitch, pitch);

        _activeSuperCapsules.Add(capsule);

        UpdateMultiplier();
    }

    public void DestroyCapsule(SuperCapsule capsule)
    {
        capsule.Zone.DecrementMultiplier(capsule.DollarEquivalent);
        capsule.Zone = null;

        _activeSuperCapsules.Remove(capsule);
        _superCapsulePool.ReturnObject(capsule);

        UpdateMultiplier();
    }

    public void DestroySuperchatZone(SuperchatZone zone)
    {
        zone.DisableZone();
        _activeSuperchatZones.Remove(zone);
        _superchatZonePool.ReturnObject(zone);
    }

    //Runs forever
    public IEnumerator CapsuleSpawner()
    {
        while (true)
        {

            while (_capsuleSpawnQ.Count > 0)
            {
                //Create text to speech announcing the superchat
                yield return SpawnNewCapsule(_capsuleSpawnQ.Dequeue());
            }
            yield return null;
        }
    }

    private void Update()
    {
        for (int i = 0; i < _activeSuperCapsules.Count; i++)
        {
            //Move each capsule towards its set index based on the number of super capsules
            SuperCapsule sc = _activeSuperCapsules[_activeSuperCapsules.Count - 1 - i];
            Vector3 nextPos = Vector3.MoveTowards(sc.transform.position, _capsuleStackStartPos.position + _capsulePileSpacing * i, _capsuleMoveSpeed * Time.deltaTime);
            sc.transform.position = nextPos;
        }
    }

    public SuperchatZone GetZoneFor(PlayerHandler ph)
    {
        foreach(SuperchatZone z in _activeSuperchatZones)
        {
            if (z.Ph.PlatformID == ph.PlatformID)
                return z;
        }

        SuperchatZone zone = _superchatZonePool.GetObject();
        zone.Init(this, ph);
        zone.name = ph.PlatformID + "_ZONE";
        _activeSuperchatZones.Add(zone);
        return zone;
    }
    public Vector3 GetTISpawnLocation()
    {
        return _multiplierText.transform.position;
    }

    private void UpdateMultiplier()
    {
        Multiplier = 0;
        //Move each capsule towards its set index based on the number of super capsules
        foreach (var superCapsule in _activeSuperCapsules)
        {
            Multiplier += superCapsule.DollarEquivalent;
        }

        if (Multiplier <= 0)
            Multiplier = 1;

        _multiplierText.SetText("x" + Multiplier);

        //MultiplierChangedEvent?.Invoke(Multiplier);
        foreach (var PBEffector in _tileController.GetPBEffectorsInActiveTiles())
            CreateMultiplierUpdateTI(PBEffector);

    }

    public void CreateMultiplierUpdateTI(PBEffector target)
    {
        //TextPopupMaster.Inst.CreateTravelingIndicator("x" + Multiplier, Multiplier, _multiplierText.transform.position, target, TISpeed, Color.white, null);
    }


}*/