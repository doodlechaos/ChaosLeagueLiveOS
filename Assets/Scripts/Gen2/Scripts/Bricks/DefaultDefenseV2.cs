using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum DefaultDefenseMode { Ordered, Random }

public enum DefenseBrickType { Dirt, Coal, Iron, Diamond, Obsidian, GoldOre}

public class DefaultDefenseV2 : MonoBehaviour
{
    [SerializeField] private GameObject _brickPrefab;
    [SerializeField] private Transform _bottomLeftCorner;
    [SerializeField] private Transform _topRightCorner;
    [SerializeField] private GoldDistributor _goldDistributor;


    [SerializeField] private DefaultDefenseMode _mode;

    [SerializeField] private int _rows;
    [SerializeField] private int _columns;
    [SerializeField] private float _cellMargin = 0.95f;

    private DefenseBrick[,] _defaultDefenseBricks;

    [SerializeField] private List<Material> _materials = new List<Material>();

    [SerializeField] private Color _brickLabelTextColor;


    public int ExponentScale = 1;

    // Start is called before the first frame update
    void Start()
    {
        _defaultDefenseBricks = new DefenseBrick[_columns, _rows];
        CreateGrid();
        ResetDefense(_mode);

        //Hide the corner markers during gameplay
        _bottomLeftCorner.GetComponent<MeshRenderer>().enabled = false;
        _topRightCorner.GetComponent<MeshRenderer>().enabled = false;
    }

    private void CreateGrid()
    {
        Vector3 topRightPos = _topRightCorner.position;
        Vector3 bottomLeftPos = _bottomLeftCorner.position;

        Vector3 gridDimensions = topRightPos - bottomLeftPos;
        Vector3 cellSize = new Vector3(gridDimensions.x / (_columns - 1), gridDimensions.y / (_rows - 1), _brickPrefab.transform.localScale.z);

        for (int row = 0; row < _rows; row++)
        {
            for (int col = 0; col < _columns; col++)
            {
                Vector3 cellPosition = bottomLeftPos + new Vector3(col * cellSize.x, row * cellSize.y, 0f);
                GameObject newBrick = Instantiate(_brickPrefab, cellPosition, Quaternion.identity);
                DefenseBrick db = newBrick.GetComponent<DefenseBrick>();
                _defaultDefenseBricks[col, row] = db;

                db.Rb2D.isKinematic = true;


                newBrick.transform.localScale = new Vector3(cellSize.x * _cellMargin, cellSize.y * _cellMargin, cellSize.z);
                newBrick.transform.SetParent(transform);
            }
        }
    }

    public DefenseBrick GetRandomBrick()
    {
        int randXIndex = Random.Range(0, _defaultDefenseBricks.GetLength(0));
        int randYIndex = Random.Range(0, _defaultDefenseBricks.GetLength(1));

        return _defaultDefenseBricks[randXIndex, randYIndex];
    }

    public void ResetDefense(DefaultDefenseMode mode)
    {
        List<int> oreTypes = new List<int>();
        for (int i = 0; i < _columns; i++)
        {
            for (int j = 0; j < _rows; j++)
            {
                oreTypes.Add(j);
            }
        }

        for (int col = 0; col < _columns; col++)
        {
            for (int row = 0; row < _rows; row++)
            {
                int randomIndex = Random.Range(0, oreTypes.Count - 1);
                int oreType = oreTypes[randomIndex];//oreTypes[row + col];
                oreTypes.RemoveAt(randomIndex); 
                //Get the next brick type from the list
                Material blockMaterial = _materials[oreType];

                int blockHP = (int)Mathf.Pow(2, ExponentScale + oreType);

                _defaultDefenseBricks[col, row].InitBrick(null, this, (DefenseBrickType)oreType, blockHP, blockMaterial, _brickLabelTextColor); 
            }
        }
    }

