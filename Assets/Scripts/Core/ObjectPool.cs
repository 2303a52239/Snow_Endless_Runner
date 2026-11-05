using System;
using System.Collections.Generic;
using UnityEngine;

namespace SnowRunner
{
    /// <summary>Simple GameObject pool. Objects are created lazily through a factory and reused.</summary>
    public class ObjectPool
    {
        readonly Func<GameObject> factory;
        readonly Stack<GameObject> free = new Stack<GameObject>();
        readonly Transform root;

        public ObjectPool(Func<GameObject> factory, Transform root, int prewarm = 0)
        {
            this.factory = factory;
            this.root = root;
            for (int i = 0; i < prewarm; i++) Release(Create());
        }

        GameObject Create()
        {
            var go = factory();
            go.transform.SetParent(root, false);
            return go;
        }

        public GameObject Get()
        {
            GameObject go = null;
            while (free.Count > 0 && go == null) go = free.Pop();
            if (go == null) go = Create();
            go.SetActive(true);
            return go;
        }

        public void Release(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            go.transform.SetParent(root, false);
            free.Push(go);
        }
    }
}
