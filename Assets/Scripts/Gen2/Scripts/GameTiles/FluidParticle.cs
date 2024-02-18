using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum FluidType { lava, water}
public class FluidParticle : MonoBehaviour
{
    public FluidType Type;
    [SerializeField] public Rigidbody2D Rb2D;
    private FluidSpawner dbs;

    [SerializeField] private MeshRenderer _meshRenderer;
    [SerializeField] private PBEffector _pbEffector;
    //[SerializeField] private PBDetector _pbDetector;

    [SerializeField] private AudioSource _lavaSizzleDeathSound;

    [SerializeField] private float _lavaDrag = 10; 

    public void InitParticle(FluidSpawner _dbs, FluidType type, Color color)
    {
        dbs = _dbs;
        Type = type;

        Rb2D.drag = 0;

        if (type == FluidType.water)
        {
            gameObject.name = "waterParticle";
            _pbEffector.OverrideExplodeAudio = null;
        }
        else if(type == FluidType.lava)
        {
            gameObject.name = "lavaParticle";
            _pbEffector.OverrideExplodeAudio = _lavaSizzleDeathSound;
            Rb2D.drag = _lavaDrag;
        }

        _meshRenderer.material.SetColor("_EmissionColor", color);
        _meshRenderer.material.SetColor("_BaseColor", color);
    }

    // Update is called once per frame
    void Update()
    {
        if (Random.Range(0f, 1f) < (0.00025f))
            dbs.ReturnFluidParticleToPool(this);
    }

    private void HandleCollision(Collision2D collision)
    {
        if (collision == null || collision.gameObject == null || !gameObject.activeSelf)
            return;

        //If we hit another fluid particle, destroy water if lava, and lava if water
        if (collision.gameObject.layer == LayerMask.NameToLayer("Fluid"))
        {
            FluidParticle fp = collision.gameObject.GetComponent<FluidParticle>();
            if (fp == null)
                return;

            if (fp.Type == FluidType.water && Type == FluidType.lava
            || fp.Type == FluidType.lava && Type == FluidType.water)
            {
                dbs.ReturnFluidParticleToPool(this);
                AudioController.inst.PlaySound(AudioController.inst.DeathByLava, 0.75f, 0.9f);
                return;
            }
            return;
        }

        //If we hit cleaning bar, destroy
        if (collision.gameObject.layer == LayerMask.NameToLayer("CleaningBar"))
        {
            dbs.ReturnFluidParticleToPool(this);
            return;
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision);
    }

    public void PlayerEnter(PlayerBall pb)
    {
        if (pb == null)
            return;

        if (Type == FluidType.lava)
        {
            AudioController.inst.PlaySound(AudioController.inst.DeathByLava, 0.9f, 1.1f);
            dbs.ReturnFluidParticleToPool(this);
            _pbEffector.DetectedPB(pb);
            return;
        }
    }

}
