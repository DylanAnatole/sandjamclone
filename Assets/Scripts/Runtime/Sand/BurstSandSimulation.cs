using System;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
namespace SandJamTest.Scene3D
{
    public sealed class BurstSandSimulation : IDisposable
    {
        public readonly int[] Rows,Cols;
        public readonly int Width,InletX,InletY;
        public NativeArray<byte> Filled;
        public NativeArray<SandPourSimulation.Grain> Moving;
        NativeArray<int> rows,cols,order,state,paths,starts;
        NativeArray<byte> occupied,mask;
        readonly int inletIndex, height;
        JobHandle handle;bool pending,disposed;
        public int Settled {get{return state[1];}}
        public int Requested {get{return state[2];}}
        public int MovingCount {get{return state[3];}}
        public bool Complete {get{return Settled==Requested;}}
        public bool UsedBurst {get{return state[4]==1;}}
        public BurstSandSimulation(int[] sourceRows,int[] sourceCols)
        {
            var layout=new SandRegionFlowLayout(sourceRows,sourceCols);
            Rows=sourceRows;Cols=sourceCols;Width=layout.Width;InletX=layout.InletX;InletY=layout.InletY;inletIndex=layout.InletIndex;height=Rows.Max()+1;
            rows=new NativeArray<int>(Rows,Allocator.Persistent);cols=new NativeArray<int>(Cols,Allocator.Persistent);
            order=new NativeArray<int>(layout.Order,Allocator.Persistent);
            paths=new NativeArray<int>(layout.PathCells,Allocator.Persistent);starts=new NativeArray<int>(layout.Starts,Allocator.Persistent);
            state=new NativeArray<int>(5,Allocator.Persistent);Filled=new NativeArray<byte>(Rows.Length,Allocator.Persistent);
            Moving=new NativeArray<SandPourSimulation.Grain>(Rows.Length,Allocator.Persistent);
            occupied=new NativeArray<byte>(Rows.Length,Allocator.Persistent);mask=new NativeArray<byte>(height*Width,Allocator.Persistent);
            for(int i=0;i<Rows.Length;i++)mask[Rows[i]*Width+Cols[i]]=1;
        }
        public bool Inside(int x,int y){return x>=0 && x<Width && y>=0 && y<height && mask[y*Width+x]!=0;}
        public void Request(int count){CompleteSteps();if(count<Requested || count>Rows.Length)throw new ArgumentOutOfRangeException("count");state[2]=count;}
        public void ScheduleSteps(int steps)
        {
            CompleteSteps();if(steps<=0 || Complete)return;
            handle=new StepJob{Rows=rows,Cols=cols,Order=order,Paths=paths,Starts=starts,InletIndex=inletIndex,State=state,Filled=Filled,Moving=Moving,Occupied=occupied,Width=Width,InletX=InletX,InletY=InletY,Steps=steps}.Schedule();pending=true;
        }
        public void CompleteSteps(){if(!pending)return;handle.Complete();pending=false;}
        public void Validate()
        {
            CompleteSteps();int filled=0,occupiedCount=0;var unique=new System.Collections.Generic.HashSet<int>();
            for(int i=0;i<Filled.Length;i++)if(Filled[i]!=0){filled++;unique.Add(Rows[i]*Width+Cols[i]);}
            for(int i=0;i<MovingCount;i++){var g=Moving[i];if(!Inside(g.X,g.Y) || !unique.Add(g.Y*Width+g.X))throw new InvalidOperationException("Burst grains overlap/overshoot");}
            for(int i=0;i<occupied.Length;i++)occupiedCount+=occupied[i];
            if(filled!=Settled || Settled+MovingCount!=state[0] || occupiedCount!=state[0])throw new InvalidOperationException("Burst sand conservation failed");
        }
        public void Dispose()
        {
            if(disposed)return;CompleteSteps();disposed=true;
            paths.Dispose();starts.Dispose();rows.Dispose();cols.Dispose();order.Dispose();state.Dispose();Filled.Dispose();Moving.Dispose();occupied.Dispose();mask.Dispose();
        }
        [BurstCompile(CompileSynchronously=true)]
        public struct StepJob : IJob
        {
            [ReadOnly] public NativeArray<int> Rows,Cols,Order,Paths,Starts;
            public NativeArray<int> State;
            public NativeArray<byte> Filled,Occupied;
            public NativeArray<SandPourSimulation.Grain> Moving;
            public int Width,InletX,InletY,InletIndex,Steps;
            [BurstDiscard] static void MarkManaged(ref int enabled){enabled=0;}
            public void Execute()
            {
                int burst=1;MarkManaged(ref burst);State[4]=burst;
                for(int step=0;step<Steps && State[1]<State[2];step++)
                {
                    int write=0;
                    for(int n=0;n<State[3];n++)
                    {
                        var g=Moving[n];int nextStep=g.PathStep+1;
                        if(nextStep<Starts[g.Index+1]){
                            int next=Paths[nextStep];
                            if(Occupied[next]==0){Occupied[Paths[g.PathStep]]=0;Occupied[next]=1;g.PathStep=nextStep;g.X=Cols[next];g.Y=Rows[next];}
                        }
                        if(g.PathStep==Starts[g.Index+1]-1){Filled[g.Index]=1;State[1]++;}else Moving[write++]=g;
                    }
                    State[3]=write;
                    if(State[0]<State[2] && Occupied[InletIndex]==0)
                    {
                        int index=Order[State[0]++];Moving[State[3]++]=new SandPourSimulation.Grain{Index=index,X=InletX,Y=InletY,PathStep=Starts[index]};Occupied[InletIndex]=1;
                    }
                }
            }
        }
    }
}
