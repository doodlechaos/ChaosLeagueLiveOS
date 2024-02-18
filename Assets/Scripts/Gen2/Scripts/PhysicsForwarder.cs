using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsForwarder : MonoBehaviour
{
    private PhysicsForwardReceiver _receiver;

    public void Init(PhysicsForwardReceiver receiver)
    {
        Debug.Log("Set receiver for physics forwarder");
       _receiver = receiver;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Physics Forwarder OTEnter");

        if (_receiver == null)
        {
            Debug.LogError("No reciever in physics forwarder");
            return;
        }

        _receiver.MyTriggerEnter2D(collision);
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        Debug.Log("Physics Forwarder OTExit");

        if (_receiver == null)
        {
            Debug.LogError("No reciever in physics forwarder");
            return;
        }

        _receiver.MyTriggerExit2D(collision);
    }

}

public interface PhysicsForwardReceiver
{
    public void MyTriggerEnter2D(Collider2D collision);
    public void MyTriggerExit2D(Collider2D collision);
}
