using System;
using System.Collections.Generic;
using UnityEngine;

namespace SnowRunner
{
    public enum PickupKind { Coin, Magnet, Shield, SpeedBoost, ScoreMultiplier, CoinBonus }

    /// <summary>Marker/state for coins and power-ups (pooled). Animated centrally by PickupRegistry.</summary>
    public class Pickup : MonoBehaviour
    {
        public PickupKind Kind;
        public Action<Pickup> OnRelease;

        [NonSerialized] public bool Collecting;
        [NonSerialized] public float CollectTimer;
        [NonSerialized] public float BaseY;
        [NonSerialized] public Vector3 BaseScale = Vector3.one;

        void OnEnable()
        {
            Collecting = false;
            CollectTimer = 0f;
            transform.localScale = BaseScale;
            PickupRegistry.Add(this);
        }

        void OnDisable() { PickupRegistry.Remove(this); }

        public void Release()
        {
            if (OnRelease != null) OnRelease(this); else gameObject.SetActive(false);
        }
    }

    /// <summary>One loop animates every active pickup (spin, bob, magnet pull, collect animation).</summary>
    public static class PickupRegistry
    {
        static readonly List<Pickup> active = new List<Pickup>(128);
        static readonly List<Pickup> toRelease = new List<Pickup>(16);

        public static void Add(Pickup p) { if (!active.Contains(p)) active.Add(p); }
        public static void Remove(Pickup p) { active.Remove(p); }
        public static void Clear() { active.Clear(); toRelease.Clear(); }

        public static void Tick(float dt, Vector3 playerPos, bool magnet, float magnetRadius, float time)
        {
            toRelease.Clear();
            for (int i = 0; i < active.Count; i++)
            {
                var p = active[i];
                if (p == null) continue;
                var t = p.transform;

                if (p.Collecting)
                {
                    p.CollectTimer += dt;
                    float k = Mathf.Clamp01(p.CollectTimer / 0.25f);
                    t.Rotate(0f, 720f * dt, 0f, Space.World);
                    t.localScale = p.BaseScale * (1f + 0.6f * k) * (1f - k);
                    t.position += Vector3.up * (3f * dt);
                    if (k >= 1f) toRelease.Add(p);
                    continue;
                }

                t.Rotate(0f, 150f * dt, 0f, Space.World);

                if (magnet && p.Kind == PickupKind.Coin)
                {
                    Vector3 to = playerPos + Vector3.up - t.position;
                    if (to.sqrMagnitude < magnetRadius * magnetRadius)
                    {
                        t.position += to.normalized * (28f * dt);
                        continue;
                    }
                }
                var pos = t.position;
                pos.y = p.BaseY + Mathf.Sin(time * 3f + pos.z * 0.3f) * 0.12f;
                t.position = pos;
            }
            for (int i = 0; i < toRelease.Count; i++) toRelease[i].Release();
        }
    }
}
