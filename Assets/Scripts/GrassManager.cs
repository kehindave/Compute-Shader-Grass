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
    [SerializeField] private Transform cameraTransform;
    [SerializeField] [Range(-1,1)]private float viewAngle;
    
    [SerializeField] private ComputeShader grassCompute;
    [SerializeField] private Terrain terrain;
    [SerializeField] private int grassDensity = 50;
    [SerializeField] private Vector3  grassOffset, grassPositionRandomness;
    [FormerlySerializedAs("grassSIze")] [SerializeField] private float grassSize;
    [SerializeField] private VisualEffect grassEffect;
    
    private GraphicsBuffer grassBuffer;
    private int grassCount;
    private int updateGrassKernelIndex;


    private void Start()
    {
        updateGrassKernelIndex = grassCompute.FindKernel("UpdateGrass");

        GenerateGrass();
    }

    [ContextMenu("Generate Grass")]
    public void GenerateGrass()
    {
        grassBuffer?.Dispose();
        var bounds = new Vector2Int((int)terrain.terrainData.size.x, (int)terrain.terrainData.size.z);
        float stepAmount = 1 / (float)grassDensity;
        Vector2Int pointsPerSide = new Vector2Int(bounds.x * grassDensity, bounds.y * grassDensity);
        var blades = new List<GrassData>();

        for (int x = 0; x < pointsPerSide.x; x++)
        {
            for (int y = 0; y < pointsPerSide.y; y++)
            {   
                Vector3 pos = new Vector3(x * stepAmount, 0, (y * stepAmount)) +grassOffset;
                float angle = Random.Range(0, Mathf.PI * 2);
        
                blades.Add(new GrassData()
                {
                    position = pos +(grassPositionRandomness * Random.Range(-1f,1f)),
                    up = new Vector3(Random.Range(-1f,1f), Random.Range(0.5f,1f), Random.Range(-1f,1f)).normalized,
                    forward = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)),
                    size = Random.Range(grassSize,grassSize*1.2f)
                    
                });
            }
        }
        grassCount = blades.Count;
        grassBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, blades.Count,
            UnsafeUtility.SizeOf<GrassData>());
        grassBuffer.SetData(blades);
        grassEffect.SetInt("SpawnSize", blades.Count);
        grassEffect.SetGraphicsBuffer("GrassDataBuffer", grassBuffer);
        grassEffect.Play();

    }

    void Update()
    {

        // Pass player data
        grassCompute.SetVector("CameraPosition", cameraTransform.position);
        grassCompute.SetVector("CameraForward", cameraTransform.forward);
        grassCompute.SetFloat("ViewAngle", viewAngle);
        grassCompute.SetInt("GrassCount", grassCount);

        grassCompute.SetBuffer(updateGrassKernelIndex, "GrassBuffer", grassBuffer);
    
        // Dispatch based on your total grass count
        int groups = Mathf.CeilToInt(grassCount / 64f);
        grassCompute.Dispatch(updateGrassKernelIndex, groups, 1, 1);
    }
    
    private  List<GrassData>  GenerateGrassData()
    {
        var minDist = 1f / grassDensity;
        var bounds = new Vector2Int((int)terrain.terrainData.size.x, (int)terrain.terrainData.size.z);

        // 1. Initialize the background grid
        // Cell size is r / sqrt(2) to ensure each cell can hold at most one point
        float cellSize = minDist / (float)Math.Sqrt(2);
        int gridWidth = (int)Math.Ceiling(bounds.x / cellSize);
        int gridHeight = (int)Math.Ceiling(bounds.y / cellSize);
        
        Vector2?[,] grid = new Vector2?[gridWidth, gridHeight];
        
        List<Vector2> activeList = new List<Vector2>();
        System.Random rand = new System.Random();

        // 2. Seed the first point
        Vector2 firstPoint = new Vector2((float)rand.NextDouble() * bounds.x, (float)rand.NextDouble() * bounds.y);
        activeList.Add(firstPoint);
        grid[(int)(firstPoint.x / cellSize), (int)(firstPoint.y / cellSize)] = firstPoint;
        List<GrassData> blades = new List<GrassData>();

        // 3. Process active list
        while (activeList.Count > 0)
        {
            int index = rand.Next(activeList.Count);
            Vector2 center = activeList[index];
            bool found = false;

            for (int i = 0; i < 3; i++)
            {
                // Generate random point in annulus [minDist, 2*minDist]
                float angle = (float)rand.NextDouble() * MathF.PI * 2;
                float radius = minDist * (1 + (float)rand.NextDouble());
                Vector2 candidate = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;

                if (IsValid(candidate, bounds, cellSize, minDist, grid))
                {
                    blades.Add(new GrassData()
                    {
                        position = new Vector3(candidate.x + grassOffset.x, grassOffset.y, candidate.y +grassOffset.z) +(grassPositionRandomness * Random.Range(-1f,1f)),
                        up = new Vector3(Random.Range(-0.15f,0.15f), 1, Random.Range(-0.15f,0.15f)),
                        forward = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)),
                        size = Random.Range(grassSize,grassSize*1.2f)
                    
                    });
                    activeList.Add(candidate);
                    grid[(int)(candidate.x / cellSize), (int)(candidate.y / cellSize)] = candidate;
                    found = true;
                    break;
                }
            }

            if (!found) activeList.RemoveAt(index);
        }

        return blades;
    }

    private static bool IsValid(Vector2 p, Vector2 bounds, float cellSize, float minDist, Vector2?[,] grid)
    {
        if (p.x < 0 || p.x >= bounds.x || p.y < 0 || p.y >= bounds.y) return false;

        int xIndex = (int)(p.x / cellSize);
        int yIndex = (int)(p.y / cellSize);

        // Search surrounding 5x5 grid neighborhood
        int searchRadius = 2;
        for (int x = Math.Max(0, xIndex - searchRadius); x <= Math.Min(grid.GetLength(0) - 1, xIndex + searchRadius); x++)
        {
            for (int y = Math.Max(0, yIndex - searchRadius); y <= Math.Min(grid.GetLength(1) - 1, yIndex + searchRadius); y++)
            {
                if (grid[x, y].HasValue)
                {
                    if (Vector2.Distance(p, grid[x, y].Value) < minDist) return false;
                }
            }
        }
        return true;
    }
    private void OnDestroy()
    {
        grassBuffer?.Dispose();
    }
}

[StructLayout(LayoutKind.Sequential)]
[VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
public struct GrassData
{
    public Vector3 position;
    public Vector3 forward;
    public Vector3 up;
    public float isVisible;
    public float size;
}


