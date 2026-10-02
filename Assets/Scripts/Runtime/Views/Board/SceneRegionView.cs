using System;
using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    public static class DisplayAmount
    {
        public static int Units(int raw,int divider) { return raw<=0?0:1+(raw-1)/Math.Max(1,divider); }
    }
    public sealed class SceneRegionView : MonoBehaviour
    {
        public int PartIndex;
        public Color EmptyTint = new Color(.88f,.93f,.95f);
        public float OpenTintStrength = .32f;
        public bool OverrideSandColor;
        public Color SandColor = Color.white;
        public MeshFilter Geometry; // Serialized geometry retained as an editor reference only.
        public TextMesh Counter;
        public Transform Target;
        public int Divider=40;
        public int SettledCount { get { return sand==null?0:sand.Settled; } }
        public int FlyingCount { get { return sand==null?0:sand.Moving.Count; } }
        public bool IsSettled { get { return sand!=null && sand.Complete; } }
        public SandBoardTextureView Board { get; private set; }
        SandPourSimulation sand;
        Color32 solid, empty;
        Color32[] settledColors;
        readonly List<Vector2Int> previousMoving=new List<Vector2Int>(256);
        int lastRemaining=-1;
        bool lastOpen, lastVisible;
        float stepTime, completionGlow;
        const float StepSeconds=1f/300f;
        public void AttachBoard(SandBoardTextureView board)
        {
            Board=board; Geometry.GetComponent<MeshRenderer>().enabled=false;
            // Labels revealed after startup must sort above the shared transparent board sprite.
            foreach (var renderer in Counter.GetComponentsInChildren<MeshRenderer>(true)) renderer.sortingOrder=10;
        }
        public void ValidateFlow() { if(sand!=null) sand.Validate(); }
        void ClearMoving()
        {
            foreach(var p in previousMoving) Board.ClearGrain(p.x,p.y);
            previousMoving.Clear();
        }
        public void ResetFlow()
        {
            ClearMoving(); sand=null; lastRemaining=-1; stepTime=0;completionGlow=0;
        }
        public void Apply(Region region)
        {
            if(sand==null)
            {
                sand=new SandPourSimulation(region.Data.rows,region.Data.cols);
                Target.position=Geometry.transform.TransformPoint(new Vector3((sand.InletX+.5f)*.075f-3.15f,(sand.InletY+.5f)*.075f+.4f,-.03f));
                solid=OverrideSandColor?SandColor:SandJamDemo.Palette(region.Data.ColorType);
                settledColors=new Color32[sand.Filled.Length];
                for(int i=0;i<settledColors.Length;i++) { Color tint=(Color)solid*(.88f+((i*37)%17)*.012f); tint.a=1; settledColors[i]=tint; }
            }
            if(region.Remaining==lastRemaining && region.Open==lastOpen && region.InformationVisible==lastVisible) return;
            lastRemaining=region.Remaining; lastOpen=region.Open; lastVisible=region.InformationVisible;
            sand.Request((int)((long)(region.Data.amount-region.Remaining)*region.Data.rows.Length/region.Data.amount));
            empty=Color.Lerp(EmptyTint,(Color)solid,region.InformationVisible?OpenTintStrength:0f);
            Counter.gameObject.SetActive(region.InformationVisible);
            var backing=transform.Find("Amount backing"); if(backing) backing.gameObject.SetActive(region.InformationVisible);
            Counter.text=!region.InformationVisible || region.Remaining==0?"":DisplayAmount.Units(region.Remaining,Divider).ToString();
            PaintBase(); PaintMoving();
        }
        void PaintBase()
        {
            for(int i=0;i<sand.Filled.Length;i++) Board.SetBase(sand.Cols[i],sand.Rows[i],sand.Filled[i]?(Color32)Color.Lerp(settledColors[i],Color.white,.12f*completionGlow):empty);
        }
        void PaintMoving()
        {
            // Restore previous pixel positions before drawing the current frame.
            ClearMoving();
            foreach(var g in sand.Moving)
                if(sand.Inside(g.X,g.Y)) { Board.SetGrain(g.X,g.Y,solid); previousMoving.Add(new Vector2Int(g.X,g.Y)); }
        }
        public void Advance(float delta)
        {
            if(sand==null || delta<=0) return;
            if(IsSettled)
            {
                if(lastRemaining==0 && completionGlow<1)
                {
                    completionGlow=Mathf.MoveTowards(completionGlow,1,delta/.35f);
                    PaintBase(); Counter.gameObject.SetActive(false);
                    var backing=transform.Find("Amount backing");if(backing)backing.gameObject.SetActive(false);
                }
                return;
            }
            int before=sand.Settled;
            stepTime+=delta;
            while(stepTime>=StepSeconds) { stepTime-=StepSeconds; sand.Step(); }
            if(before!=sand.Settled) PaintBase();
            PaintMoving();
            if(IsSettled && lastRemaining==0) { Counter.text="";Counter.gameObject.SetActive(false); }
        }
    }
}

