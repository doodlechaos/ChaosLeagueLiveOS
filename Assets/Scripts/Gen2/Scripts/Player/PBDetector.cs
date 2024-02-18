using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PBDetector : MonoBehaviour
{

    [SerializeField] private bool _collision2DDetection = true;
    [SerializeField] private bool _trigger2DDetection = true;
    [SerializeField] private bool _collisionStayDetection = false;

    [Serializable] public class DetectedPbEvent : UnityEvent<PlayerBall> { }

    [SerializeField] DetectedPbEvent OnPbDetected;

    [SerializeField] private List<PlayerHandlerState> _statesToIgnore;
    [SerializeField] private bool _ignoreKinematicPlayers; 

    private Collider2D _collider2D;

    private void Awake()
    {
        _collider2D = GetComponent<Collider2D>();

        if (_collider2D == null)
            Debug.LogError("Failed to find _collider2D in " + gameObject.name); 
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_collision2DDetection)
            return;

        if (collision == null || collision.gameObject == null)
            return;

        if (collision.gameObject.layer != LayerMask.NameToLayer("Player"))
            return;

        PlayerBall pb = collision.transform.GetComponentInParent<PlayerBall>();


        if(_ignoreKinematicPlayers && pb._rb2D.isKinematic)
        {
            Physics2D.IgnoreCollision(collision.collider, collision.otherCollider);
            return;
        }

        foreach(var state in _statesToIgnore)
        {
            if (pb.Ph.GetState() == state)
            {
                Physics2D.IgnoreCollision(collision.collider, collision.otherCollider);
                return;
            }
        }

        if (pb == null)
            return;

        if (pb.IsExploding)
            return;

        OnPbDetected?.Invoke(pb);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!_collisionStayDetection)
            return;

        if (collision == null || collision.gameObject == null)
            return;

        if (collision.gameObject.layer != LayerMask.NameToLayer("Player"))
            return;

        PlayerBall pb = collision.transform.GetComponentInParent<PlayerBall>();


        if (_ignoreKinematicPlayers && pb._rb2D.isKinematic)
        {
            Physics2D.IgnoreCollision(collision.collider, collision.otherCollider);
            return;
        }

        foreach (var state in _statesToIgnore)
        {
            if (pb.Ph.GetState() == state)
            {
                Physics2D.IgnoreCollision(collision.collider, collision.otherCollider);
                return;
            }
        }

        if (pb == null)
            return;

        if (pb.IsExploding)
            return;

        OnPbDetected?.Invoke(pb);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_trigger2DDetection)
            return;

        if (collision == null || collision.gameObject == null)
            return;

        if (collision.gameObject.layer != LayerMask.NameToLayer("Player"))
            return;

        PlayerBall pb = collision.transform.GetComponentInParent<PlayerBall>();

        if (_ignoreKinematicPlayers && pb._rb2D.isKinematic)
        {
            Physics2D.IgnoreCollision(collision, _collider2D);
            return;
        }

        foreach (var state in _statesToIgnore)
        {
            if (pb.Ph.GetState() == state)
            {
                Physics2D.IgnoreCollision(collision, _collider2D);
                return;
            }

        }
        if (pb == null)
            return;

        if (pb.IsExploding)
            return;

        OnPbDetected?.Invoke(pb);

    }

}
