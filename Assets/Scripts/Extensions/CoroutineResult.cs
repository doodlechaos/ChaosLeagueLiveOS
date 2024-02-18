using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class CoroutineResult<T> : CustomYieldInstruction
{
    public T Result { get; private set; }
    private bool isCompleted = false;

    public override bool keepWaiting => !isCompleted;

    public void Complete(T result)
    {
        Result = result;
        isCompleted = true;
    }
}