using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RGBText : MonoBehaviour
{
    public TextMeshPro text;
    [SerializeField] private Gradient gradient;
    [SerializeField] private float speed = 0.01f;

    private float t = 0;


    // Update is called once per frame
    void Update()
    {
        text.color = gradient.Evaluate(t);
        t = (t + speed) % 1;
    }
}
