using UnityEngine;

namespace SandJamTest.Scene3D
{
    public sealed class ChainLinkView : MonoBehaviour
    {
        public SceneActorView First, Second;
        public bool IsInQueue { get; private set; } = true;
        LineRenderer[] straps;
        Material material;
        void Awake()
        {
            material = new Material(Shader.Find("Sprites/Default"));
            straps = new LineRenderer[2];
            for (int i = 0; i < straps.Length; i++)
            {
                var child = new GameObject("Chain strap " + i);
                child.transform.SetParent(transform, false);
                var line = child.AddComponent<LineRenderer>();
                line.sharedMaterial = material; line.positionCount = 2;
                line.useWorldSpace = true; line.startWidth = line.endWidth = .065f;
                line.numCapVertices = 3;
                line.startColor = line.endColor = new Color(1, .83f, .12f);
                straps[i] = line;
                line.enabled = IsInQueue;
            }
        }
        public void SetInQueue(bool inQueue)
        {
            IsInQueue = inQueue;
            // Play prepares the queue while its screen is inactive, before Awake runs.
            if (straps == null) return;
            foreach (var strap in straps) strap.enabled = inQueue;
        }
        void LateUpdate()
        {
            bool visible = IsInQueue && First && Second && First.gameObject.activeInHierarchy && Second.gameObject.activeInHierarchy;
            foreach (var strap in straps) strap.enabled = visible;
            if (!visible) return;
            for (int i = 0; i < straps.Length; i++)
            {
                var offset = new Vector3(0, .3f + i * .25f, -.82f);
                var a = First.transform.position + offset;
                var b = Second.transform.position + offset;
                var inset = (b - a).normalized * Mathf.Min(.24f, Vector3.Distance(a,b) * .25f);
                if(First.SourceLane==Second.SourceLane)
                {
                    var upper=First.SourceOrder<Second.SourceOrder?First:Second;
                    var lower=upper==First?Second:First;
                    float side=i==0?1:-1;
                    a=upper.Visual.TransformPoint(new Vector3(side*.29f,.27f,-.30f));
                    b=lower.Visual.TransformPoint(new Vector3(-side*.29f,.81f,-.30f));
                    a.z=b.z=-.85f;inset=Vector3.zero;
                }
                var firstSkin=First.GetComponent<CharacterDepthStyle>();var secondSkin=Second.GetComponent<CharacterDepthStyle>();
                if(firstSkin && secondSkin){straps[i].startColor=firstSkin.Skin.sharedMaterial.color;straps[i].endColor=secondSkin.Skin.sharedMaterial.color;}
                straps[i].SetPosition(0, a + inset); straps[i].SetPosition(1, b - inset);
            }
        }
        void OnDestroy() { if (material) Destroy(material); }
    }
}
