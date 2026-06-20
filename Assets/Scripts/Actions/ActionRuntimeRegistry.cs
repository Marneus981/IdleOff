using System.Collections.Generic;
using UnityEngine;

namespace IdleOff.Actions
{
    public static class ActionRuntimeRegistry
    {
        private static readonly List<GameObject> RuntimeObjects = new();

        public static int ActiveCount
        {
            get
            {
                RuntimeObjects.RemoveAll(entry => entry == null);
                return RuntimeObjects.Count;
            }
        }

        public static void Register(GameObject runtimeObject)
        {
            if (runtimeObject != null && !RuntimeObjects.Contains(runtimeObject))
            {
                RuntimeObjects.Add(runtimeObject);
            }
        }

        public static void Unregister(GameObject runtimeObject)
        {
            RuntimeObjects.Remove(runtimeObject);
        }

        public static void ClearAll()
        {
            for (var i = RuntimeObjects.Count - 1; i >= 0; i--)
            {
                var runtimeObject = RuntimeObjects[i];
                RuntimeObjects.RemoveAt(i);
                DestroyRuntimeObject(runtimeObject);
            }
        }

        public static void DestroyRuntimeObject(GameObject runtimeObject)
        {
            if (runtimeObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(runtimeObject);
                return;
            }

            Object.DestroyImmediate(runtimeObject);
        }
    }
}
