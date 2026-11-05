using System.Collections.Generic;
using UnityEngine;

namespace SnowRunner
{
    /// <summary>
    /// Builds pooled obstacles / coins / power-ups from primitives and spawns them in rows
    /// across the three lanes as track segments are generated.
    /// </summary>
    public class ObstacleSpawner
    {
        static readonly int ObstacleKinds = System.Enum.GetValues(typeof(ObstacleKind)).Length;
        static readonly int PowerKinds = 5; // Magnet, Shield, Speed, Multiplier, CoinBonus

        readonly Transform root;
        readonly ObjectPool[] obstaclePools;
        readonly ObjectPool[] powerPools;
        readonly ObjectPool coinPool;
        readonly List<Obstacle> liveObstacles = new List<Obstacle>(64);
        readonly List<Pickup> livePickups = new List<Pickup>(128);

        public ObstacleSpawner(Transform parent)
        {
            root = new GameObject("SpawnedObjects").transform;
            root.SetParent(parent, false);

            obstaclePools = new ObjectPool[ObstacleKinds];
            for (int i = 0; i < ObstacleKinds; i++)
            {
                int k = i;
                obstaclePools[i] = new ObjectPool(() => BuildObstacle((ObstacleKind)k), root, 3);
            }
            powerPools = new ObjectPool[PowerKinds];
            for (int i = 0; i < PowerKinds; i++)
            {
                int k = i;
                powerPools[i] = new ObjectPool(() => BuildPowerUp((PickupKind)(k + 1)), root, 1);
            }
            coinPool = new ObjectPool(BuildCoin, root, 30);
        }

        // ---------------------------------------------------------------- spawning
        /// <param name="difficulty">0..1</param>
        public void Populate(float zStart, float length, float difficulty)
        {
            float spacing = Mathf.Lerp(15f, 9f, difficulty);
            float z = zStart + spacing * 0.5f;
            float zEnd = zStart + length;

            while (z < zEnd)
            {
                if (z < 45f) { z += spacing; continue; } // safe start zone

                int blocked = Random.value < 0.25f + 0.35f * difficulty ? 2 : 1;
                int freeLane = Random.Range(0, 3);

                var rowLow = new bool[3];
                int singleLane = (freeLane + 1 + Random.Range(0, 2)) % 3;
                for (int lane = 0; lane < 3; lane++)
                {
                    if (lane == freeLane) continue;
                    if (blocked == 1 && lane != singleLane) continue;
                    var kind = (ObstacleKind)Random.Range(0, ObstacleKinds);
                    SpawnObstacle(kind, lane, z);
                    rowLow[lane] = kind != ObstacleKind.BigRock && kind != ObstacleKind.IceBlock && kind != ObstacleKind.IceArch;
                }

                // Coins: a line in the free lane, plus arcs over low obstacles
                SpawnCoinLine(freeLane, z - spacing * 0.35f, 5, 2f, 0f);
                for (int lane = 0; lane < 3; lane++)
                    if (rowLow[lane] && Random.value < 0.5f) SpawnCoinArc(lane, z);

                // Power-up occasionally, in the free lane between rows
                if (Random.value < 0.12f)
                    SpawnPowerUp(Random.Range(0, PowerKinds), freeLane, z + spacing * 0.5f);

                z += spacing;
            }
        }

        void SpawnObstacle(ObstacleKind kind, int lane, float z)
        {
            var go = obstaclePools[(int)kind].Get();
            go.transform.position = new Vector3((lane - 1) * PlayerController.LaneWidth, 0f, z);
            go.transform.rotation = Quaternion.Euler(0f, kind == ObstacleKind.Snowmobile ? Random.Range(-15f, 15f) : Random.Range(-8f, 8f), 0f);
            var ob = go.GetComponent<Obstacle>();
            ob.OnRelease = ReleaseObstacle;
            liveObstacles.Add(ob);
        }

        void SpawnCoinLine(int lane, float zStart, int count, float step, float y)
        {
            for (int i = 0; i < count; i++)
                SpawnCoin(new Vector3((lane - 1) * PlayerController.LaneWidth, 1f + y, zStart + i * step));
        }

