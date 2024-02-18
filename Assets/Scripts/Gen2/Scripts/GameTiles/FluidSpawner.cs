using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FluidSpawner : MonoBehaviour
{
    public GameObject lavaPrefab; 
    public Transform LavaReleasePosition; 
    
    [SerializeField] private bool testEnqueueFluidButton;
    [SerializeField] private FluidType testFluidType;

    [SerializeField] private PBEffect _lavaEffect;
    [SerializeField] private PBEffect _waterEffect;

    [SerializeField] private int particlesPerLavaBucket = 130;

    [SerializeField] private float lavaGravity = 1f;

    [SerializeField] private Vector3 lavaSpawnVelocity; 

    private ObjectPool<FluidParticle> _lavaParticlePool;
    private Queue<FluidParticle> _lavaParticleSpawnQueue = new Queue<FluidParticle>();

    [ColorUsage(true, true)]
    [SerializeField] private Color _lavaColor;
    [ColorUsage(true, true)]
    [SerializeField] private Color _waterColor;

    public AudioClip releaseLavaSound;

    private void Start()
    {
        _lavaParticlePool = new ObjectPool<FluidParticle>(FluidParticleFactory, TurnOnFluidParticle, TurnOffFluidParticle);

        LavaReleasePosition.GetComponent<SpriteRenderer>().enabled = false;
    }

    private void OnValidate()
    {
        if (testEnqueueFluidButton)
        {
            testEnqueueFluidButton = false;
            LoadFluidBucket(testFluidType);
        }
    }

    public void LoadLavaBucket()
    {
        LoadFluidBucket(FluidType.lava); 
    }

    public void LoadWaterBucket()
    {
        LoadFluidBucket(FluidType.water);
    }

    private void LoadFluidBucket(FluidType fluidType)
    {
        AudioController.inst.PlaySound(AudioController.inst.LavaDump, 0.95f, 1.05f); 
        for(int i = 0; i < particlesPerLavaBucket; i++)
        {
            FluidParticle lp = _lavaParticlePool.GetObject();

            lp.InitParticle(this, fluidType, (fluidType == FluidType.lava) ? _lavaColor : _waterColor);

            _lavaParticleSpawnQueue.Enqueue(lp); 
        }
    }

    
    private void ReleaseFluidParticle(FluidParticle lp)
    {
        lp.gameObject.SetActive(true);
        lp.transform.position = new Vector3(LavaReleasePosition.position.x, LavaReleasePosition.position.y, LavaReleasePosition.position.z);
        lp.Rb2D.velocity = lavaSpawnVelocity; 
        lp.Rb2D.gravityScale = lavaGravity; 
    }
    public void ReturnFluidParticleToPool(FluidParticle lp)
    {
        _lavaParticlePool.ReturnObject(lp);
    }
    

    private FluidParticle FluidParticleFactory()
    {
        GameObject newLP = Instantiate(lavaPrefab);
        newLP.transform.SetParent(this.transform);
        FluidParticle lp = newLP.GetComponent<FluidParticle>();
        //lp.InitParticle(this, _lavaEffect, FluidType.lava, _lavaColor);
        newLP.gameObject.SetActive(false);
        newLP.transform.position = Vector3.up * 100;
        return lp;
    }

    private void TurnOnFluidParticle(FluidParticle lp)
    {
        //lp.InitParticle(this, FluidType.lava, _lavaColor);
        lp.transform.position = Vector3.up * 100;
    }

    private void TurnOffFluidParticle(FluidParticle lp)
    {
        lp.transform.position = Vector3.up * 100;
        lp.gameObject.SetActive(false);
    }

    

    // Update is called once per frame
    void Update()
    {
        
        if (_lavaParticleSpawnQueue.Count > 0)
            ReleaseFluidParticle(_lavaParticleSpawnQueue.Dequeue());
       
    }
}
