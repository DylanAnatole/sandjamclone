using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

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
        public int FlyingCount { get { return sand==null?0:sand.MovingCount; } }
        public bool IsSettled { get { return sand!=null && sand.Complete; } }
        public SandBoardTextureView Board { get; private set; }
        public Texture2D FlowMask { get; private set; }
        BurstSandSimulation sand;
        int beforeStep;bool advancing;
        Color32 solid, empty;
        NativeArray<Color32> settledColors, paintedColors;
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
            advancing=false; ClearMoving(); ReleaseColors(); if(sand!=null)sand.Dispose(); sand=null; lastRemaining=-1; stepTime=0;completionGlow=0;
        }
        public void Apply(Region region)
        {
            if(sand==null)
            {
                sand=new BurstSandSimulation(region.Data.rows,region.Data.cols);
                Target.position=Board.transform.TransformPoint(new Vector3((sand.InletX+.5f)*SandBoardTextureView.CellSize,(sand.InletY+.5f)*SandBoardTextureView.CellSize,0));
                if(!FlowMask){
                    FlowMask=new Texture2D(Board.Width,Board.Height,TextureFormat.RGBA32,false){name="Region flow mask "+PartIndex,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
                    var maskPixels=new Color32[Board.Width*Board.Height];
                    for(int i=0;i<sand.Rows.Length;i++)maskPixels[sand.Rows[i]*Board.Width+sand.Cols[i]]=new Color32(255,255,255,255);
                    FlowMask.SetPixels32(maskPixels);FlowMask.Apply(false,true);
                }
                solid=OverrideSandColor?SandColor:SandJamDemo.Palette(region.Data.ColorType);
                settledColors=new NativeArray<Color32>(sand.Filled.Length,Allocator.Persistent);
                paintedColors=new NativeArray<Color32>(sand.Filled.Length,Allocator.Persistent);
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
            // Run avoids dispatch/wait overhead for these small regions while using Burst native code.
            new PaintRegionJob{Filled=sand.Filled,Colors=settledColors,Output=paintedColors,Empty=empty,Glow=.12f*completionGlow}.Run();
            for(int i=0;i<sand.Filled.Length;i++) Board.SetBase(sand.Cols[i],sand.Rows[i],paintedColors[i]);
        }
        void PaintMoving()
        {
            // Restore previous pixel positions before drawing the current frame.
            ClearMoving();
            for(int i=0;i<sand.MovingCount;i++){var g=sand.Moving[i];
                if(sand.Inside(g.X,g.Y)) { Board.SetGrain(g.X,g.Y,solid); previousMoving.Add(new Vector2Int(g.X,g.Y)); }}
        }
        void ReleaseColors(){if(settledColors.IsCreated)settledColors.Dispose();if(paintedColors.IsCreated)paintedColors.Dispose();}
        void OnDestroy(){if(sand!=null)sand.Dispose();ReleaseColors();if(FlowMask)Destroy(FlowMask);}
        [BurstCompile]
        public struct PaintRegionJob : IJob
        {
            [ReadOnly] public NativeArray<byte> Filled;
            [ReadOnly] public NativeArray<Color32> Colors;
            [WriteOnly] public NativeArray<Color32> Output;
            public Color32 Empty;
            public float Glow;
            public void Execute()
            {
                for(int i=0;i<Output.Length;i++)
                {
                    if(Filled[i]==0){Output[i]=Empty;continue;}
                    Color32 c=Colors[i];
                    Output[i]=(Color32)Color.Lerp((Color)c,Color.white,Glow);
                }
            }
        }
        public void Advance(float delta){BeginAdvance(delta);FinishAdvance();}
        public void BeginAdvance(float delta)
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
            beforeStep=sand.Settled;
            stepTime+=delta;
            int steps=0;while(stepTime>=StepSeconds){stepTime-=StepSeconds;steps++;}
            sand.ScheduleSteps(steps);advancing=true;
        }
        public void FinishAdvance()
        {
            if(!advancing)return;advancing=false;sand.CompleteSteps();
            if(beforeStep!=sand.Settled) PaintBase();
            PaintMoving();
            if(IsSettled && lastRemaining==0) { Counter.text="";Counter.gameObject.SetActive(false); }
        }
    }
}

