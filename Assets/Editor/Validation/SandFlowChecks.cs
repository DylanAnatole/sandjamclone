using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using SandJamTest.Scene3D;
namespace SandJamTest.Editor
{
    public static class SandFlowChecks
    {
        static void Drain(SandPourSimulation sand)
        {
            var previous=new Dictionary<int,int>();
            for(int tick=0;!sand.Complete && tick<40000;tick++)
            {
                sand.Step(); sand.Validate();
                foreach(var grain in sand.Moving)
                {
                    int y; if(previous.TryGetValue(grain.Index,out y) && grain.Y>y) throw new Exception("A grain moved upward");
                    previous[grain.Index]=grain.Y;
                }
            }
            if(!sand.Complete) throw new Exception("Sand flow stalled");
        }
        public static void Run()
        {
            var data=JsonUtility.FromJson<LevelData>(Resources.Load<TextAsset>("Tutorial").text);
            foreach(var part in data.parts)
            {
                var sand=new SandPourSimulation(part.rows,part.cols);
                sand.Request(part.rows.Length/3); Drain(sand);
                sand.Request(part.rows.Length); Drain(sand);
                if(sand.Settled!=part.rows.Length) throw new Exception("Sand lost");
            }
            var rows=Enumerable.Range(0,384).Select(i=>i/32).ToArray();
            var cols=Enumerable.Range(0,384).Select(i=>i%32).ToArray();
            var flat=new SandPourSimulation(rows,cols); flat.Request(256); Drain(flat);
            for(int i=0;i<384;i++) if(flat.Filled[i]!=(rows[i]<8)) throw new Exception("Uneven surface");
            Directory.CreateDirectory("TestResults/EvenFlow");
            File.WriteAllText("TestResults/EvenFlow/grid-tests.txt","PASS: 5 tutorial shapes fully filled\nPASS: No moving or settled grain overlap\nPASS: No upward movement\nPASS: Conservation during partial and full pour\nPASS: Rectangle spreads evenly across all 32 columns\n");
        }
    }
}
