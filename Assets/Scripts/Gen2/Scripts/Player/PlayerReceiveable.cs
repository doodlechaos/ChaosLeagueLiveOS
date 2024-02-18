using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerReceiveable : MonoBehaviour, IPlayerReceiveable
{
    public Queue<PlayerBall> playerQ = new Queue<PlayerBall>();

    public virtual Vector3 GetReceivePosition()
    {
        throw new System.NotImplementedException();
    }

    public virtual void ReceivePlayer(PlayerBall pb)
    {
        pb._rb2D.simulated = true; 
        playerQ.Enqueue(pb); 
        throw new System.NotImplementedException();
    }

    public virtual void ReceiveDeathBall(DeathBall db)
    {
        throw new System.NotImplementedException();
    }
}

public interface IPlayerReceiveable
{
    public void ReceivePlayer(PlayerBall pb);

    public void ReceiveDeathBall(DeathBall db); 

    public Vector3 GetReceivePosition(); 
}