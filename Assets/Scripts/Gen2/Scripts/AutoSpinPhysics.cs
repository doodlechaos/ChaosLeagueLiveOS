using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoSpinPhysics : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 30.0f; // Adjust the rotation speed as desired
    [SerializeField] private Vector3 spinAxis = Vector3.up;

    [SerializeField] private GameTile tile;

    private float currentAngle = 0.0f; // Store the current rotation angle
    private Quaternion _initialRotation; 

    private void Awake()
    {
        _initialRotation = transform.localRotation;
    }

    void FixedUpdate()
    {
        //If we're not in gameplay or bidding, don't rotate
        if (tile != null && tile.TileState == TileState.Inactive)
            return;

        //transform.RotateAround(transform.position, spinAxis, rotationSpeed);

        // Calculate the new angle
        currentAngle += rotationSpeed; // Adjust the angle based on the rotation speed and time
        currentAngle = currentAngle % 360; // Optional: Keep the angle within 0-360 degrees

        // Apply the rotation
        transform.localRotation = _initialRotation * Quaternion.AngleAxis(currentAngle, spinAxis);
    }
}
