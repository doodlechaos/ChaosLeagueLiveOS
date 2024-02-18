using System.Collections.Generic;
using UnityEngine;

public class Collider2DFromMesh : MonoBehaviour
{
    [SerializeField] private MeshFilter _meshfilter;
    [SerializeField] private bool generateButton;

    private void OnValidate()
    {
        if (generateButton)
        {
            generateButton = false;
            GeneratePolyCollider2D(); 
        }
    }

    private void GeneratePolyCollider2D()
    {
        // Stop if no mesh filter exists or there's already a collider
        if (_meshfilter == null)
        {
            return;
        }



        //transform.position = _meshfilter.gameObject.transform.position;

        GameObject colliderChild = new GameObject("2DcolliderChild");
        PolygonCollider2D polyCollider = colliderChild.AddComponent<PolygonCollider2D>(); 

        // Get triangles and vertices from mesh
        int[] triangles = _meshfilter.sharedMesh.triangles;
        Vector3[] vertices = _meshfilter.sharedMesh.vertices;

        List<Vector2> flatVerts = new List<Vector2>();
        List<int> flatTris = new List<int>();
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = _meshfilter.transform.TransformPoint(vertices[i]);
            vertices[i] -= _meshfilter.transform.position;
            if (!flatVerts.Contains(vertices[i]))
            {
                flatVerts.Add(vertices[i]);
            }
        }

        // Get just the outer edges from the mesh's triangles (ignore or remove any shared edges)
        Dictionary<string, KeyValuePair<int, int>> edges = new Dictionary<string, KeyValuePair<int, int>>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            for (int e = 0; e < 3; e++)
            {
                int vert1 = triangles[i + e];
                int vert2 = triangles[i + e + 1 > i + 2 ? i : i + e + 1];
                string edge = Mathf.Min(vert1, vert2) + ":" + Mathf.Max(vert1, vert2);

                Debug.DrawLine(vertices[vert1], vertices[vert2], Color.green, 100); 

                if (edges.ContainsKey(edge))
                {
                    Debug.Log($"removing edge: {edge}");

                    edges.Remove(edge);
                }
                else
                {
                    Debug.Log($"adding edge: {edge}");

                    //if there is already a vert
                    edges.Add(edge, new KeyValuePair<int, int>(vert1, vert2));
                }
            }
        }

        // Create edge lookup (Key is first vertex, Value is second vertex, of each edge)
        Dictionary<int, int> lookup = new Dictionary<int, int>();
        foreach (KeyValuePair<int, int> edge in edges.Values)
        {
            if (lookup.ContainsKey(edge.Key) == false)
            {
                lookup.Add(edge.Key, edge.Value);
            }
        }

        // Create empty polygon collider


        polyCollider.pathCount = 0;

        // Loop through edge vertices in order
        int startVert = 0;
        int nextVert = startVert;
        int highestVert = startVert;
        List<Vector2> colliderPath = new List<Vector2>();
        while (true)
        {

            // Add vertex to collider path
            colliderPath.Add(vertices[nextVert]);

            // Get next vertex
            nextVert = lookup[nextVert];

            // Store highest vertex (to know what shape to move to next)
            if (nextVert > highestVert)
            {
                highestVert = nextVert;
            }

            // Shape complete
            if (nextVert == startVert)
            {

                // Add path to polygon collider
                polyCollider.pathCount++;
                polyCollider.SetPath(polyCollider.pathCount - 1, colliderPath.ToArray());
                colliderPath.Clear();

                // Go to next shape if one exists
                if (lookup.ContainsKey(highestVert + 1))
                {

                    // Set starting and next vertices
                    startVert = highestVert + 1;
                    nextVert = startVert;

                    // Continue to next loop
                    continue;
                }

                // No more verts
                break;
            }
        }
        polyCollider.transform.position = _meshfilter.transform.position;
        polyCollider.transform.SetParent(_meshfilter.transform, true); 
    }


}