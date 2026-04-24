using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class GrassManager : MonoBehaviour
{
    [SerializeField] private ComputeShader grassCompute;

    [SerializeField] private Terrain terrain;
    [SerializeField] private int grassDensity = 50;
    [SerializeField] private Vector3 grassScale;
    [SerializeField] private Mesh grassMesh;
    [SerializeField] private Material grassMaterial;
    private List<Matrix4x4[]> batches = new List<Matrix4x4[]>();

    void Start()
    {
        GenerateGrass(new Vector2Int((int)terrain.terrainData.size.x,(int)terrain.terrainData.size.z));
    }
    
    public void GenerateGrass(Vector2Int bounds)
    {
        float stepAmount = 1 / (float)grassDensity;
        Vector2Int pointsPerSide = new Vector2Int(bounds.x * grassDensity, bounds.y * grassDensity);
        List<Matrix4x4> currentBatch = new List<Matrix4x4>();

        for (int x = 0; x < pointsPerSide.x; x++)
        {
            for (int y = 0; y < pointsPerSide.y; y++)
            {   
                Vector3 pos = new Vector3(x * stepAmount, 0, (y * stepAmount));
            
                Matrix4x4 mat = Matrix4x4.TRS(pos, Quaternion.identity, grassScale);
                
                currentBatch.Add(mat);

                // Graphics.DrawMeshInstanced limit is 1023
                if (currentBatch.Count == 1023)
                {
                    batches.Add(currentBatch.ToArray());
                    currentBatch.Clear();
                }
            }
        }
        
        if (currentBatch.Count > 0) batches.Add(currentBatch.ToArray());

    
    }
    
    void Update()
    {
        // Render every batch every frame
        foreach (var batch in batches)
        {
            Graphics.DrawMeshInstanced(grassMesh, 0, grassMaterial, batch);
        }
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct GrassBlade
{
    public Vector3 position;
}                                 
