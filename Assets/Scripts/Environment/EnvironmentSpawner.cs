using UnityEngine;

namespace SnowRunner
{
    /// <summary>Winter atmosphere: lighting, fog, mountains and snowfall. Also shared particle helpers.</summary>
    public class EnvironmentSpawner
    {
        public static readonly Color SkyColor = new Color(0.62f, 0.78f, 0.93f);
        public static readonly Color FogColor = new Color(0.78f, 0.88f, 0.97f);

        Transform mountains;
        Transform snowfall;

        public static void ApplyLighting(Camera cam)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.7f, 0.82f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogStartDistance = 55f;
            RenderSettings.fogEndDistance = 210f;

            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = FogColor;
                cam.fieldOfView = 62f;
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 320f;
                cam.allowHDR = false;
                cam.allowMSAA = true;
            }

            var lightGo = new GameObject("Sun (Winter Light)");
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(1f, 0.96f, 0.9f);
            l.intensity = 1.05f;
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.55f;
            lightGo.transform.rotation = Quaternion.Euler(48f, -30f, 0f);
            QualitySettings.shadowDistance = 45f;
        }

        public void Build(Transform parent, Camera cam)
        {
            BuildMountains(parent);
            BuildSnowfall(parent, cam);
        }

        void BuildMountains(Transform parent)
        {
            mountains = new GameObject("Mountains").transform;
            mountains.SetParent(parent, false);
            var rng = new System.Random(42);
            var rockMat = Mats.Get("MountainRock", new Color(0.55f, 0.65f, 0.78f));
            for (int i = 0; i < 18; i++)
            {
                int side = i % 2 == 0 ? -1 : 1;
                float x = side * (70f + (float)rng.NextDouble() * 70f);
                float z = (i / 2) * 32f - 30f + (float)rng.NextDouble() * 12f;
                float h = 55f + (float)rng.NextDouble() * 55f;
                float w = h * (1.1f + (float)rng.NextDouble() * 0.5f);
                MeshFactory.ConeObject(mountains, new Vector3(x, -1f, z), new Vector3(w, h, w), rockMat);
                MeshFactory.ConeObject(mountains, new Vector3(x, h * 0.55f - 1f, z), new Vector3(w * 0.5f, h * 0.45f, w * 0.5f), Mats.Mountain);
            }
        }

        void BuildSnowfall(Transform parent, Camera cam)
        {
            var go = new GameObject("Snowfall");
            go.transform.SetParent(parent, false);
            snowfall = go.transform;

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 6f;
            main.startSpeed = 0.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.gravityModifier = 0.35f;
            main.maxParticles = 700;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = Color.white;

            var em = ps.emission;
            em.rateOverTime = 110f;

            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(50f, 1f, 70f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 0.3f;

            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = SnowFx.ParticleMaterial();
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            go.transform.position = new Vector3(0f, 14f, 30f);
            ps.Play();
        }

        public void Tick(Vector3 playerPos)
        {
            if (mountains != null) mountains.position = new Vector3(0f, 0f, playerPos.z * 0.985f);
            if (snowfall != null) snowfall.position = new Vector3(playerPos.x, 14f, playerPos.z + 25f);
        }
    }

    /// <summary>Particle helpers: soft snow material, footstep puffs, collection bursts.</summary>
    public static class SnowFx
    {
        static Material particleMat;
        static ParticleSystem[] bursts;
        static int burstIndex;

        public static Material ParticleMaterial()
        {
            if (particleMat != null) return particleMat;
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            tex.Apply();
            var shader = Shader.Find("Sprites/Default");
            particleMat = new Material(shader) { mainTexture = tex, color = Color.white };
            return particleMat;
        }

        /// <summary>Snow kicked up behind the player while running.</summary>
        public static ParticleSystem CreateFootPuff(Transform parent)
        {
            var go = new GameObject("FootSnow");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.05f, -0.3f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop();
            var main = ps.main;
            main.startLifetime = 0.5f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.gravityModifier = 0.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(1f, 1f, 1f, 0.8f);
            var em = ps.emission; em.rateOverTime = 30f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 25f; sh.radius = 0.2f;
            sh.rotation = new Vector3(-120f, 0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMaterial();
            return ps;
        }

        /// <summary>Pooled (round-robin) particle burst for coin/power-up collection.</summary>
        public static void Burst(Vector3 pos, Color color, int count = 14)
        {
            if (bursts == null || bursts[0] == null)
            {
                bursts = new ParticleSystem[8];
                var root = new GameObject("BurstFX").transform;
                for (int i = 0; i < bursts.Length; i++)
                {
                    var go = new GameObject("Burst" + i);
                    go.transform.SetParent(root, false);
                    var ps = go.AddComponent<ParticleSystem>();
                    ps.Stop();
                    var main = ps.main;
                    main.loop = false;
                    main.playOnAwake = false;
                    main.startLifetime = 0.55f;
                    main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
                    main.gravityModifier = 0.5f;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                    var em = ps.emission; em.rateOverTime = 0f;
                    var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.2f;
                    go.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMaterial();
                    bursts[i] = ps;
                }
            }
            var b = bursts[burstIndex];
            burstIndex = (burstIndex + 1) % bursts.Length;
            b.transform.position = pos;
            var m = b.main;
            m.startColor = color;
            b.Emit(count);
        }
    }
}
