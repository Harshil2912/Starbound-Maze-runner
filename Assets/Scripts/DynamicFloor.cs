using System.Collections.Generic;
using UnityEngine;

// One welded floor collider built from a tile grid. Tiles can be switched on/off at runtime
// (used by bridges) without ever creating seams between separate colliders.
[RequireComponent(typeof(MeshCollider))]
public class DynamicFloor : MonoBehaviour
{
    public int cols = 21;
    public int rows = 21;
    public float tile = 4f;
    public bool[] baseSolid;

    bool[] solid;
    Mesh mesh;
    MeshCollider mc;

    void OnEnable()
    {
        mc = GetComponent<MeshCollider>();
        solid = (bool[])baseSolid.Clone();
        Rebuild();
    }

    public void SetSolid(int c, int r, bool s)
    {
        if (solid == null || c < 0 || r < 0 || c >= cols || r >= rows) return;
        if (solid[r * cols + c] == s) return;
        solid[r * cols + c] = s;
        Rebuild();
    }

    public bool IsSolid(int c, int r)
    {
        return solid != null && c >= 0 && r >= 0 && c < cols && r < rows && solid[r * cols + c];
    }

    void Rebuild()
    {
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "DynamicFloor";
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        var verts = new List<Vector3>((cols + 1) * (rows + 1));
        for (int r = 0; r <= rows; r++)
            for (int c = 0; c <= cols; c++)
                verts.Add(new Vector3((c - cols / 2f) * tile, 0f, (rows / 2f - r) * tile));
        var tris = new List<int>();
        int stride = cols + 1;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                if (!solid[r * cols + c]) continue;
                int a = r * stride + c, b = a + 1, d = a + stride, e = d + 1;
                tris.Add(a); tris.Add(b); tris.Add(e);
                tris.Add(a); tris.Add(e); tris.Add(d);
            }
        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        mc.sharedMesh = null;
        mc.sharedMesh = mesh;
    }
}
