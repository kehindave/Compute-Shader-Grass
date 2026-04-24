using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using Random = UnityEngine.Random;

public class GrassManager : MonoBehaviour
{
    [SerializeField] private ComputeShader grassCompute;

    [SerializeField] private Terrain terrain;
    [SerializeField] private int grassDensity = 50;
    [SerializeField] private Vector3  grassOffset, grassPositionRandomness;
    [FormerlySerializedAs("grassSIze")] [SerializeField] private float grassSize;
    [SerializeField] private VisualEffect grassEffect;
    private GraphicsBuffer grassBuffer;


    private void Start()
    {
        GenerateGrass();
    }

    [ContextMenu("Generate Grass")]
    public void GenerateGrass()
    {
        grassBuffer?.Dispose();
        var bounds = new Vector2Int((int)terrain.terrainData.size.x, (int)terrain.terrainData.size.z);
        float stepAmount = 1 / (float)grassDensity;
        Vector2Int pointsPerSide = new Vector2Int(bounds.x * grassDensity, bounds.y * grassDensity);
        List<GrassData> blades = new List<GrassData>();

        for (int x = 0; x < pointsPerSide.x; x++)
        {
            for (int y = 0; y < pointsPerSide.y; y++)
            {   
                Vector3 pos = new Vector3(x * stepAmount, 0, (y * stepAmount)) +grassOffset;
                float angle = Random.Range(0, Mathf.PI * 2);

                blades.Add(new GrassData()
                {
                    position = pos +(grassPositionRandomness * Random.Range(-1f,1f)),
                    up = new Vector3(Random.Range(-0.15f,0.15f), 1, Random.Range(-0.15f,0.15f)),
                    forward = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)),
                    size = Random.Range(grassSize,grassSize*1.2f)
                    
                });
            }
        }
        
        grassBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, blades.Count, UnsafeUtility.SizeOf<GrassData>());
        grassBuffer.SetData(blades);
    grassEffect.SetInt("SpawnSize", blades.Count);
    grassEffect.SetGraphicsBuffer("GrassDataBuffer", grassBuffer);
    grassEffect.Play();
    
    }

    private void OnDestroy()
    {
        grassBuffer.Dispose();
    }
}

[StructLayout(LayoutKind.Sequential)]
[VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
public struct GrassData
{
    public Vector3 position;
    public Vector3 forward;
    public Vector3 up;
    public float size;
}


