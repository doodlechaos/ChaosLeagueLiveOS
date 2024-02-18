using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EffectorLogic { increment, decrement, multiply, nthtriangle, reset, none}
/*public class PBEffectorLogic : MonoBehaviour
{
    [SerializeField] private List<PBEffector> _pbEffectors;
    [SerializeField] private EffectorLogic _effectorLogic;
    [SerializeField] private float _value = 1; 

    public void TriggerLogic(PlayerBall pb)
    {
        if(_effectorLogic == EffectorLogic.increment)
        {
            foreach (PBEffector effector in _pbEffectors)
                effector.IncrementCurrValue(_value);
        }
        else if(_effectorLogic == EffectorLogic.decrement)
        {
            foreach (PBEffector effector in _pbEffectors)
                effector.DecrementCurrValue(_value);
        }
        else if(_effectorLogic == EffectorLogic.reset)
        {
            foreach (PBEffector effector in _pbEffectors)
                effector.ResetValue();
        }
        else if(_effectorLogic == EffectorLogic.multiply)
        {
            foreach (PBEffector effector in _pbEffectors)
                effector.MultiplyCurrValue(_value);
        }

    }

}*/