    public void AddBonusDefense(long totalAmount, PlayerHandler ph)
    {
        Debug.Log("adding bonus defense");
        long pointsPerBrick = totalAmount / _columns;

        if(pointsPerBrick <= 0)
            pointsPerBrick = 1;

        ph.SubtractPoints(totalAmount, canKill: false, createTextPopup: true);

        int TI_Count = 0; 
        //Split the points up evenly among the obsidian
        for (int col = 0; col < _columns; col++)
        {
            for (int row = 0; row < _rows; row++)
            {
                DefenseBrick db = _defaultDefenseBricks[col, row];
                if (db.GetBrickType() == DefenseBrickType.Obsidian)
                {
                    TextPopupMaster.Inst.CreateTravelingIndicator(MyUtil.AbbreviateNum4Char(pointsPerBrick), pointsPerBrick, ph, db, 0.06f, Color.white, ph.PfpTexture);
                    TI_Count++;

                    if (TI_Count >= totalAmount)
                        return;
                }
            }
        }
        pointsPerBrick = (pointsPerBrick / 10);

        TI_Count = 0;
        for (int col = 0; col < _columns; col++)
        {
            for (int row = 0; row < _rows; row++)
            {
                DefenseBrick db = _defaultDefenseBricks[col, row];
                if (db.GetBrickType() == DefenseBrickType.Diamond)
                {
                    TextPopupMaster.Inst.CreateTravelingIndicator(MyUtil.AbbreviateNum4Char(pointsPerBrick), pointsPerBrick, ph, db, 0.06f, Color.white, ph.PfpTexture);
                    TI_Count++;

                    if (TI_Count >= totalAmount)
                        return;
                }
            }
        }
        pointsPerBrick = (pointsPerBrick / 10);

        TI_Count = 0;
        for (int col = 0; col < _columns; col++)
        {
            for (int row = 0; row < _rows; row++)
            {
                DefenseBrick db = _defaultDefenseBricks[col, row];
                if (db.GetBrickType() == DefenseBrickType.Iron)
                {
                    TextPopupMaster.Inst.CreateTravelingIndicator(MyUtil.AbbreviateNum4Char(pointsPerBrick), pointsPerBrick, ph, db, 0.06f, Color.white, ph.PfpTexture);
                    TI_Count++;

                    if (TI_Count >= totalAmount)
                        return;
                }
            }
        }
        pointsPerBrick = (pointsPerBrick / 10);

        TI_Count = 0;
        for (int col = 0; col < _columns; col++)
        {
            for (int row = 0; row < _rows; row++)
            {
                DefenseBrick db = _defaultDefenseBricks[col, row];
                if (db.GetBrickType() == DefenseBrickType.Coal)
                {
                    TextPopupMaster.Inst.CreateTravelingIndicator(MyUtil.AbbreviateNum4Char(pointsPerBrick), pointsPerBrick, ph, db, 0.06f, Color.white, ph.PfpTexture);
                    TI_Count++;

                    if (TI_Count >= totalAmount)
                        return;
                }
            }
        }
    
        pointsPerBrick = (pointsPerBrick / 10);

        TI_Count = 0;
        for (int col = 0; col<_columns; col++)
        {
            for (int row = 0; row<_rows; row++)
            {
                DefenseBrick db = _defaultDefenseBricks[col, row];
                if (db.GetBrickType() == DefenseBrickType.Dirt)
                {
                    TextPopupMaster.Inst.CreateTravelingIndicator(MyUtil.AbbreviateNum4Char(pointsPerBrick), pointsPerBrick, ph, db, 0.06f, Color.white, ph.PfpTexture);
                    TI_Count++;

                    if (TI_Count >= totalAmount)
                        return;
                }
            }
        }
    }

    public void GoldBreakSignal(PlayerBall pb, Vector3 pos, int coins)
    {
        _goldDistributor.SpawnGoldFromDefenseBrick(pos, coins, pb.Ph);
    }

}