        void SpawnCoinArc(int lane, float z)
        {
            for (int i = -2; i <= 2; i++)
            {
                float h = 1f + 1.6f * (1f - (i * i) / 4f);
                SpawnCoin(new Vector3((lane - 1) * PlayerController.LaneWidth, h, z + i * 1.4f));
            }
        }

        void SpawnCoin(Vector3 pos)
        {
            var go = coinPool.Get();
            go.transform.position = pos;
            var p = go.GetComponent<Pickup>();
            p.BaseY = pos.y;
            p.OnRelease = ReleasePickup;
            livePickups.Add(p);
        }

        void SpawnPowerUp(int index, int lane, float z)
        {
            var go = powerPools[index].Get();
            go.transform.position = new Vector3((lane - 1) * PlayerController.LaneWidth, 1.3f, z);
            var p = go.GetComponent<Pickup>();
            p.BaseY = 1.3f;
            p.OnRelease = ReleasePickup;
            livePickups.Add(p);
        }

        // ---------------------------------------------------------------- lifetime
        public void Cleanup(float playerZ)
        {
            float limit = playerZ - 14f;
            for (int i = liveObstacles.Count - 1; i >= 0; i--)
            {
                var o = liveObstacles[i];
                if (o == null) { liveObstacles.RemoveAt(i); continue; }
                if (o.transform.position.z < limit) o.Release();
            }
            for (int i = livePickups.Count - 1; i >= 0; i--)
            {
                var p = livePickups[i];
                if (p == null) { livePickups.RemoveAt(i); continue; }
                if (p.transform.position.z < limit && !p.Collecting) p.Release();
            }
        }

        /// <summary>Removes obstacles in a z-range (used on revive so the player isn't hit again instantly).</summary>
        public void ClearObstacles(float fromZ, float toZ)
        {
            for (int i = liveObstacles.Count - 1; i >= 0; i--)
            {
                var o = liveObstacles[i];
                if (o == null) { liveObstacles.RemoveAt(i); continue; }
                float z = o.transform.position.z;
                if (z >= fromZ && z <= toZ) o.Release();
            }
        }

        public void ReleaseAll()
        {
            for (int i = liveObstacles.Count - 1; i >= 0; i--) if (liveObstacles[i] != null) liveObstacles[i].Release();
            for (int i = livePickups.Count - 1; i >= 0; i--) if (livePickups[i] != null) livePickups[i].Release();
            liveObstacles.Clear();
            livePickups.Clear();
            PickupRegistry.Clear();
        }

        void ReleaseObstacle(Obstacle o)
        {
            liveObstacles.Remove(o);
            if (o != null && o.gameObject.activeSelf) obstaclePools[(int)o.Kind].Release(o.gameObject);
        }

        void ReleasePickup(Pickup p)
        {
            livePickups.Remove(p);
            if (p == null || !p.gameObject.activeSelf) return;
            if (p.Kind == PickupKind.Coin) coinPool.Release(p.gameObject);
            else powerPools[(int)p.Kind - 1].Release(p.gameObject);
        }

        // ---------------------------------------------------------------- builders
        static GameObject NewRoot(string name)
        {
            return new GameObject(name);
        }

        static BoxCollider AddTriggerBox(GameObject go, Vector3 center, Vector3 size)
        {
            var b = go.AddComponent<BoxCollider>();
            b.isTrigger = true;
            b.center = center;
            b.size = size;
            return b;
        }

