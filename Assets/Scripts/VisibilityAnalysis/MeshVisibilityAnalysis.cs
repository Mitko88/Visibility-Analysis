using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MeshVisibilityAnalysis : MonoBehaviour
{
    public float horizontalFieldOfView = 360f;
    public float verticalFieldOfView = 50f;
    public float rayStep = 1f;
    public float maxDistance = 500f;
    public bool partialVisibility;

    private Mesh mesh;
    private MeshFilter meshFilter;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        mesh = new Mesh();
        meshFilter.mesh = mesh;
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        if (partialVisibility)
        {
            GenerateMesh2();
        }
        else
        {
            GenerateMesh();
        }
        stopwatch.Stop();
        var stopwatchElapsed = stopwatch.Elapsed;
        UnityEngine.Debug.Log("Execution time of mesh construction is: " + stopwatchElapsed.TotalSeconds);
    }

    void GenerateMesh()
    {
        int raysHorizontal = Mathf.RoundToInt(horizontalFieldOfView / rayStep);
        int raysVertical = Mathf.RoundToInt(verticalFieldOfView / rayStep);
        int rayCount = (raysHorizontal + 1) * (raysVertical + 1);

        Vector3[] vertices = new Vector3[rayCount];
        Vector3[] normals = new Vector3[rayCount];
        Vector2[] uv = new Vector2[rayCount];
        int[] triangles = new int[raysHorizontal * raysVertical * 6];

        int vertIndex = 0;
        int triIndex = 0;

        for (int y = 0; y <= raysVertical; y++)
        {
            for (int x = 0; x <= raysHorizontal; x++)
            {
                float angleX = (x * rayStep) - (horizontalFieldOfView / 2);
                float angleY = (y * rayStep) - (verticalFieldOfView / 2);

                Vector3 direction = Quaternion.Euler(angleY, angleX, 0) * transform.forward;
                if (Physics.Raycast(transform.position, direction, out RaycastHit hit, maxDistance))
                {
                    vertices[vertIndex] = transform.InverseTransformPoint(hit.point);
                    normals[vertIndex] = transform.InverseTransformDirection(hit.normal);
                }
                else
                {
                    vertices[vertIndex] = transform.InverseTransformPoint(transform.position + direction * maxDistance);
                    normals[vertIndex] = Vector3.up; // Default normal
                }

                uv[vertIndex] = new Vector2(x / (float)raysHorizontal, y / (float)raysVertical);

                if (x < raysHorizontal && y < raysVertical)
                {
                    int topLeft = vertIndex;
                    int bottomLeft = vertIndex + 1;
                    int topRight = vertIndex + raysHorizontal + 1;
                    int bottomRight = vertIndex + raysHorizontal + 2;

                    // Reverse the order of vertices to change the winding
                    triangles[triIndex++] = topLeft;
                    triangles[triIndex++] = bottomLeft;
                    triangles[triIndex++] = bottomRight;

                    triangles[triIndex++] = topLeft;
                    triangles[triIndex++] = bottomRight;
                    triangles[triIndex++] = topRight;
                }

                vertIndex++;
            }
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.triangles = triangles;

        mesh.RecalculateBounds();
        mesh.RecalculateNormals(); // Ensure normals are recalculated correctly
    }

    void GenerateMesh2()
    {
        int raysHorizontal = Mathf.RoundToInt(horizontalFieldOfView / rayStep);
        int raysVertical = Mathf.RoundToInt(verticalFieldOfView / rayStep);

        Vector3[] directions = new Vector3[(raysHorizontal + 1) * (raysVertical + 1)];
        Dictionary<Vector2Int, int> hitPoints = new Dictionary<Vector2Int, int>();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        int vertIndex = 0;

        for (int y = 0; y <= raysVertical; y++)
        {
            for (int x = 0; x <= raysHorizontal; x++)
            {
                float angleX = (x * rayStep) - (horizontalFieldOfView / 2);
                float angleY = (y * rayStep) - (verticalFieldOfView / 2);

                Vector3 direction = Quaternion.Euler(angleY, angleX, 0) * transform.forward;
                directions[y * (raysHorizontal + 1) + x] = direction;
                if (Physics.Raycast(transform.position, direction, out RaycastHit hit, maxDistance))
                {
                    vertices.Add(transform.InverseTransformPoint(hit.point));
                    normals.Add(transform.InverseTransformDirection(hit.normal));
                    uv.Add(new Vector2(x / (float)raysHorizontal, y / (float)raysVertical));
                    hitPoints[new Vector2Int(x, y)] = vertIndex++;
                }
            }
        }

        List<List<int>> subMeshes = new List<List<int>>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        foreach (var point in hitPoints)
        {
            if (visited.Contains(point.Key)) continue;

            List<int> subMesh = new List<int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(point.Key);
            visited.Add(point.Key);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                int x = current.x;
                int y = current.y;

                if (hitPoints.ContainsKey(current))
                {
                    int topLeft = hitPoints[current];
                    if (hitPoints.ContainsKey(new Vector2Int(x + 1, y)) &&
                        hitPoints.ContainsKey(new Vector2Int(x, y + 1)) &&
                        hitPoints.ContainsKey(new Vector2Int(x + 1, y + 1)))
                    {
                        int bottomLeft = hitPoints[new Vector2Int(x + 1, y)];
                        int topRight = hitPoints[new Vector2Int(x, y + 1)];
                        int bottomRight = hitPoints[new Vector2Int(x + 1, y + 1)];

                        subMesh.Add(topLeft);
                        subMesh.Add(bottomLeft);
                        subMesh.Add(bottomRight);

                        subMesh.Add(topLeft);
                        subMesh.Add(bottomRight);
                        subMesh.Add(topRight);
                    }
                }

                List<Vector2Int> neighbors = new List<Vector2Int>
                {
                    new Vector2Int(x + 1, y),
                    new Vector2Int(x - 1, y),
                    new Vector2Int(x, y + 1),
                    new Vector2Int(x, y - 1)
                };

                foreach (var neighbor in neighbors)
                {
                    if (hitPoints.ContainsKey(neighbor) && !visited.Contains(neighbor))
                    {
                        queue.Enqueue(neighbor);
                        visited.Add(neighbor);
                    }
                }
            }

            if (subMesh.Count > 0)
            {
                subMeshes.Add(subMesh);
            }
        }

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uv.ToArray();

        mesh.subMeshCount = subMeshes.Count;
        for (int i = 0; i < subMeshes.Count; i++)
        {
            mesh.SetTriangles(subMeshes[i], i);
        }

        mesh.RecalculateBounds();
    }


}