using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuperChatReceiver : MonoBehaviour, ISuperChatReceiver
{
/*    public virtual void ReceiveSuperChat(StreamEvent se, PlayerHandler ph)
    {
        throw new System.NotImplementedException();
    }*/
}

public interface ISuperChatReceiver
{
    //public void ReceiveSuperChat(StreamEvent se, PlayerHandler ph);
}