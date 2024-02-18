using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ClickSelector : MonoBehaviour
{
    [SerializeField] private UnitTesting _unitTesting;
    private void Update()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)//if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(GetMouseWorldPosition(), Vector2.zero);
            foreach (var hit in hits)
            {
                PlayerBall pb = hit.collider.GetComponentInParent<PlayerBall>();
                if (pb == null)
                    continue;

                //_unitTesting.SetSelectionPlatformID(pb.Ph.PlatformID, pb.Ph.PlatformAuthorName);
                pb.SpinY();
            }
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePosition = Mouse.current.position.ReadValue(); //Input.mousePosition;
        mousePosition.z = -Camera.main.transform.position.z;

        return Camera.main.ScreenToWorldPoint(mousePosition);
    }
}
