using System;
using System.Collections.Generic;
using System.Linq;

namespace SandJamTest.Scene3D
{
    // Presentation simulation. A cell can contain exactly one grain (moving or settled).
    // One inlet feeds a vertical channel; grains spread sideways above the fill surface.
    public sealed class SandPourSimulation
    {
        public struct Grain { public int Index, X, Y, TurnRow; }
        public readonly int[] Rows, Cols;
        public readonly int Width, InletX, InletY;
        public readonly List<Grain> Moving = new List<Grain>();
        public readonly bool[] Filled;
        public int Emitted { get; private set; }
        public int Settled { get; private set; }
        public int Requested { get; private set; }
        public bool Complete { get { return Settled == Requested; } }
        readonly bool[] occupied;
        readonly HashSet<int> mask;
        readonly int[] order;

        public SandPourSimulation(int[] rows, int[] cols)
        {
            Moving.Capacity=rows.Length;
            Rows=rows; Cols=cols; Width=cols.Max()+1;
            InletX=(cols.Min()+cols.Max())/2;
            InletY=rows.Max()+5;
            occupied=new bool[(InletY+1)*Width]; Filled=new bool[rows.Length];
            mask=new HashSet<int>(Enumerable.Range(0,rows.Length).Select(i=>Key(cols[i],rows[i])));
            // Fill complete low rows before building higher rows, alternating both sides.
            order=Enumerable.Range(0,rows.Length).OrderBy(i=>rows[i]).ThenBy(i=>Math.Abs(cols[i]-InletX)).ThenBy(i=>cols[i]).ToArray();
        }
        int Key(int x,int y) { return y*Width+x; }
        public bool Inside(int x,int y) { return mask.Contains(Key(x,y)); }
        public void Request(int count)
        {
            if(count<Requested || count>Rows.Length) throw new ArgumentOutOfRangeException("count");
            Requested=count;
        }
        public void Step()
        {
            // Oldest grains move first; following grains wait rather than intersect.
            for(int n=0;n<Moving.Count;)
            {
                var g=Moving[n]; int targetX=Cols[g.Index], targetY=Rows[g.Index];
                int x=g.X, y=g.Y;
                if(x!=targetX && y<=g.TurnRow) x+=Math.Sign(targetX-x);
                else if(y>targetY) y--;
                if(!occupied[Key(x,y)])
                {
                    occupied[Key(g.X,g.Y)]=false; occupied[Key(x,y)]=true;
                    g.X=x; g.Y=y;
                }
                if(g.X==targetX && g.Y==targetY)
                {
                    Filled[g.Index]=true; Settled++; Moving.RemoveAt(n);
                }
                else { Moving[n]=g; n++; }
            }
            if(Emitted<Requested && !occupied[Key(InletX,InletY)])
            {
                int i=order[Emitted++];
                Moving.Add(new Grain {Index=i,X=InletX,Y=InletY,TurnRow=Rows[i]+1});
                occupied[Key(InletX,InletY)]=true;
            }
        }
        public void Validate()
        {
            var unique=new HashSet<int>(); int count=0;
            for(int i=0;i<Filled.Length;i++) if(Filled[i]) { unique.Add(Key(Cols[i],Rows[i])); count++; }
            foreach(var g in Moving)
            {
                if(g.Y<Rows[g.Index] || g.Y>InletY || !unique.Add(Key(g.X,g.Y))) throw new InvalidOperationException("Sand grains overlap or overshoot.");
            }
            if(count!=Settled || Settled+Moving.Count!=Emitted || occupied.Count(x=>x)!=Emitted)
                throw new InvalidOperationException("Sand conservation failed.");
        }
    }
}


