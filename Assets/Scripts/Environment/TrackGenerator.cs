using System.Collections.Generic;
using UnityEngine;

namespace SnowRunner
{
    /// <summary>
    /// Endless track: a ring of pooled segments. When the player passes a segment it is
    /// moved to the front, re-decorated and re-populated with obstacles / coins / power-ups.
    /// </summary>
    public class TrackGenerator
    {
        public const float SegmentLength = 40f;
        const int SegmentCount = 7;

        readonly Transform root;
        readonly ObstacleSpawner spawner;
        readonly List<TrackSegment> segments = new List<TrackSegment>();
        float nextZ;
        Transform player;

        public TrackGenerator(Transform parent, ObstacleSpawner spawner)
        {
            this.spawner = spawner;
            root = new GameObject("Track").transform;
            root.SetParent(parent, false);
            for (int i = 0; i < SegmentCount; i++) segments.Add(new TrackSegment(root, i));
        }

        public void Begin(Transform playerTransform)
        {
            player = playerTransform;
            nextZ = -SegmentLength; // one segment behind the start
            for (int i = 0; i < segments.Count; i++) Place(segments[i], 0f);
        }

        void Place(TrackSegment seg, float difficulty)
        {
            seg.Place(nextZ);
            spawner.Populate(nextZ, SegmentLength, difficulty);
            nextZ += SegmentLength;
        }

        public void Tick(float difficulty)
        {
            if (player == null) return;
            var first = segments[0];
            if (player.position.z - (first.Z + SegmentLength) > 20f)
            {
                segments.RemoveAt(0);
                Place(first, difficulty);
                segments.Add(first);
            }
        }
    }

    /// <summary>One 40m piece of snowy scenery. All children are built once and re-randomised on reuse.</summary>
    public class TrackSegment
    {
        readonly GameObject root;
        readonly List<GameObject> trees = new List<GameObject>();
        readonly List<GameObject> rocks = new List<GameObject>();
        readonly GameObject cabinL, cabinR, bridgeL, bridgeR, snowmanL, snowmanR, lampL, lampR;
        public float Z { get; private set; }

        static void NoShadow(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public TrackSegment(Transform parent, int index)
        {
            root = new GameObject("Segment_" + index);
            root.transform.SetParent(parent, false);
            var t = root.transform;
            float h = TrackGenerator.SegmentLength * 0.5f;

            // running pathway (3 lanes) + packed snow edge
            var path = Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, -0.5f, h), new Vector3(8.4f, 1f, TrackGenerator.SegmentLength + 0.1f), Mats.Snow);
            path.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // side terrain
            for (int s = -1; s <= 1; s += 2)
            {
                var side = Mats.Prim(PrimitiveType.Cube, t, new Vector3(s * 19f, -0.55f, h), new Vector3(30f, 1f, TrackGenerator.SegmentLength + 0.1f), Mats.Get("SideSnow", new Color(0.88f, 0.94f, 1f)));
                side.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                // frozen river strip
                var river = Mats.Prim(PrimitiveType.Cube, t, new Vector3(s * 8.4f, -0.02f, h), new Vector3(3.6f, 0.08f, TrackGenerator.SegmentLength + 0.1f), Mats.Ice);
                river.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                // ice crystals along the river
                Mats.Prim(PrimitiveType.Cube, t, new Vector3(s * 6.4f, 0.25f, h), new Vector3(0.2f, 0.5f, TrackGenerator.SegmentLength), Mats.Snow);
            }

            for (int i = 0; i < 12; i++) trees.Add(BuildTree(t));
            for (int i = 0; i < 5; i++) rocks.Add(BuildRock(t));
            cabinL = BuildCabin(t); cabinR = BuildCabin(t);
            bridgeL = BuildBridge(t); bridgeR = BuildBridge(t);
            snowmanL = BuildSnowman(t); snowmanR = BuildSnowman(t);
            lampL = BuildLamp(t); lampR = BuildLamp(t);
        }

