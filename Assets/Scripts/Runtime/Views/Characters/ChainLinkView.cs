using UnityEngine;

namespace SandJamTest.Scene3D
{
    public sealed class ChainLinkView : MonoBehaviour
    {
        public SceneActorView First, Second;
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
            }
        }
        void LateUpdate()
        {
            bool visible = First && Second && First.gameObject.activeInHierarchy && Second.gameObject.activeInHierarchy;
            foreach (var strap in straps) strap.enabled = visible;
            if (!visible) return;
            for (int i = 0; i < straps.Length; i++)
            {
                var offset = new Vector3(0, .3f + i * .25f, -.82f);
                var a = First.transform.position + offset;
                var b = Second.transform.position + offset;
                var inset = (b - a).normalized * Mathf.Min(.24f, Vector3.Distance(a,b) * .25f);
                straps[i].SetPosition(0, a + inset); straps[i].SetPosition(1, b - inset);
            }
        }
        void OnDestroy() { if (material) Destroy(material); }
    }
}
