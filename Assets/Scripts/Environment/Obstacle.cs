using System;
using UnityEngine;

namespace SnowRunner
{
    public enum ObstacleKind { WoodenBox, IceBarrier, FallenTree, BigRock, IceBlock, Snowmobile, IceArch }

    /// <summary>Marker on pooled obstacles. Collider on the same object is a trigger.</summary>
    public class Obstacle : MonoBehaviour
    {
        public ObstacleKind Kind;
        public Action<Obstacle> OnRelease;

        public void Release()
        {
            if (OnRelease != null) OnRelease(this); else gameObject.SetActive(false);
        }
    }
}
