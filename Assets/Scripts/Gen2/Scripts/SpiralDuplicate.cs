using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiralDuplicate : MonoBehaviour
{
    public GameObject objectToDuplicate;
    public Transform center;
    public Transform startPosition;

    [SerializeField] private float degreesRange;

    [SerializeField] private int numberOfDuplicates = 5;

    [SerializeField] private bool createArrayOfObjButton;

    private void OnValidate()
    {
        if (createArrayOfObjButton)
        {
            createArrayOfObjButton = false;
            CreateArrayOfObjs();
        }
    }

    void CreateArrayOfObjs()
    {

        for (int i = 0; i <= numberOfDuplicates; i++)
        {
            // Duplicate the object and set its position
            GameObject duplicate = Instantiate(objectToDuplicate);
            duplicate.transform.SetParent(this.transform);

            duplicate.transform.position = startPosition.position;

            duplicate.transform.RotateAround(center.position, Vector3.forward, i * (degreesRange / numberOfDuplicates));


        }
    }
}
