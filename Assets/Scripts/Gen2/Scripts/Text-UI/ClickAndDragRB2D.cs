using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ClickAndDragRB2D : MonoBehaviour
{
    public LayerMask layersToDrag; 

    private Rigidbody2D rb;
    private bool isDragging = false;
    private Vector2 offset;

    public Transform tempTest;

    [SerializeField] private GameObject _disableIfActive;

    private void Update()
    {
        if (_disableIfActive.activeInHierarchy)
            return;

        if(Mouse.current.leftButton.wasPressedThisFrame)//if (Input.GetMouseButtonDown(0))
        {
            //Debug.Log("Mouse world pos: " + GetMouseWorldPosition());
            //tempTest.position = GetMouseWorldPosition(); 

            RaycastHit2D[] hits = Physics2D.RaycastAll(GetMouseWorldPosition(), Vector2.zero);
            foreach(var hit in hits)
            {
                if (hit.rigidbody != null && MyUtil.IsLayerInMask(hit.collider.gameObject.layer, layersToDrag))
                {

                    rb = hit.rigidbody;
                    // Object was clicked, begin dragging
                    isDragging = true;
                    // Calculate the offset between the clicked position and the object's position
                    offset = rb.position - (Vector2)GetMouseWorldPosition();
                    rb.simulated = false;
                    break;
                }
            }

        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame)//(Input.GetMouseButtonUp(0))
        {
            // Stop dragging
            isDragging = false;
            if(rb != null)
            {
                rb.simulated = true;
                rb.velocity = Vector2.zero;
                rb = null; 
            }
        }

        if (isDragging)
        {
            // Move the object to the current mouse position plus the offset
            Vector2 newPos = (Vector2)GetMouseWorldPosition() + offset; 
            rb.transform.position = new Vector3(newPos.x, newPos.y, rb.transform.position.z);
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePosition = Mouse.current.position.ReadValue(); //Input.mousePosition;
        mousePosition.z = -Camera.main.transform.position.z;

        return Camera.main.ScreenToWorldPoint(mousePosition);
    }
}
