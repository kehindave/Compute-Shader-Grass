using UnityEngine;

public class QuadGenerator : MonoBehaviour
{
    [ContextMenu("Generate Quad")]
    void CreateQuad()
    {
        var duplicate = Instantiate(this.gameObject, transform.position + Vector3.up, transform.rotation);
        Mesh mesh = new Mesh();
        mesh.name = "UpwardNormalQuad";

        // 1. Vertices (Standing up, bottom edge at Y=0)
        // Adjust these if you want that specific "little bit up" offset
        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, 0, 0),  // Bottom Left
            new Vector3(0.5f, 0, 0),   // Bottom Right
            new Vector3(-0.5f, 1, 0),  // Top Left
            new Vector3(0.5f, 1, 0)    // Top Right
        };

        // 2. UVs (Standard 0-1 mapping)
        mesh.uv = new Vector2[]
        {
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0, 1), new Vector2(1, 1)
        };

        // 3. Triangles
        mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };

        // 4. THE KEY: Force Normals Up
        // In Unity, Vector3.up is (0, 1, 0)
        mesh.normals = new Vector3[]
        {
            Vector3.up, Vector3.up, Vector3.up, Vector3.up
        };

        duplicate.GetComponent<MeshFilter>().mesh = mesh;
    }
}
