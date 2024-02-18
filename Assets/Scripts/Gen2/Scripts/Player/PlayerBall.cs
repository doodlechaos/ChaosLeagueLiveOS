using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerBall : MonoBehaviour
{
    public PlayerHandler Ph;
    public PBHologram pbh;

    private GameManager _gm; 

    [field: SerializeField] public TextMeshPro _usernameText { get; private set; }
    [field: SerializeField] public TextMeshPro _usernameBackgroundHighlight { get; private set; }
    [field: SerializeField] public TextMeshPro _pointsText { get; private set; }
    [field: SerializeField] public TextMeshPro _ticketCountText { get; private set; }
    [field: SerializeField] public GameObject _ticketDisplayRoot { get; private set; }

    [field: SerializeField] public MeshRenderer _mainBody { get; private set; }
    [field: SerializeField] public Rigidbody2D _rb2D { get; private set; }
    [field: SerializeField] public MeshRenderer _colorRing { get; private set; }

    [field: SerializeField] public SpeechBubbleControllerV2 _sbcV2 { get; private set; }

    [field: SerializeField] public ParticleSystem _ps;
    [field: SerializeField] public TrailRenderer _trailRenderer { get; private set; }
    [field: SerializeField] public MeshRenderer _inviterIndicator { get; private set; }

    [SerializeField] private SpriteRenderer _communityPointSprite; 

    [SerializeField] private Vector3 _defaultRbScale;
    [SerializeField] private Vector3 _finalExplosionScale;
    [SerializeField] private float _explodeDuration;
    [SerializeField] private float _dragInFluids;
    [SerializeField] public float MaxTomatoKickbackForce = 10f; 

    public List<MultiplierZone> OverlappingZones = new List<MultiplierZone>();
    public List<FluidParticle> OverlappingFluids = new List<FluidParticle>();

    public bool LockSpeechBubbleAngle;
    public float LockedSpeechBubbleAngle;

    [SerializeField] private bool _explodeBallButton = false;
    public bool IsExploding;

    private MaterialPropertyBlock matBlock;
    private TextMeshPro _currPointsText;

    private List<(Vector2 pos, float speed, Action action)> _waypoints = new List<(Vector2 pos, float speed, Action action)>();

    public void InitPB(GameManager gm, PlayerHandler ph)
    {
        _gm = gm;

        matBlock = new MaterialPropertyBlock();
        _currPointsText = _pointsText;
        if(ph == null)
        {
            Debug.LogError("Failed to init player ball with null player handler");
            return;
        }

        Ph = ph;
        _communityPointSprite.sprite = gm.CommunityPointSprite; 

        //Just in case it died before it could be unfrozen 
        _rb2D.constraints = RigidbodyConstraints2D.None;

        _mainBody.material.SetColor("_BaseColor", Color.white);

        ph.SetBallCustomizations();

        SetBallTexture(); 

        _rb2D.transform.eulerAngles = new Vector3(90, 0, 0);
        _rb2D.transform.localScale = _defaultRbScale; 

        if (_sbcV2 != null)
            _sbcV2.InitSpeechBubbleController(_gm, this);

        IsExploding = false;
        _rb2D.simulated = true;

        EnableKinematicMode(); 

        _rb2D.drag = 0;

        _rb2D.angularDrag = 0.05f; 
        _colorRing.enabled = true;
        LockSpeechBubbleAngle = false;
        LockedSpeechBubbleAngle = 0;
        _usernameText.enabled = true;
        _usernameBackgroundHighlight.enabled = true;

        var collider = GetComponentInChildren<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
        }
        else
        {
            Debug.Log($"No collider2D found in {ph.pp.TwitchUsername} pb");
        }

        _sbcV2.lineRenderer.enabled = true;

        ResetAndUpdatePointsTextTarget();
        UpdateBidCountText();

        StartCoroutine(UpdateInviterIndicator());

        OverlappingZones.Clear();
        UpdateRingColor();

        OverlappingFluids.Clear();
        UpdateFluidDrag();
    }

    private void OnValidate()
    {
        if (_explodeBallButton)
        {
            _explodeBallButton = false;
            ExplodeBall(); 
        }
    }

    public void SetBallTexture()
    {
        if (Ph.PfpTexture != null)
        {
            _mainBody.material.mainTexture = Ph.PfpTexture;
            Ph.pbh.MainBody.material.mainTexture = Ph.PfpTexture;
        }
        else
            StartCoroutine(Ph.LoadBallPfp());
    }


    //Do this to avoid deactivating the main playerball script so that the coroutines can still run
    public void TempDeactivate()
    {
        _rb2D.gameObject.SetActive(false);
        _usernameText.gameObject.SetActive(false);
        _pointsText.gameObject.SetActive(false);
        _sbcV2.gameObject.SetActive(false);
    }
    public void Reactivate()
    {
        _rb2D.gameObject.SetActive(true);
        _usernameText.gameObject.SetActive(true);
        _pointsText.gameObject.SetActive(true);
        _sbcV2.gameObject.SetActive(true);
    }

    public void EnableKinematicMode()
    {
        _rb2D.simulated = true; 
        _rb2D.isKinematic = true;
        _rb2D.velocity = Vector2.zero;
        _rb2D.angularVelocity = 0;
    }

    public void EnableDynamicPhysicsMode()
    {
        _rb2D.simulated = true;
        _rb2D.isKinematic = false;
    }

    public void Update()
    {
        if (IsExploding)
            return;

        if (_usernameText != null)
            _usernameText.transform.position = Ph.GetBallPos() + _gm.UsernameOffset;

        if (_pointsText != null)
            _pointsText.transform.position = Ph.GetBallPos() + _gm.PointsTextOffset;

        if(_waypoints.Count > 0)
        {
            Vector2 nextPos = Vector2.MoveTowards(_rb2D.transform.position, _waypoints[0].pos, _waypoints[0].speed);
            SetPosition(nextPos);
            EnableKinematicMode(); 
            //_rb2D.simulated = false; //Keep setting in case I click and drag on the ball
            if (Vector2.Distance(_rb2D.transform.position, _waypoints[0].pos) < 0.1f)
            {
                if (_waypoints[0].action != null)
                    _waypoints[0].action(); 
                _waypoints.RemoveAt(0);
            }
            return;
        }

        if (Ph.ReceivableTarget != null)
        {
            SetPosition(Vector2.MoveTowards(_rb2D.transform.position, Ph.ReceivableTarget.GetReceivePosition(), 0.1f));
            EnableKinematicMode();
            //_rb2D.simulated = false; //Keep setting in case I click and drag on the ball
            if (Vector2.Distance(_rb2D.transform.position, Ph.ReceivableTarget.GetReceivePosition()) < 0.1f)
            {
                Ph.ReceivableTarget.ReceivePlayer(this);
                
                Ph.ReceivableTarget = null;
            }
            return;
        }
    }
    public void ExplodeBall(bool implode = false)
    {
        StartCoroutine(CExplodeBall(implode)); 
    }
    private IEnumerator CExplodeBall(bool implode)
    {
        if (IsExploding)
            yield break;

        //Clear any targets the player had before they exploded
        _waypoints.Clear();
        Ph.ReceivableTarget = null; 

        IsExploding = true;

        _rb2D.simulated = false;

        //Move the text off screen, because when re-enabled, they need one frame to catch up
        Vector3 holdingPenPos = _gm.HoldingPen.Get_TI_IO_Position();
        _sbcV2.transform.position = holdingPenPos;
        _usernameText.transform.position = holdingPenPos;
        _pointsText.transform.position = holdingPenPos;

        _sbcV2.lineRenderer.enabled = false;
        _sbcV2.lineRenderer.SetPositions(new Vector3[] { holdingPenPos, holdingPenPos });

        _colorRing.enabled = false;

        float timer = 0;
        while (timer < _explodeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / _explodeDuration;
            Vector3 scale = Vector3.Lerp(_defaultRbScale, (implode) ? Vector3.zero : _finalExplosionScale, t);;
            _rb2D.transform.localScale = scale;
            _mainBody.material.SetColor("_BaseColor", Color.Lerp(Color.white, Color.clear, t));
            yield return null;
        }

        _rb2D.transform.localScale = _defaultRbScale; //Reset

        IsExploding = false;
        _gm.DestroyPlayerBall(this);
        yield return null;
    }

    public void SetupAsKing(Vector3 kingScale)
    {
        //_animation.clip = _king;
        _rb2D.transform.localScale = kingScale;

        //Replace the crowned character
        EnableKinematicMode();
        GetComponentInChildren<Collider2D>().enabled = false;

        Ph.SetState(PlayerHandlerState.King); 
        _colorRing.enabled = false;

        //Disable the displayName and points text
        _usernameText.enabled = false;
        _usernameBackgroundHighlight.enabled = false; 
        _currPointsText.enabled = false;
        _inviterIndicator.enabled = false;

        _sbcV2.transform.localScale = new Vector3(0.7f, 0.7f, 1);
    }

    public void SpinY()
    {
        StartCoroutine(SpinY_C(1));
    }
    private IEnumerator SpinY_C(float duration)
    {
        float timer = 0;
        while(timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            float rot = Mathf.Lerp(0, 360, t);
            _rb2D.transform.eulerAngles = new Vector3(90, rot, 0);

            yield return null;
        }
    }
    public void SetPosition(Vector3 pos)
    {
        //_rb2D.MovePosition(pos); 
        pos.z = 0; 
        _rb2D.transform.position = pos;
    }

    public Vector3 GetPosition()
    {
        return _rb2D.transform.position;
    }
    public long GetPoints()
    {
        return Ph.pp.SessionScore;
    }
    public void OverridePointsTextTarget(TextMeshPro tmp)
    {
        _currPointsText = tmp;
        UpdatePointsText();
    }
    public void ResetAndUpdatePointsTextTarget()
    {
        _currPointsText = _pointsText;
        _currPointsText.enabled = true;

        UpdatePointsText();
    }
    public void UpdateBidCountText()
    {
        if(Ph.pp.CurrentBid <= 0 || Ph.IsKing())
        {
            _ticketDisplayRoot.SetActive(false);
            return;
        }

        _ticketDisplayRoot.SetActive(true); 

        _ticketCountText.SetText(MyUtil.AbbreviateNum4Char(Ph.pp.CurrentBid));
    }
    public void UpdatePointsText()
    {
        if(Ph.IsKing())
            _currPointsText.SetText(MyUtil.AbbreviateNum4Char(Ph.pp.SessionScore) + " Points");
        else
            _currPointsText.SetText(MyUtil.AbbreviateNum4Char(Ph.pp.SessionScore));
    }

    public IEnumerator UpdateInviterIndicator()
    {
        //If the player has no _recuitedbyId set, hide the indicator
        if (string.IsNullOrEmpty(Ph.pp.InvitedByID))
        {
            _inviterIndicator.material.mainTexture = null;
            _inviterIndicator.enabled = false;

            //mirror to hologram
            Ph.pbh.InviterIndicator.material.mainTexture = null;
            Ph.pbh.InviterIndicator.enabled = false;
            yield break;
        }

        //Get the inviter Ph
        CoroutineResult<PlayerHandler> coResult = new CoroutineResult<PlayerHandler>();
        yield return _gm.GetPlayerHandler(Ph.pp.InvitedByID, coResult);
        PlayerHandler inviterPh = coResult.Result;

        if(inviterPh == null)
        {
            Debug.LogError($"Failed to find ph for invitedbyId: {Ph.pp.InvitedByID}");
            yield break;
        }

        if (inviterPh.PfpTexture == null)
        {
            Debug.Log("Recuiter pfp texture is null. Loading it now"); 
            yield return inviterPh.LoadBallPfp();
        }

        _inviterIndicator.material.mainTexture = inviterPh.PfpTexture;
        _inviterIndicator.enabled = true;

        //mirror to hologram
        Ph.pbh.InviterIndicator.material.mainTexture = inviterPh.PfpTexture;
        Ph.pbh.InviterIndicator.enabled = true; 
    }

    public void SetRingColor(Color color)
    {
        SpinY();
        //_colorRing.SetPropertyBlock(matBlock);
        _colorRing.material.SetColor("_BaseColor", color);
    }

    public void PayToll(int tollRate)
    {
        if (tollRate > 0)
        {
            long payment = tollRate;
            if (Ph.pp.SessionScore < tollRate)
                payment = Ph.pp.SessionScore;

            if (payment <= 0)
                return;

            Ph.SubtractPoints(payment, false, true);
            TextPopupMaster.Inst.CreateTravelingIndicator(payment.ToString(), payment, Ph, Ph.GetGameManager().GetKingController().currentKing.Ph, 0.1f, Color.red, null);
        }
    }


    public void AddPriorityWaypoint(Vector3 target, float travelSpeed, Action action = null)
    {
        _waypoints.Add((target, travelSpeed, action)); 
    }

    public void OnZoneEnter(object collider2D)
    {
        Collider2D col = collider2D as Collider2D;

        if (col == null)
            return;

        MultiplierZone zone = col.gameObject.GetComponentInParent<MultiplierZone>();
        if (zone == null)
            return;

        OverlappingZones.Add(zone);
        UpdateRingColor();
    }

    public void OnZoneExit(object collider2D)
    {
        Collider2D col = collider2D as Collider2D;

        if (col == null)
            return;

        MultiplierZone zone = col.gameObject.GetComponentInParent<MultiplierZone>();
        if (zone == null)
            return;

        OverlappingZones.Remove(zone);
        UpdateRingColor();
    }

    public void OnFluidEnter(object collider2D)
    {
        if (IsExploding)
            return;

        if (Ph.State != PlayerHandlerState.Gameplay)
            return;

        //Balls are switched from biddinq state to gameplay state immediately once they're on the conveyor belt, so must check this
        GameTile gameplayTile = Ph.GetGameManager().GetTileController().GameplayTile;
        if (gameplayTile != null && gameplayTile.ConveyorBelt.Contains(Ph))
            return;

        Collider2D col = collider2D as Collider2D;

        if (col == null)
            return;

        FluidParticle fp = col.gameObject.GetComponent<FluidParticle>();
        if (fp == null)
            return;

        OverlappingFluids.Add(fp);
        UpdateFluidDrag();

        fp.PlayerEnter(this); 
    }

    public void OnFluidExit(object collider2D)
    {
        if (IsExploding)
            return;

        if (Ph.State != PlayerHandlerState.Gameplay)
            return;

        //Balls are switched from biddinq state to gameplay state immediately once they're on the conveyor belt, so must check this
        GameTile gameplayTile = Ph.GetGameManager().GetTileController().GameplayTile;
        if (gameplayTile != null && gameplayTile.ConveyorBelt.Contains(Ph))
            return;

        Collider2D col = collider2D as Collider2D;

        if (col == null)
            return;

        FluidParticle fp = col.gameObject.GetComponent<FluidParticle>();
        if (fp == null)
            return;

        OverlappingFluids.Remove(fp);
        UpdateFluidDrag();
    }

    private void UpdateFluidDrag()
    {
        if (OverlappingFluids.Count > 0)
            _rb2D.drag = _dragInFluids;
        else
            _rb2D.drag = 0; 
    }

    private void UpdateRingColor()
    {
        if (OverlappingZones.Count > 0)
            _colorRing.material.color = Color.white;
        else
            _colorRing.material.color = Color.gray;
    }
}
