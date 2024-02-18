using TMPro;
using UnityEngine;

public class PipeReleaser : PlayerReceiveable
{
    [SerializeField] private GameTile tile; 
    [SerializeField] public SpriteRenderer LockIcon;
    [SerializeField] private float launchSpeedMin;
    [SerializeField] private float launchSpeedMax;
    [SerializeField] private float gravityOnExit = -1;
    [SerializeField] private Transform releasePos;

    [SerializeField] private TextMeshPro _pipeLabel;

    [SerializeField] private MeshRenderer _pipeRim;
    [SerializeField] private MeshRenderer _pipeBase;

    public int TargetNum; //equals negative 1 when locked
    [SerializeField] private bool overrideColorCycle;
    [SerializeField] private Color overrideColor;

    [SerializeField] private bool _enableToll = false;
    [SerializeField] private TextMeshPro _tollText;
    [SerializeField] private MeshRenderer _tollBackground; 
    private int _tollCost = 0; 

    public bool IsLocked = true;

    private Side _side;
    private KingController _kingController;

    public void Awake()
    {
        if(_tollText != null)
            _tollText.enabled = _enableToll;
        if(_tollBackground != null)
            _tollBackground.enabled = _enableToll;

        if (overrideColorCycle)
            SetPipeColor(overrideColor);
        else
            SetPipeColor(Random.ColorHSV(0, 1, 1, 1, 0.9f, 0.9f));


        TargetNum = -1;
    }

    public void InitPipeReleaser(TileController tc, int targetNum, Side side, KingController kingController)
    {
        _kingController = kingController;
        _side = side;

/*
        if (!overrideColorCycle)
            SetPipeColor(tc.GetNextPipeColor());*/

        UnlockPipe(targetNum);
    }

    private void SetPipeColor(Color color)
    {
        _pipeRim.material.color = color;
        _pipeBase.material.color = color;
    }
    public Color GetPipeColor()
    {
        return _pipeRim.material.color;
    }

    public void SetTollCost(int cost)
    {
        _tollCost = cost;
        _tollText.gameObject.SetActive(true);
        _tollText.SetText(cost.ToString()); 
    }

    public override void ReceivePlayer(PlayerBall pb)
    {
        LaunchPlayer(pb);
    }

    public override void ReceiveDeathBall(DeathBall db)
    {
        AudioController.inst.PlaySound(AudioController.inst.ReleasePlayer, 0.85f, 1.15f);
        db._rb.transform.position = releasePos.position;
        db._rb.velocity = transform.up * Random.Range(launchSpeedMin, launchSpeedMax) + (transform.right * Random.Range(-0.001f, 0.001f)); //Also adds a tiny amount of random side to side vel to avoid stacking
        db._rb.gravityScale = gravityOnExit;
    }

    public override Vector3 GetReceivePosition()
    {
        return releasePos.position; 
    }

    public void LaunchPlayer(PlayerBall pb)
    {
        if(tile != null)
            tile.OnPipeReleasePlayer(pb); 

        AudioController.inst.PlaySound(AudioController.inst.ReleasePlayer, 0.85f, 1.15f);
        pb.EnableDynamicPhysicsMode(); 

        //Set the player position to the middle of the pipe
        pb._rb2D.transform.position = releasePos.position; 
        //pb._rb2D.MovePosition(releasePos.position);

        pb._rb2D.velocity = transform.up * Random.Range(launchSpeedMin, launchSpeedMax) + (transform.right * Random.Range(-0.001f, 0.001f)); //Also adds a tiny amount of random side to side vel to avoid stacking
        pb._rb2D.gravityScale = gravityOnExit;

        pb.Reactivate();

        if (_enableToll)
            pb.PayToll(_tollCost); 

        if (tile == null)
            return;

        if (!tile.AlivePlayers.Contains(pb.Ph))
        {
            pb.Ph.ResetRankScore();
            tile.AlivePlayers.Add(pb.Ph);
            //tile.Players.Add(pb.Ph);
        }

        tile.ConveyorBelt.Remove(pb.Ph); 
    }

    public void UnlockPipe(int targetNum)
    {
        TargetNum = targetNum;
        _pipeLabel.SetText("!" + targetNum);

        LockIcon.enabled = false;
        _pipeLabel.enabled = true;
        IsLocked = false;
    }
    public void LockPipe()
    {
        LockIcon.enabled = true;
        _pipeLabel.enabled = false;

        TargetNum = -1;
        IsLocked = true; 
    }

}
