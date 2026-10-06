using System;
using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // One reusable stream per occupied slot. No separate projectile per ammo tick.
    public sealed class ProjectileManager
    {
        readonly Transform root;
        readonly Material[] materials;
        readonly int[] colorIds;
        readonly Dictionary<int, Stream> streams = new Dictionary<int, Stream>();
        sealed class Stream { public LineRenderer Line; public Vector3 From, To; public float Remaining, Phase; }
        public ProjectileManager(Transform root, GameObject prefab, Material[] materials, int[] colorIds)
        {
            this.root=root;this.colorIds=colorIds;this.materials=new Material[materials.Length];
            var shader=Shader.Find("SandJamTest/SandStream");
            for(int i=0;i<materials.Length;i++)this.materials[i]=new Material(shader){color=materials[i].color};
        }
        public void Dispose(){foreach(var material in materials)UnityEngine.Object.Destroy(material);}
        public void Reset()
        {
            foreach(var stream in streams.Values) { stream.Remaining=0;stream.Line.enabled=false; }
        }
        public void Spawn(Vector3 from, Vector3 to, int color, int slot=0)
        {
            Stream stream;
            if(!streams.TryGetValue(slot,out stream))
            {
                var obj=new GameObject("Sand stream slot "+(slot+1));obj.transform.SetParent(root,false);
                var line=obj.AddComponent<LineRenderer>();line.useWorldSpace=true;line.positionCount=3;
                line.startWidth=.036f;line.endWidth=.024f;line.numCapVertices=2;
                line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;
                stream=new Stream{Line=line,Phase=slot*1.73f};streams.Add(slot,stream);
            }
            from.z=Mathf.Min(from.z,-.8f);to.z=Mathf.Min(to.z,-.8f);
            stream.From=from;stream.To=to;stream.Remaining=.13f;
            stream.Line.sharedMaterial=materials[Array.IndexOf(colorIds,color)];stream.Line.enabled=true;
            Draw(stream);
        }
        static void Draw(Stream stream)
        {
            stream.Line.SetPosition(0,stream.From);
            stream.Line.SetPosition(1,Vector3.Lerp(stream.From,stream.To,.55f)+Vector3.right*Mathf.Sin(stream.Phase)*.008f+Vector3.back*.12f);
            stream.Line.SetPosition(2,stream.To);
        }
        public void Advance(float delta)
        {
            foreach(var stream in streams.Values)
            {
                stream.Remaining-=delta;
                if(stream.Remaining<=0){stream.Line.enabled=false;continue;}
                stream.Phase+=delta*30;Draw(stream);
            }
        }
    }
}
