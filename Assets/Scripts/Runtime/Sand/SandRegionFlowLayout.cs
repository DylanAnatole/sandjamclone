using System;
using System.Collections.Generic;
using System.Linq;

namespace SandJamTest.Scene3D
{
    // One immutable route tree per region. Parents remain free until all descendants fill.
    public sealed class SandRegionFlowLayout
    {
        public readonly int[] Rows, Cols, Order, PathCells, Starts;
        public readonly int InletIndex, Width, InletX, InletY;
        public SandRegionFlowLayout(int[] rows, int[] cols)
        {
            if(rows==null || cols==null || rows.Length==0 || rows.Length!=cols.Length)
                throw new ArgumentException("Invalid sand region");
            Rows=rows;Cols=cols;Width=cols.Max()+1;
            float middle=(cols.Min()+cols.Max())*.5f;
            InletX=cols.Distinct().OrderBy(x=>Math.Abs(x-middle)).ThenBy(x=>x).First();
            InletY=rows.Where((y,i)=>cols[i]==InletX).Max();
            InletIndex=Enumerable.Range(0,rows.Length).First(i=>cols[i]==InletX && rows[i]==InletY);
            var indices=new Dictionary<int,int>();
            for(int i=0;i<rows.Length;i++)indices.Add(rows[i]*Width+cols[i],i);
            var parent=Enumerable.Repeat(-1,rows.Length).ToArray();parent[InletIndex]=InletIndex;
            var queue=new Queue<int>();queue.Enqueue(InletIndex);var reached=new List<int>();
            // Prefer descending/sideways paths. Only enclosed concave tips require upward steps.
            int[] dx={0,-1,1,-1,1,0,-1,1},dy={-1,-1,-1,0,0,1,1,1};
            Action<int> expand=limit=>{
                while(queue.Count>0){int current=queue.Dequeue();reached.Add(current);
                    for(int d=0;d<limit;d++){
                        int x=cols[current]+dx[d],y=rows[current]+dy[d],next;
                        if(x<0 || x>=Width || y<0 || !indices.TryGetValue(y*Width+x,out next) || parent[next]>=0)continue;
                        parent[next]=current;queue.Enqueue(next);
                    }
                }
            };
            expand(5);
            foreach(int i in reached.ToArray())queue.Enqueue(i);
            expand(8);
            if(parent.Any(i=>i<0))throw new ArgumentException("A sand region must be connected");
            var children=new int[rows.Length];for(int i=0;i<rows.Length;i++)if(i!=InletIndex)children[parent[i]]++;
            var leaves=new SortedSet<int>(Comparer<int>.Create((a,b)=>{
                int c=rows[a].CompareTo(rows[b]);if(c!=0)return c;
                c=Math.Abs(cols[a]-InletX).CompareTo(Math.Abs(cols[b]-InletX));if(c!=0)return c;
                c=cols[a].CompareTo(cols[b]);return c!=0?c:a.CompareTo(b);
            }));
            for(int i=0;i<rows.Length;i++)if(children[i]==0)leaves.Add(i);
            Order=new int[rows.Length];int at=0;
            while(leaves.Count>0){int i=leaves.Min;leaves.Remove(i);Order[at++]=i;if(i!=InletIndex && --children[parent[i]]==0)leaves.Add(parent[i]);}
            Starts=new int[rows.Length+1];var paths=new List<int>();var reverse=new List<int>();
            for(int i=0;i<rows.Length;i++){
                Starts[i]=paths.Count;reverse.Clear();int node=i;
                while(node!=InletIndex){reverse.Add(node);node=parent[node];}
                paths.Add(InletIndex);for(int j=reverse.Count-1;j>=0;j--)paths.Add(reverse[j]);
            }
            Starts[rows.Length]=paths.Count;PathCells=paths.ToArray();
        }
    }
}
