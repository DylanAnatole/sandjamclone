using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Rounded cage and a separate sand volume: empty space is actual geometry, not a body cutout.
    public sealed class CharacterSandVessel : MonoBehaviour
    {
        public SceneActorView Actor;
        Transform sand;
        MeshRenderer fillRenderer;
        static Mesh frameMesh, sandMesh, legsMesh;
        static int users;
        bool initialized;
        public void Initialize(SceneActorView actor, Material material)
        {
            if(initialized)return;
            Actor=actor;users++;initialized=true;
            if(!frameMesh)BuildMeshes();
            var skin=actor.GetComponent<CharacterDepthStyle>().Skin;
            if(!legsMesh)legsMesh=Resources.Load<Mesh>("CharacterAnimation/CharacterLegs");
            if(!legsMesh)throw new System.InvalidOperationException("Generated CharacterLegs resource is missing");
            skin.sharedMesh=legsMesh;
            var frame=Part("Rounded vessel frame",frameMesh,material,actor.Visual);
            var block=new MaterialPropertyBlock();block.SetFloat("_GrainStrength",0);block.SetFloat("_OutlineWidth",.007f);frame.SetPropertyBlock(block);
            fillRenderer=Part("Contained sand volume",sandMesh,material,actor.Visual);sand=fillRenderer.transform;
            block=new MaterialPropertyBlock();block.SetFloat("_OutlineWidth",0);block.SetFloat("_ShadeStrength",.55f);block.SetFloat("_GrainContrast",2.2f);fillRenderer.SetPropertyBlock(block);
            var cover=actor.GetComponent<ReferenceQueueCover>();
            if(cover)cover.MaskedRenderers=cover.MaskedRenderers.Concat(new Renderer[]{frame,fillRenderer}).ToArray();
            Refresh();
        }
        void LateUpdate(){Refresh();}
        void Refresh()
        {
            if(!sand || Actor.Shooter==null)return;
            float amount=Actor.DisplayedFill;
            sand.localPosition=new Vector3(0,.285f,0);
            sand.localScale=new Vector3(1,.51f*Mathf.Max(.001f,amount),1);
            sand.gameObject.SetActive(amount>.001f);
        }
        static MeshRenderer Part(string name,Mesh mesh,Material material,Transform parent)
        {
            var obj=new GameObject(name);obj.transform.SetParent(parent,false);
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;return renderer;
        }
        static Vector3[] Loop(float width,float depth,float radius,float y)
        {
            var points=new List<Vector3>();
            for(int corner=0;corner<4;corner++)for(int step=0;step<5;step++)
            {
                float angle=(corner*90+step*22.5f)*Mathf.Deg2Rad;
                float x=(corner==0 || corner==3)?width*.5f-radius:-width*.5f+radius;
                float z=corner<2?depth*.5f-radius:-depth*.5f+radius;
                points.Add(new Vector3(x+Mathf.Cos(angle)*radius,y,z+Mathf.Sin(angle)*radius));
            }
            return points.ToArray();
        }
        static void Quad(List<Vector3> vertices,List<int> indices,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {
            int start=vertices.Count;vertices.AddRange(new[]{a,b,c,d});indices.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
        }
        static void Ring(List<Vector3> v,List<int> t,float y)
        {
            var outer=Loop(.74f,.57f,.095f,y);var inner=Loop(.65f,.48f,.055f,y);
            var up=Vector3.up*.035f;
            for(int i=0;i<outer.Length;i++)
            {
                int j=(i+1)%outer.Length;
                Quad(v,t,outer[i]+up,inner[i]+up,inner[j]+up,outer[j]+up);
                Quad(v,t,outer[j],inner[j],inner[i],outer[i]);
                Quad(v,t,outer[i],outer[i]+up,outer[j]+up,outer[j]);
                Quad(v,t,inner[j],inner[j]+up,inner[i]+up,inner[i]);
            }
        }
        static Mesh Finish(List<Vector3> v,List<int> t,string name)
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(t,0);
            mesh.SetUVs(0,v.Select(p=>new Vector2(p.x*.6f+p.y*.4f,p.z*.6f+p.y*.4f)).ToList());mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        static void BuildMeshes()
        {
            var v=new List<Vector3>();var t=new List<int>();Ring(v,t,.25f);Ring(v,t,.80f);
            foreach(float x in new[]{-.33f,.33f})foreach(float z in new[]{-.245f,.245f})
            {
                var low=Loop(.032f,.032f,.01f,.27f);var high=Loop(.032f,.032f,.01f,.815f);var offset=new Vector3(x,0,z);
                for(int i=0;i<low.Length;i++){int j=(i+1)%low.Length;Quad(v,t,low[i]+offset,high[i]+offset,high[j]+offset,low[j]+offset);}
            }
            frameMesh=Finish(v,t,"Rounded sand vessel frame");
            v=new List<Vector3>();t=new List<int>();var bottom=Loop(.65f,.48f,.07f,0);var top=Loop(.65f,.48f,.07f,1);
            for(int i=0;i<bottom.Length;i++)
            {
                int j=(i+1)%bottom.Length;
                Quad(v,t,bottom[i],top[i],top[j],bottom[j]);
                Quad(v,t,top[i],Vector3.up,Vector3.up,top[j]);
                Quad(v,t,bottom[j],Vector3.zero,Vector3.zero,bottom[i]);
            }
            sandMesh=Finish(v,t,"Rounded sand volume");
        }
        void OnDestroy(){if(initialized && --users==0){Destroy(frameMesh);Destroy(sandMesh);}}
    }
}
