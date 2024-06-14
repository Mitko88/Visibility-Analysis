using CesiumForUnity;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class SpawnCube : MonoBehaviour
{
    public float angleIncrement = 5f;
    public float rayDistance = 100f;
    public GameObject cubePrefab; // Assign a cube prefab in the Inspector
    public bool visualizeRays;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            CastRays();
        }
    }

    void CastRays()
    {
        float radIncrement = Mathf.Deg2Rad * angleIncrement;
        var i = 0;
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var hitPoints = new List<Vector3>();
        var scalePoints = new List<Vector3>();
        for (float horizontalAngle = 0; horizontalAngle < 360; horizontalAngle += angleIncrement)
        {
            for (float verticalAngle = 0; verticalAngle < 360; verticalAngle += angleIncrement)
            {
                Vector3 direction = Quaternion.Euler(verticalAngle, horizontalAngle, 0) * transform.forward;
                Ray ray = new Ray(transform.position, direction);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, rayDistance))
                {
                    if(visualizeRays)
                        UnityEngine.Debug.DrawRay(transform.position, direction * hit.distance, Color.yellow, 5f);

                    float distance = hit.distance;

                    // Calculate the spacing between rays at this distance
                    float spacing = 2 * distance * Mathf.Tan(radIncrement / 2);

                    Vector3 cubeScale = new Vector3(spacing, spacing, spacing);
                    hitPoints.Add(hit.point);
                    scalePoints.Add(cubeScale);
                    i++;
                }
            }
        }

        stopwatch.Stop();
        var stopwatchElapsed = stopwatch.Elapsed;
        UnityEngine.Debug.Log("Visibility computational time " + stopwatchElapsed.TotalSeconds + " s");

        stopwatch = new Stopwatch();
        stopwatch.Start();
        for (int j = 0; j < i; j++)
        {
            var hitPoint = hitPoints[j];
            var scalePoint = scalePoints[j];
            CreateCube(hitPoint, scalePoint);
        }

        stopwatch.Stop();
        stopwatchElapsed = stopwatch.Elapsed;
        UnityEngine.Debug.Log("Cubes creatio time " + stopwatchElapsed.TotalSeconds + " s");

        UnityEngine.Debug.Log("Total number of cubes created: " + i.ToString());
    }

    void CreateCube(Vector3 position, Vector3 scale)
    {
        GameObject cube = Instantiate(cubePrefab, position, Quaternion.identity);
        cube.transform.localScale = scale;
        cube.transform.parent = transform;
    }
}
