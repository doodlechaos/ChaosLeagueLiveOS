using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoSpinVisual : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 30.0f; // Adjust the rotation speed as desired
    [SerializeField] private Vector3 spinAxis = Vector3.up;



    void Update()
    {
        transform.RotateAround(transform.position, spinAxis, rotationSpeed * Time.deltaTime);
    }
}
