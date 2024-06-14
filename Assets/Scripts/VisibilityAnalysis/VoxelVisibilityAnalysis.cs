using CesiumForUnity;
using MeshManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VoxelSystem;

public class VoxelVisibilityAnalysis : MonoBehaviour
{
    public float voxelSize;
    public bool visualizeVoxels;
    public Transform visibilityPoint;
    public float raycastingDistance = 200;
    public VoxelVisualizationType vfxVisType { get; set; } = VoxelVisualizationType.quad;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            // get all gameobject that have a mesh attached
            var meshFilters = GetMeshFilters();

            // voxelize the scene
            var voxelizer = GetVoxelizer();
            var multiValueVoxelModel = VoxelFunctions.GetMultiValueVoxelData(meshFilters, voxelizer, voxelSize, VoxelizationGeomType.surface);

            // perform raycasting to every voxel
            PerformRaycasting(multiValueVoxelModel);

            //visualize the visibility analysis
            if (visualizeVoxels)
            {
                var vfxVisualization = VfxFunctions.VisualiseVfxColorVoxels(multiValueVoxelModel, voxelSize, vfxVisType);
                vfxVisualization.transform.parent = transform.parent.transform.parent.transform;
                vfxVisualization.AddComponent<CesiumGlobeAnchor>();                    
            }
        }
    }

    private MeshFilter[] GetMeshFilters()
    {
        var meshFilters = new List<MeshFilter>();
        foreach (Transform child in transform)
        {
            // Check if the child is active
            if (child.gameObject.activeSelf)
            {
                foreach (Transform child2 in child)
                {
                    // Check if the child has a MeshFilter component
                    var meshFilter = child2.GetComponent<MeshFilter>();
                    if (meshFilter != null)
                    {
                        meshFilters.Add(meshFilter);
                    }
                }
            }
        }
        return meshFilters.ToArray();
    }

    private ComputeShader GetVoxelizer()
    {
        if (AssetDatabase.LoadAssetAtPath("Assets/Scripts/Shaders/Voxelizer.compute", typeof(ComputeShader)) == null)
        {
            throw new FileLoadException("Voxelizer compute shader is not present");
        }

        return (ComputeShader)AssetDatabase.LoadAssetAtPath("Assets/Scripts/Shaders/Voxelizer.compute", typeof(ComputeShader));
    }

    private void PerformRaycasting(MultiValueVoxelModel voxelModel)
    {
        var voxelModelPivotPoint = voxelModel.Bounds.min;
        var firstVoxelCentroid = new Vector3(voxelModelPivotPoint.x, voxelModelPivotPoint.y, voxelModelPivotPoint.z);
        var m = 0;
        var visibleVoxelColor = new VoxelObject();
        visibleVoxelColor.VoxelColor = Color.green;
        var blockedVoxelColor = new VoxelObject();
        blockedVoxelColor.VoxelColor = Color.red;

        for (int k = 0; k < voxelModel.Depth; k++)
            for (int j = 0; j < voxelModel.Height; j++)
                for (int i = 0; i < voxelModel.Width; i++)
                {
                    var voxel = voxelModel.Voxels[m];

                    if (voxel != null)
                    {
                        var voxelCentroid = new Vector3(firstVoxelCentroid.x + i * voxelSize, firstVoxelCentroid.y + j * voxelSize, firstVoxelCentroid.z + k * voxelSize);                     
                        var rayDirection = (voxelCentroid - visibilityPoint.position);
                        var distanceToVoxel = rayDirection.magnitude;

                        if (raycastingDistance > distanceToVoxel)
                        {
                            RaycastHit hit;
                            // Does the ray intersect any objects excluding the player layer
                            if (Physics.Raycast(visibilityPoint.position, rayDirection.normalized, out hit, raycastingDistance))
                            {
                                //Debug.DrawLine(visibilityPoint.position, hit.point, Color.yellow, 5);
                                var distanceToHitPoint = Vector3.Distance(hit.point, visibilityPoint.position);

                                //not blocked
                                if (distanceToHitPoint - distanceToVoxel > 0 || Mathf.Abs(distanceToVoxel - distanceToHitPoint) < voxelSize)
                                {
                                    voxelModel.Voxels[m][0] = visibleVoxelColor;

                                }
                            }
                            else
                            {
                                voxelModel.Voxels[m] = null;
                            }
                        }
                        else
                        {
                            voxelModel.Voxels[m] = null;
                        }
                    }
                    m++;
                }

    }
}
