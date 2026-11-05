using System.Collections.Generic;
using UnityEngine;

namespace SnowRunner
{
    /// <summary>Procedural material + primitive helpers. No external assets required.</summary>
    public static class Mats
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Shader litShader;

        static Shader GetShader()
        {
            if (litShader != null) return litShader;
            litShader = Shader.Find("Standard");
            if (litShader == null) litShader = Shader.Find("Legacy Shaders/Diffuse");
            if (litShader == null) litShader = Shader.Find("Sprites/Default");
            return litShader;
        }

        public static Material Get(string name, Color c, float smoothness = 0.2f, float emission = 0f)
        {
            if (cache.TryGetValue(name, out var m) && m != null) return m;
            m = new Material(GetShader()) { name = name, color = c };
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (emission > 0f && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * emission);
            }
            cache[name] = m;
            return m;
        }

        public static Material Snow => Get("Snow", new Color(0.96f, 0.98f, 1f), 0.35f);
        public static Material Ice => Get("Ice", new Color(0.62f, 0.85f, 0.98f), 0.9f);
        public static Material DeepIce => Get("DeepIce", new Color(0.35f, 0.6f, 0.85f), 0.9f);
        public static Material Wood => Get("Wood", new Color(0.45f, 0.3f, 0.18f), 0.1f);
        public static Material DarkWood => Get("DarkWood", new Color(0.3f, 0.2f, 0.12f), 0.1f);
        public static Material Pine => Get("Pine", new Color(0.12f, 0.36f, 0.26f), 0.1f);
        public static Material Rock => Get("Rock", new Color(0.5f, 0.54f, 0.6f), 0.1f);
        public static Material Mountain => Get("Mountain", new Color(0.78f, 0.86f, 0.95f), 0.1f);
        public static Material Gold => Get("Gold", new Color(1f, 0.82f, 0.15f), 0.8f, 0.25f);
        public static Material Red => Get("Red", new Color(0.8f, 0.15f, 0.15f), 0.3f);
        public static Material Metal => Get("Metal", new Color(0.3f, 0.33f, 0.38f), 0.6f);
        public static Material Skin => Get("Skin", new Color(0.95f, 0.78f, 0.65f), 0.1f);
        public static Material Coat => Get("Coat", new Color(0.9f, 0.25f, 0.2f), 0.2f);
        public static Material Pants => Get("Pants", new Color(0.15f, 0.25f, 0.5f), 0.1f);
        public static Material Glow(string n, Color c) => Get(n, c, 0.6f, 0.8f);

        /// <summary>Creates a primitive child without its collider (visual only unless keepCollider).</summary>
        public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale,
            Material mat, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            if (!keepCollider)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);
            }
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            if (r != null) { r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
            return go;
        }
    }
}
