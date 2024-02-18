using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SuperStickerReceiver : MonoBehaviour, ISuperStickerReceiver
{
/*    public virtual void ReceiveSuperSticker(StreamEvent se, PlayerHandler ph)
    {
        throw new System.NotImplementedException();
    }*/
}

public interface ISuperStickerReceiver
{
    //public void ReceiveSuperSticker(StreamEvent se, PlayerHandler ph);
}