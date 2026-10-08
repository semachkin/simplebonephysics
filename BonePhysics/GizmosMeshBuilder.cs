using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GizmosMeshBuilder : MonoBehaviour
{
    public class CachedMeshes
    {
        int segments;

        Mesh _cachedCylinderMesh;
        public Mesh CachedCylinderMesh
        {
            get 
            {
                _cachedCylinderMesh ??= GizmosMeshBuilder.CreateWireCylinderMesh(segments);

                return _cachedCylinderMesh;
            }
            set => _cachedCylinderMesh = value;
        }

        Mesh _cachedHalfSphereMesh;
        public Mesh CachedHalfSphereMesh
        {
            get 
            {
                _cachedHalfSphereMesh ??= GizmosMeshBuilder.CreateWireHalfSphere(segments);

                return _cachedHalfSphereMesh;
            }
            set => _cachedHalfSphereMesh = value;
        }

        public void Destroy()
        {
            if (CachedCylinderMesh != null)
            {
                DestroyImmediate(CachedCylinderMesh);
            }
            if (CachedHalfSphereMesh != null)
            {
                DestroyImmediate(CachedHalfSphereMesh);
            }
            _cachedCylinderMesh = null;
            _cachedHalfSphereMesh = null;
        }

        public CachedMeshes(int segments) => this.segments = segments;
    }

    public static Mesh CreateWireCylinderMesh(int lines)
    {
        Mesh mesh = new Mesh();
        mesh.hideFlags = HideFlags.DontSave;

        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();

        for (int i = 0; i < lines; i++)
        {
            int idx = i * 2;

            float alpha = i * 2 * Mathf.PI/lines;

            float sin = Mathf.Sin(alpha);
            float cos = Mathf.Cos(alpha);

            vertices.Add(new Vector3(cos, sin, 0));
            vertices.Add(new Vector3(cos, sin, 1));

            indices.Add(idx);
            indices.Add(idx + 1);

            if (i > 0)
            {
                indices.Add(idx);
                indices.Add(idx - 2);
                indices.Add(idx + 1);
                indices.Add(idx - 1);
            }
            if (i == lines-1)
            {
                indices.Add(idx);
                indices.Add(0);
                indices.Add(idx + 1);
                indices.Add(1);
            }
        }
        

        Vector3[] normals = new Vector3[vertices.Count];
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = Vector3.forward;
        }

        mesh.SetVertices(vertices);
        mesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);
        mesh.SetNormals(normals);

        return mesh;
    }

    public static Mesh CreateWireHalfSphere(int segments)
    {
        if (segments < 4) segments = 4;
        if (segments % 2 != 0) segments++;

        Mesh mesh = new Mesh();
        mesh.hideFlags = HideFlags.DontSave;

        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();

        int rings = segments / 2; 

        for (int ri = 0; ri <= rings; ri++)
        {
            float latAngle = (ri * Mathf.PI) / (segments);
            
            float ringRadius = Mathf.Cos(latAngle);
            float z = Mathf.Sin(latAngle);

            if (ri == rings)
            {
                vertices.Add(new Vector3(0, 0, 1));
                break;
            }

            for (int si = 0; si < segments; si++)
            {
                float lonAngle = (si * 2.0f * Mathf.PI) / segments;
                
                float x = Mathf.Cos(lonAngle) * ringRadius;
                float y = Mathf.Sin(lonAngle) * ringRadius;

                vertices.Add(new Vector3(x, y, z));
            }
        }

        int poleIndex = vertices.Count - 1;

        for (int ri = 0; ri < rings; ri++)
        {
            bool isNextRingPole = (ri + 1 == rings);

            for (int si = 0; si < segments; si++)
            {
                int current = ri * segments + si;
                int next = ri * segments + ((si + 1) % segments);

                indices.Add(current);
                indices.Add(next);

                indices.Add(current);
                if (isNextRingPole)
                {
                    indices.Add(poleIndex);
                }
                else
                {
                    int above = (ri + 1) * segments + si;
                    indices.Add(above);
                }
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);

        Vector3[] normals = new Vector3[vertices.Count];
        for (int i = 0; i < vertices.Count; i++)
        {
            normals[i] = vertices[i].normalized;
        }
        mesh.SetNormals(normals);
        
        return mesh;
    }
}