        public void Place(float z)
        {
            Z = z;
            root.transform.position = new Vector3(0f, 0f, z);

            foreach (var tr in trees)
            {
                int side = Random.value < 0.5f ? -1 : 1;
                float x = side * Random.Range(11.5f, 30f);
                tr.transform.localPosition = new Vector3(x, 0f, Random.Range(0f, TrackGenerator.SegmentLength));
                float s = Random.Range(0.8f, 1.5f);
                tr.transform.localScale = new Vector3(s, s * Random.Range(0.9f, 1.3f), s);
                tr.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
            foreach (var r in rocks)
            {
                int side = Random.value < 0.5f ? -1 : 1;
                r.transform.localPosition = new Vector3(side * Random.Range(5.2f, 18f), 0f, Random.Range(0f, TrackGenerator.SegmentLength));
                float s = Random.Range(0.7f, 1.6f);
                r.transform.localScale = new Vector3(s, s, s);
                r.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }

            PlaceCabin(cabinL, -1); PlaceCabin(cabinR, 1);
            PlaceBridge(bridgeL, -1); PlaceBridge(bridgeR, 1);
            PlaceDecor(snowmanL, -1, 5.4f, 0.35f); PlaceDecor(snowmanR, 1, 5.4f, 0.35f);
            PlaceDecor(lampL, -1, 4.8f, 0.5f, true); PlaceDecor(lampR, 1, 4.8f, 0.5f, true);
        }

        void PlaceCabin(GameObject g, int side)
        {
            bool on = Random.value < 0.35f;
            g.SetActive(on);
            if (!on) return;
            g.transform.localPosition = new Vector3(side * Random.Range(14f, 20f), 0f, Random.Range(6f, 34f));
            g.transform.localRotation = Quaternion.Euler(0f, side > 0 ? -90f : 90f, 0f);
        }

        void PlaceBridge(GameObject g, int side)
        {
            bool on = Random.value < 0.3f;
            g.SetActive(on);
            if (!on) return;
            g.transform.localPosition = new Vector3(side * 8.4f, 0f, Random.Range(6f, 34f));
        }

        void PlaceDecor(GameObject g, int side, float x, float chance, bool alwaysMid = false)
        {
            bool on = alwaysMid || Random.value < chance;
            g.SetActive(on);
            if (!on) return;
            float z = alwaysMid ? (side < 0 ? 10f : 30f) : Random.Range(4f, 36f);
            g.transform.localPosition = new Vector3(side * x, 0f, z);
            g.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }

        // ---------------------------------------------------------------- builders
        static GameObject BuildTree(Transform parent)
        {
            var g = new GameObject("PineTree");
            g.transform.SetParent(parent, false);
            Mats.Prim(PrimitiveType.Cylinder, g.transform, new Vector3(0, 0.6f, 0), new Vector3(0.35f, 0.6f, 0.35f), Mats.DarkWood);
            MeshFactory.ConeObject(g.transform, new Vector3(0, 0.9f, 0), new Vector3(3.0f, 2.2f, 3.0f), Mats.Pine);
            MeshFactory.ConeObject(g.transform, new Vector3(0, 2.2f, 0), new Vector3(2.3f, 1.9f, 2.3f), Mats.Pine);
            MeshFactory.ConeObject(g.transform, new Vector3(0, 3.4f, 0), new Vector3(1.5f, 1.6f, 1.5f), Mats.Pine);
            // snow on the tiers (slightly smaller, raised cones)
            MeshFactory.ConeObject(g.transform, new Vector3(0, 1.25f, 0), new Vector3(2.7f, 1.9f, 2.7f), Mats.Snow);
            MeshFactory.ConeObject(g.transform, new Vector3(0, 2.5f, 0), new Vector3(2.0f, 1.6f, 2.0f), Mats.Snow);
            MeshFactory.ConeObject(g.transform, new Vector3(0, 3.7f, 0), new Vector3(1.2f, 1.3f, 1.2f), Mats.Snow);
            NoShadow(g);
            return g;
        }

        static GameObject BuildRock(Transform parent)
        {
            var g = new GameObject("SnowRock");
            g.transform.SetParent(parent, false);
            Mats.Prim(PrimitiveType.Sphere, g.transform, new Vector3(0, 0.45f, 0), new Vector3(1.6f, 1.0f, 1.3f), Mats.Rock);
            Mats.Prim(PrimitiveType.Sphere, g.transform, new Vector3(0, 0.8f, 0), new Vector3(1.2f, 0.45f, 1.0f), Mats.Snow);
            NoShadow(g);
            return g;
        }

        static GameObject BuildCabin(Transform parent)
        {
            var g = new GameObject("Cabin");
            g.transform.SetParent(parent, false);
            var t = g.transform;
            Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, 1.4f, 0), new Vector3(5f, 2.8f, 4f), Mats.Wood);
            // log stripes
            for (int i = 0; i < 4; i++) Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.4f + i * 0.7f, 0), new Vector3(5.1f, 0.12f, 4.1f), Mats.DarkWood);
            var roofA = Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, 3.3f, -1.05f), new Vector3(5.6f, 0.3f, 2.6f), Mats.DarkWood);
            roofA.transform.localRotation = Quaternion.Euler(-30f, 0, 0);
            var roofB = Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, 3.3f, 1.05f), new Vector3(5.6f, 0.3f, 2.6f), Mats.DarkWood);
            roofB.transform.localRotation = Quaternion.Euler(30f, 0, 0);
            var snowA = Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, 3.5f, -1.05f), new Vector3(5.7f, 0.25f, 2.7f), Mats.Snow);
            snowA.transform.localRotation = Quaternion.Euler(-30f, 0, 0);
            var snowB = Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, 3.5f, 1.05f), new Vector3(5.7f, 0.25f, 2.7f), Mats.Snow);
            snowB.transform.localRotation = Quaternion.Euler(30f, 0, 0);
            Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.9f, 2.02f), new Vector3(1f, 1.8f, 0.1f), Mats.Get("Door", new Color(0.25f, 0.15f, 0.08f)));
            Mats.Prim(PrimitiveType.Cube, t, new Vector3(1.6f, 1.7f, 2.02f), new Vector3(0.9f, 0.8f, 0.1f), Mats.Glow("WarmWindow", new Color(1f, 0.8f, 0.4f)));
            Mats.Prim(PrimitiveType.Cube, t, new Vector3(1.6f, 3.9f, -1.2f), new Vector3(0.6f, 1.2f, 0.6f), Mats.Rock); // chimney
            NoShadow(g);
            return g;
        }

        static GameObject BuildBridge(Transform parent)
        {
            var g = new GameObject("SnowBridge");
            g.transform.SetParent(parent, false);
            var t = g.transform;
            Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.25f, 0), new Vector3(5.2f, 0.25f, 2.2f), Mats.Wood);
            Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.42f, 0), new Vector3(5.3f, 0.1f, 2.3f), Mats.Snow);
            for (int s = -1; s <= 1; s += 2)
            {
                Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.9f, s * 1.05f), new Vector3(5.2f, 0.12f, 0.12f), Mats.DarkWood);
                Mats.Prim(PrimitiveType.Cube, t, new Vector3(-2.4f, 0.65f, s * 1.05f), new Vector3(0.12f, 0.8f, 0.12f), Mats.DarkWood);
                Mats.Prim(PrimitiveType.Cube, t, new Vector3(2.4f, 0.65f, s * 1.05f), new Vector3(0.12f, 0.8f, 0.12f), Mats.DarkWood);
            }
            NoShadow(g);
            return g;
        }

        static GameObject BuildSnowman(Transform parent)
        {
            var g = new GameObject("Snowman");
            g.transform.SetParent(parent, false);
            var t = g.transform;
            Mats.Prim(PrimitiveType.Sphere, t, new Vector3(0, 0.55f, 0), new Vector3(1.1f, 1.1f, 1.1f), Mats.Snow);
            Mats.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.35f, 0), new Vector3(0.8f, 0.8f, 0.8f), Mats.Snow);
            Mats.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.95f, 0), new Vector3(0.55f, 0.55f, 0.55f), Mats.Snow);
            Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, 1.97f, 0.28f), new Vector3(0.08f, 0.08f, 0.3f), Mats.Get("Carrot", new Color(1f, 0.5f, 0.1f)));
            Mats.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 2.3f, 0), new Vector3(0.45f, 0.12f, 0.45f), Mats.Get("TopHat", new Color(0.1f, 0.1f, 0.12f)));
            Mats.Prim(PrimitiveType.Cube, t, new Vector3(0, 1.4f, 0), new Vector3(0.85f, 0.12f, 0.85f), Mats.Red); // scarf
            NoShadow(g);
            return g;
        }

        static GameObject BuildLamp(Transform parent)
        {
            var g = new GameObject("WinterLamp");
            g.transform.SetParent(parent, false);
            var t = g.transform;
            Mats.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 1.4f, 0), new Vector3(0.14f, 1.4f, 0.14f), Mats.Metal);
            Mats.Prim(PrimitiveType.Sphere, t, new Vector3(0, 2.9f, 0), new Vector3(0.45f, 0.45f, 0.45f), Mats.Glow("LampGlow", new Color(1f, 0.92f, 0.6f)));
            Mats.Prim(PrimitiveType.Sphere, t, new Vector3(0, 3.15f, 0), new Vector3(0.6f, 0.2f, 0.6f), Mats.Snow);
            // string of festive lights
            Mats.Prim(PrimitiveType.Sphere, t, new Vector3(0.25f, 2.5f, 0), new Vector3(0.15f, 0.15f, 0.15f), Mats.Glow("LightR", new Color(1f, 0.25f, 0.25f)));
            Mats.Prim(PrimitiveType.Sphere, t, new Vector3(-0.25f, 2.3f, 0), new Vector3(0.15f, 0.15f, 0.15f), Mats.Glow("LightG", new Color(0.3f, 1f, 0.4f)));
            NoShadow(g);
            return g;
        }
    }
}
