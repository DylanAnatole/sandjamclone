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
        bool previousFront;bool initialized;
        static readonly int Width=Shader.PropertyToID("_OutlineWidth");
        void LateUpdate()
        {
            if(!Skin || Actor.Shooter==null || Controller.Game==null)return;
            var lane=Controller.Game.Lanes[Actor.SourceLane];bool front=lane.Count>0 && lane.Peek()==Actor.Shooter;
            if(initialized && front==previousFront)return;
            if(properties==null)properties=new MaterialPropertyBlock();
            Skin.GetPropertyBlock(properties);properties.SetFloat(Width,front?.026f:.006f);Skin.SetPropertyBlock(properties);
            previousFront=front;initialized=true;
        }
    }
}
