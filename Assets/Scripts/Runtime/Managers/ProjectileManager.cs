using System;
using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Owns projectile presentation and reuse; game rules stay in SandGame.
    public sealed class ProjectileManager
    {
        readonly Transform ProjectileRoot;
        readonly GameObject ProjectilePrefab;
        readonly Material[] ProjectileMaterials;
        readonly int[] MaterialColorIds;
        readonly List<Bolt> bolts = new List<Bolt>();
        readonly Queue<GameObject> boltPool = new Queue<GameObject>();
        sealed class Bolt { public GameObject Object; public Vector3 From, To; public float Time; }

        public ProjectileManager(Transform root, GameObject prefab, Material[] materials, int[] colorIds)
        { ProjectileRoot = root; ProjectilePrefab = prefab; ProjectileMaterials = materials; MaterialColorIds = colorIds; }

        public void Reset()
        {
            foreach (var bolt in bolts) ReturnBolt(bolt.Object);
            bolts.Clear();
        }

        public void Advance(float delta)
        {
            for (int i = bolts.Count - 1; i >= 0; i--)
            {
                var bolt = bolts[i]; bolt.Time += delta;
                float t = Mathf.Clamp01(bolt.Time / .24f);
                bolt.Object.transform.position = Vector3.Lerp(bolt.From, bolt.To, t) + Vector3.back * Mathf.Sin(t * Mathf.PI) * .8f;
                if (t >= 1) { ReturnBolt(bolt.Object); bolts.RemoveAt(i); }
            }
        }

        public void Spawn(Vector3 from, Vector3 to, int color)
        {
            var obj = boltPool.Count > 0 ? boltPool.Dequeue() : UnityEngine.Object.Instantiate(ProjectilePrefab, ProjectileRoot);
            obj.GetComponent<Renderer>().sharedMaterial = ProjectileMaterials[Array.IndexOf(MaterialColorIds, color)];
            obj.transform.position = from; obj.SetActive(true);
            bolts.Add(new Bolt { Object = obj, From = from, To = to });
        }
        void ReturnBolt(GameObject obj) { obj.SetActive(false); boltPool.Enqueue(obj); }

    }
}
