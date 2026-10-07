using System;
using System.Collections.Generic;
using System.Linq;
namespace SandJamTest.Scene3D
{
    // Managed reference used by editor regression checks; gameplay uses the Burst version.
    public sealed class SandPourSimulation
    {
        public struct Grain { public int Index, X, Y, PathStep; }
        public readonly int[] Rows, Cols;
        public readonly int Width, InletX, InletY;
        public readonly List<Grain> Moving=new List<Grain>();
        public readonly bool[] Filled;
        public int Emitted {get;private set;}
        public int Settled {get;private set;}
        public int Requested {get;private set;}
        public bool Complete {get{return Settled==Requested;}}
        readonly bool[] occupied;readonly SandRegionFlowLayout layout;readonly HashSet<int> mask;
        public SandPourSimulation(int[] rows,int[] cols){
            layout=new SandRegionFlowLayout(rows,cols);Rows=rows;Cols=cols;Width=layout.Width;InletX=layout.InletX;InletY=layout.InletY;
            mask=new HashSet<int>(Enumerable.Range(0,rows.Length).Select(i=>rows[i]*Width+cols[i]));
            Filled=new bool[rows.Length];occupied=new bool[rows.Length];Moving.Capacity=rows.Length;
        }
        public bool Inside(int x,int y){return x>=0 && x<Width && y>=0 && mask.Contains(y*Width+x);}
        public void Request(int count){if(count<Requested || count>Rows.Length)throw new ArgumentOutOfRangeException("count");Requested=count;}
        public void Step(){
            for(int n=0;n<Moving.Count;){
                var g=Moving[n];int nextStep=g.PathStep+1;
                if(nextStep<layout.Starts[g.Index+1]){
                    int next=layout.PathCells[nextStep];
                    if(!occupied[next]){occupied[layout.PathCells[g.PathStep]]=false;occupied[next]=true;g.PathStep=nextStep;g.X=Cols[next];g.Y=Rows[next];}
                }
                if(g.PathStep==layout.Starts[g.Index+1]-1){Filled[g.Index]=true;Settled++;Moving.RemoveAt(n);}else{Moving[n]=g;n++;}
            }
            if(Emitted<Requested && !occupied[layout.InletIndex]){
                int i=layout.Order[Emitted++];Moving.Add(new Grain{Index=i,X=InletX,Y=InletY,PathStep=layout.Starts[i]});occupied[layout.InletIndex]=true;
            }
        }
        public void Validate(){
            var unique=new HashSet<int>();int count=0;
            for(int i=0;i<Filled.Length;i++)if(Filled[i]){unique.Add(Rows[i]*Width+Cols[i]);count++;}
            foreach(var g in Moving)if(!Inside(g.X,g.Y) || !unique.Add(g.Y*Width+g.X))throw new InvalidOperationException("Sand escaped or overlapped");
            if(count!=Settled || Settled+Moving.Count!=Emitted || occupied.Count(v=>v)!=Emitted)throw new InvalidOperationException("Sand conservation failed");
        }
    }
}
