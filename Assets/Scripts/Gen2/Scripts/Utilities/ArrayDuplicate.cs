using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrayDuplicate : MonoBehaviour
{
    public GameObject objectToDuplicate;
    public Transform startPosition;
    public Transform endPosition;

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
        // Calculate the spacing between duplicates
        float spacing = Vector3.Distance(startPosition.position, endPosition.position) / (numberOfDuplicates);

        for (int i = 0; i <= numberOfDuplicates; i++)
        {
            // Calculate the position for the current duplicate
            Vector3 duplicatePosition = Vector3.Lerp(startPosition.position, endPosition.position, i / (float)(numberOfDuplicates));

            // Duplicate the object and set its position
            GameObject duplicate = Instantiate(objectToDuplicate, duplicatePosition, Quaternion.identity);
            duplicate.transform.SetParent(this.transform); 
        }
    }
}