        static GameObject BuildObstacle(ObstacleKind kind)
        {
            var go = NewRoot(kind.ToString());
            var t = go.transform;
            var ob = go.AddComponent<Obstacle>();
            ob.Kind = kind;

            switch (kind)
            {
                case ObstacleKind.WoodenBox:
                    Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.6f, 0), new Vector3(1.2f, 1.2f, 1.2f), Mats.Wood);
                    Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, 1.22f, 0), new Vector3(1.28f, 0.14f, 1.28f), Mats.Snow);
                    Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.6f, 0), new Vector3(1.24f, 0.12f, 1.24f), Mats.DarkWood);
                    AddTriggerBox(go, new Vector3(0, 0.65f, 0), new Vector3(1.25f, 1.3f, 1.25f));
                    break;
                case ObstacleKind.IceBarrier:
                    Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.55f, 0), new Vector3(2.0f, 0.9f, 0.3f), Mats.Ice);
                    Mats.Prim(PrimitiveType.Cube, t, new Vector3(-0.95f, 0.5f, 0), new Vector3(0.18f, 1.0f, 0.4f), Mats.DeepIce);
                    Mats.Prim(PrimitiveType.Cube, t, new Vector3(0.95f, 0.5f, 0), new Vector3(0.18f, 1.0f, 0.4f), Mats.DeepIce);
                    Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, 1.03f, 0), new Vector3(2.0f, 0.1f, 0.34f), Mats.Snow);
                    AddTriggerBox(go, new Vector3(0, 0.55f, 0), new Vector3(2.1f, 1.1f, 0.5f));
                    break;
                case ObstacleKind.FallenTree:
                    var trunk = Mats.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.45f, 0), new Vector3(0.9f, 1.1f, 0.9f), Mats.DarkWood);
                    trunk.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                    var cap = Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.88f, 0), new Vector3(2.0f, 0.12f, 0.6f), Mats.Snow);
                    cap.transform.localRotation = Quaternion.identity;
                    AddTriggerBox(go, new Vector3(0, 0.45f, 0), new Vector3(2.3f, 0.95f, 0.95f));
                    break;
                case ObstacleKind.BigRock:
                    Mats.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.2f, 0), new Vector3(2.0f, 2.5f, 2.0f), Mats.Rock);
                    Mats.Prim(PrimitiveType.Sphere, t, new Vector3(0, 2.1f, 0), new Vector3(1.5f, 0.8f, 1.5f), Mats.Snow);
                    AddTriggerBox(go, new Vector3(0, 1.3f, 0), new Vector3(1.9f, 2.6f, 1.9f));
                    break;
                case ObstacleKind.IceBlock:
                    Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, 1.3f, 0), new Vector3(1.6f, 2.6f, 1.6f), Mats.DeepIce);
                    Mats.Prim(PrimitiveType.Cube, t, new Vector3(0.1f, 0.8f, -0.05f), new Vector3(1.4f, 1.4f, 1.7f), Mats.Ice);
                    Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, 2.62f, 0), new Vector3(1.7f, 0.12f, 1.7f), Mats.Snow);
                    AddTriggerBox(go, new Vector3(0, 1.3f, 0), new Vector3(1.65f, 2.65f, 1.65f));
                    break;
                case ObstacleKind.Snowmobile:
                    Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.6f, 0), new Vector3(1.0f, 0.55f, 2.0f), Mats.Red);
                    Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.95f, -0.45f), new Vector3(0.7f, 0.2f, 0.8f), Mats.Metal);
                    Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, 1.05f, 0.5f), new Vector3(0.8f, 0.5f, 0.1f), Mats.Ice);
                    Mats.Prim(PrimitiveType.Cube, t, new Vector3(-0.45f, 0.2f, 0.3f), new Vector3(0.14f, 0.1f, 2.0f), Mats.Metal);
                    Mats.Prim(PrimitiveType.Cube, t, new Vector3(0.45f, 0.2f, 0.3f), new Vector3(0.14f, 0.1f, 2.0f), Mats.Metal);
                    Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.92f, 0.1f), new Vector3(1.05f, 0.1f, 1.3f), Mats.Snow);
                    AddTriggerBox(go, new Vector3(0, 0.65f, 0.05f), new Vector3(1.2f, 1.3f, 2.2f));
                    break;
                case ObstacleKind.IceArch:
                    Mats.Prim(PrimitiveType.Cube, t, new Vector3(-1.0f, 1.0f, 0), new Vector3(0.25f, 2.2f, 0.4f), Mats.Wood);
                    Mats.Prim(PrimitiveType.Cube, t, new Vector3(1.0f, 1.0f, 0), new Vector3(0.25f, 2.2f, 0.4f), Mats.Wood);
                    Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, 1.75f, 0), new Vector3(2.4f, 0.4f, 0.5f), Mats.Ice);
                    Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, 2.0f, 0), new Vector3(2.5f, 0.12f, 0.6f), Mats.Snow);
                    for (int i = -2; i <= 2; i++)
                    {
                        Mats.Prim(PrimitiveType.Cylinder, t, new Vector3(i * 0.4f, 1.4f, 0), new Vector3(0.1f, 0.15f, 0.1f), Mats.Ice);
                    }
                    // Slide under: collider bottom at y=1.4, standing height is 1.8, sliding 0.8
                    AddTriggerBox(go, new Vector3(0, 1.75f, 0), new Vector3(2.2f, 0.7f, 0.6f));
                    break;
            }
            return go;
        }

        static GameObject BuildCoin()
        {
            var go = NewRoot("Coin");
            var p = go.AddComponent<Pickup>();
            p.Kind = PickupKind.Coin;
            p.BaseScale = Vector3.one;
            var disc = Mats.Prim(PrimitiveType.Cylinder, go.transform, Vector3.zero, new Vector3(0.7f, 0.06f, 0.7f), Mats.Gold);
            disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var sc = go.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.55f;
            MakeKinematic(go);
            return go;
        }

        static GameObject BuildPowerUp(PickupKind kind)
        {
            var go = NewRoot("PowerUp_" + kind);
            var t = go.transform;
            var p = go.AddComponent<Pickup>();
            p.Kind = kind;
            p.BaseScale = Vector3.one;

            switch (kind)
            {
                case PickupKind.Magnet:
                    var mm = Mats.Glow("MagnetRed", new Color(0.9f, 0.1f, 0.1f));
                    var mb = Mats.Glow("MagnetBlue", new Color(0.15f, 0.35f, 1f));
                    Mats.Prim(PrimitiveType.Cube, t, new Vector3(-0.3f, 0.1f, 0), new Vector3(0.25f, 0.7f, 0.25f), mm);
                    Mats.Prim(PrimitiveType.Cube, t, new Vector3(0.3f, 0.1f, 0), new Vector3(0.25f, 0.7f, 0.25f), mb);
                    Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, -0.3f, 0), new Vector3(0.85f, 0.25f, 0.25f), Mats.Metal);
                    break;
                case PickupKind.Shield:
                    Mats.Prim(PrimitiveType.Sphere, t, Vector3.zero, new Vector3(0.9f, 0.9f, 0.9f), Mats.Glow("ShieldCyan", new Color(0.2f, 0.85f, 1f)));
                    Mats.Prim(PrimitiveType.Cube, t, Vector3.zero, new Vector3(0.3f, 0.6f, 0.3f), Mats.Get("ShieldWhite", Color.white));
                    break;
                case PickupKind.SpeedBoost:
                    var sg = Mats.Glow("SpeedGreen", new Color(0.55f, 1f, 0.2f));
                    var b1 = Mats.Prim(PrimitiveType.Cube, t, new Vector3(0.1f, 0.25f, 0), new Vector3(0.25f, 0.65f, 0.25f), sg);
                    b1.transform.localRotation = Quaternion.Euler(0, 0, 25f);
                    var b2 = Mats.Prim(PrimitiveType.Cube, t, new Vector3(-0.1f, -0.25f, 0), new Vector3(0.25f, 0.65f, 0.25f), sg);
                    b2.transform.localRotation = Quaternion.Euler(0, 0, 25f);
                    break;
                case PickupKind.ScoreMultiplier:
                    var dm = Mats.Prim(PrimitiveType.Cube, t, Vector3.zero, new Vector3(0.6f, 0.6f, 0.6f), Mats.Glow("MultPurple", new Color(0.75f, 0.3f, 1f)));
                    dm.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
                    Mats.Prim(PrimitiveType.Sphere, t, Vector3.zero, new Vector3(0.3f, 0.3f, 0.3f), Mats.Gold);
                    break;
                case PickupKind.CoinBonus:
                    Mats.Prim(PrimitiveType.Sphere, t, new Vector3(0, -0.1f, 0), new Vector3(0.75f, 0.65f, 0.75f), Mats.Get("BagBrown", new Color(0.55f, 0.35f, 0.15f)));
                    Mats.Prim(PrimitiveType.Sphere, t, new Vector3(0, 0.3f, 0), new Vector3(0.3f, 0.2f, 0.3f), Mats.Gold);
                    break;
            }
            var sc = go.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.85f;
            MakeKinematic(go);
            return go;
        }

        // Pickups move every frame (spin/bob/magnet); a kinematic Rigidbody keeps that cheap for physics.
        static void MakeKinematic(GameObject go)
        {
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }
}
