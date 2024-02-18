using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CleaningBarController : MonoBehaviour
{
    [SerializeField] public Transform CleaningBar;
    [SerializeField] private Transform _cleaningBarStartPos;
    [SerializeField] private Transform _cleaningBarEndPos; 
    [SerializeField] private float _cleaningBarDuration = 3;

    private float _cleaningBarTimer = 0;

    public bool Activated;

    public IEnumerator RunCleaningBar()
    {
        Activated = true;
        CleaningBar.gameObject.SetActive(true);

        _cleaningBarTimer = 0;

        while (Activated)
        {
            float t = _cleaningBarTimer / _cleaningBarDuration;
            if (t >= 1)
            {
                Activated = false;
                break;
            }
            Vector2 nextBarPos = Vector2.Lerp(_cleaningBarStartPos.position, _cleaningBarEndPos.position, t);
            CleaningBar.transform.position = nextBarPos;
            _cleaningBarTimer += Time.deltaTime;

            yield return null;
        }

        CleaningBar.gameObject.SetActive(false); 
    }



}
