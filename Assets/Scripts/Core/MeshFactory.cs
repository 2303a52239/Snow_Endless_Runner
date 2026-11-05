using UnityEngine;

namespace SnowRunner
{
    /// <summary>Procedural meshes that Unity primitives don't provide (cone).</summary>
    public static class MeshFactory
    {
        static Mesh coneMesh;

        /// <summary>Unit cone: base radius 0.5 at y=0, apex at y=1.</summary>
        public static Mesh Cone(int segments = 12)
        {
            if (coneMesh != null) return coneMesh;

            var verts = new Vector3[segments * 2 + 2];
            var tris = new int[segments * 6];
            // side vertices (duplicated per ring so edges are shaded as a cone)
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                verts[i] = new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f);
                verts[segments + i] = verts[i];
            }
            int apex = segments * 2;
            int center = segments * 2 + 1;
            verts[apex] = new Vector3(0f, 1f, 0f);
            verts[center] = Vector3.zero;

            int t = 0;
            for (int i = 0; i < segments; i++)
            {
                int n = (i + 1) % segments;
                // side
                tris[t++] = i; tris[t++] = apex; tris[t++] = n;
                // base
                tris[t++] = segments + i; tris[t++] = segments + n; tris[t++] = center;
            }

            coneMesh = new Mesh { name = "ProceduralCone", vertices = verts, triangles = tris };
            coneMesh.RecalculateNormals();
            coneMesh.RecalculateBounds();
            return coneMesh;
        }

        public static GameObject ConeObject(Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = new GameObject("Cone");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = Cone();
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }
    }
}
