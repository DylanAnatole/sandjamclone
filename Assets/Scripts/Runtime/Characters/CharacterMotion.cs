using UnityEngine;
namespace SandJamTest.Scene3D
{
    // Drives the recovered rig clips; translation remains owned by SceneActorView.
    public sealed class CharacterMotion : MonoBehaviour
    {
        public Animator Animator;
        public bool WalkContinuously;
        public bool Walking { get; private set; }
        static readonly int WalkingId=UnityEngine.Animator.StringToHash("Walking");
        void OnEnable(){if(Animator){Animator.Rebind();SetWalking(WalkContinuously);Animator.Update(0);}}
        public void SetWalking(bool value)
        {
            Walking=value||WalkContinuously;
            if(Animator)Animator.SetBool(WalkingId,Walking);
        }
        public void ResetPose()
        {
            if(!Animator)return;
            Animator.Rebind();SetWalking(WalkContinuously);Animator.Play(WalkContinuously?"Walk":"Sit",0,WalkContinuously?0:1);Animator.Update(0);
        }
    }
}
