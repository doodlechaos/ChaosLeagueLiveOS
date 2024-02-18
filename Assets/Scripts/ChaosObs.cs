using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ChaosObsType { t1, t2, t3, dud}
public class ChaosObs : MonoBehaviour, TravelingIndicatorIO
{
    [SerializeField] private PBEffector _square;
    [SerializeField] private PBEffector _circle;

    [SerializeField] private BoxCollider2D _squarePbCollider;
    [SerializeField] private CircleCollider2D _circlePbCollider;

    [SerializeField] private ChaosObsType _testType;
    [SerializeField] private bool _testInit;
    [SerializeField] private Animation _animation;
    [SerializeField] private ParticleSystem _ps;
    [SerializeField] private LayerMask _tempAnimateLayer;
    [SerializeField] private LayerMask _obstacleLayer;

    private IObsSpawner _spawner;

    private void OnValidate()
    {
        if (_testInit)
        {
            _testInit = false;
            float radius = GetRadiusOfType(_testType);
            Init(RarityType.Epic, null, _testType, radius); 
        }
    }

    public static float GetRadiusOfType(ChaosObsType obsType)
    {
        float radius = Random.Range(0.4f, 1f);

        //Set the radius based on the tier
        if (obsType == ChaosObsType.t1)
            radius = 0.7f;
        else if (obsType == ChaosObsType.t2)
            radius = 0.505f;
        else if (obsType == ChaosObsType.t3)
            radius = 0.4f;

        return radius;
    }

    public void Init(RarityType tileRarity, IObsSpawner obsSpawner, ChaosObsType type, float radius)
    {
        _spawner = obsSpawner;

        if (type == ChaosObsType.dud)
        {
            _square.gameObject.SetActive(true);
            _circle.gameObject.SetActive(false);

            _square.Init(PBEffect.None, 0, 10);
            _square.transform.localScale = new Vector3(radius * 2, radius * 2, _square.transform.localScale.z);
            _square.transform.eulerAngles = new Vector3(0, 0, Random.Range(0, 360)); 
        }
        else
        {
            //_circle.SetCurrValue((int)type + 1);
            _square.gameObject.SetActive(false);
            _circle.gameObject.SetActive(true);
            _circle.transform.localScale = new Vector3(radius * 2, radius * 2, _circle.transform.localScale.z);
            if(type == ChaosObsType.t1)
            {
                _circle.Init(PBEffect.Add, 1 * AppConfig.GetMult(tileRarity), 20);
            }
            else if(type == ChaosObsType.t2)    
            {
                _circle.Init(PBEffect.Add, 2 * AppConfig.GetMult(tileRarity), 10);
            }
            else if( type == ChaosObsType.t3)
            {
                _circle.Init(PBEffect.Add, 3 * AppConfig.GetMult(tileRarity), 5);
            }
        }

        TogglePbColliders(false);


        StartCoroutine(RunSpawnAnimation()); 
    }

    private Color GetMeshColor()
    {
        if (_square.gameObject.activeSelf)
        {
            Color color = _square.GetMeshColor();
            //Debug.Log($"Got square color in {this.name} {color.ColorToHexString()}");
            return color;
        }

        if (_circle.gameObject.activeSelf)
        {
            Color color = _circle.GetMeshColor();
            //Debug.Log($"Got circle color in {this.name} {color.ColorToHexString()}");
            return color;
        }

        return Color.blue; 
    }

    IEnumerator RunSpawnAnimation()
    {
        _animation.Play(); 
        yield return new WaitForSeconds(_animation.clip.length);

        TogglePbColliders(true); 

        var main = _ps.main;
        main.startColor = GetMeshColor();
        _ps.Play();
    }

    private void TogglePbColliders(bool b)
    {
        _squarePbCollider.enabled = b;
        _circlePbCollider.enabled = b;
    }

    public void ReturnToPool()
    {
        Debug.Log("returning obs to pool in obs");
        _spawner.ReturnObsToPool(this); 
    }

    public void ReceiveTravelingIndicator(TravelingIndicator TI)
    {
        if (TI.TI_Type == TI_Type.Multiply)
        {
            MultiplyCurrValue((int)TI.value);
        }
    }

    public void MultiplyCurrValue(int amount)
    {
        _circle.MultiplyCurrValue(amount);
    }

    public Vector3 Get_TI_IO_Position()
    {
        return transform.position;
    }

/*    public GameObject GetGameObject()
    {
        return gameObject;
    }*/
}
