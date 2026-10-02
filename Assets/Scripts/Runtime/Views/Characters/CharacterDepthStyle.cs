using UnityEngine;
namespace SandJamTest.Scene3D
{
    // Emphasize selectable front-row characters without duplicating per-character materials.
    public sealed class CharacterDepthStyle : MonoBehaviour
    {
        public SceneActorView Actor;
        public SandJamSceneController Controller;
        public SkinnedMeshRenderer Skin;
        MaterialPropertyBlock properties;
        static readonly int Width=Shader.PropertyToID("_OutlineWidth");
        void LateUpdate()
        {
            if(!Skin || Actor.Shooter==null || Controller.Game==null)return;
            var lane=Controller.Game.Lanes[Actor.SourceLane];bool front=lane.Count>0 && lane.Peek()==Actor.Shooter;
            if(properties==null)properties=new MaterialPropertyBlock();
            Skin.GetPropertyBlock(properties);
            float amount=Actor.DisplayedFill;
            properties.SetFloat(Width,Mathf.Lerp(.024f,front?.026f:.006f,amount));
            properties.SetFloat("_FillEnabled",1);properties.SetFloat("_FillAmount",amount);
            properties.SetFloat("_FillBottom",Skin.bounds.min.y+.12f*Skin.bounds.size.y);
            properties.SetFloat("_FillTop",Skin.bounds.max.y);
            var color=Skin.sharedMaterial.GetColor("_Color");
            properties.SetColor("_OutlineColor",Color.Lerp(color,new Color(.045f,.025f,.07f),amount));
            Skin.SetPropertyBlock(properties);
        }
    }
}
