using System;
using System.Collections.Generic;
using System.Linq;

namespace SandJamTest
{
    // Pure simulation. The view advances this with a fixed timestep.
    // Prototype rule: finishing ANY linked region opens an ordinary region.
    public sealed class SandGame
    {
        public readonly LevelData Data;
        public readonly Region[] Regions;
        public readonly Queue<Shooter>[] Lanes;
        public readonly Shooter[] Slots;
        public GameState State { get; private set; }
        public int TotalRequired { get; private set; }
        public int SpentAmmo { get; private set; }
        public int Moves { get; private set; }
        public int Revision { get; private set; }
        public int Remaining { get { return Regions.Sum(p => p.Remaining); } }
        public int InitialSupply { get; private set; }
        public int Supply { get { return Lanes.Sum(l => l.Sum(s => s.Ammo)) + Slots.Where(s => s != null).Sum(s => s.Ammo); } }

        readonly Func<Region, bool> isCovered;
        readonly bool useSlotUnlocks;
        public SandGame(LevelData data, int capacity = 6, Func<Region, bool> coverageCheck = null, bool enableSlotUnlocks = false)
        {
            Validate(data);
            useSlotUnlocks = enableSlotUnlocks;
            if (capacity < 1) throw new ArgumentOutOfRangeException("capacity");
            Data = data; isCovered = coverageCheck;
            Regions = data.parts.Select(p => new Region(p)).ToArray();
            Lanes = data.laneData.Select(l => new Queue<Shooter>(l.ColorAmmoDatas.Select(c => new Shooter(c)))).ToArray();
            Slots = new Shooter[capacity];
            TotalRequired = Remaining;
            InitialSupply = Supply;
            State = GameState.Playing;
        }

        public static void Validate(LevelData data)
        {
            if (data == null || data.rowCount < 1 || data.columnCount < 1 || data.parts == null || data.parts.Length == 0 || data.laneData == null)
                throw new ArgumentException("Level data is incomplete.");
            var names = new HashSet<string>();
            var pixels = new HashSet<int>();
            foreach (var p in data.parts)
            {
                if (string.IsNullOrEmpty(p.name) || !names.Add(p.name)) throw new ArgumentException("Duplicate region ID.");
                if (p.amount <= 0 || p.rows == null || p.cols == null || p.rows.Length != p.cols.Length || p.rows.Length == 0)
                    throw new ArgumentException("Invalid region: " + p.name);
                for (int i = 0; i < p.rows.Length; i++)
                {
                    int r = p.rows[i], c = p.cols[i];
                    if (r < 0 || r >= data.rowCount || c < 0 || c >= data.columnCount || !pixels.Add(r * data.columnCount + c))
                        throw new ArgumentException("Invalid or duplicate pixel: " + p.name);
                }
            }
            foreach (var p in data.parts)
                foreach (var other in p.prerequests ?? new string[0])
                    if (!names.Contains(other)) throw new ArgumentException("Missing region: " + other);
            if (data.opr == null || data.opc == null || data.opr.Length != data.opc.Length) throw new ArgumentException("Invalid obstacle coordinates.");
            for (int i = 0; i < data.opr.Length; i++)
            {
                int r = data.opr[i], c = data.opc[i];
                if (r < 0 || r >= data.rowCount || c < 0 || c >= data.columnCount || !pixels.Add(r * data.columnCount + c))
                    throw new ArgumentException("Invalid or overlapping obstacle.");
            }
            foreach (var lane in data.laneData)
            {
                if (lane == null || lane.ColorAmmoDatas == null) throw new ArgumentException("Missing lane.");
                foreach (var c in lane.ColorAmmoDatas)
                {
                    if (c.AmmoCount <= 0) throw new ArgumentException("Invalid ammo.");
                    if (c.IsChain || c.IsSecret || c.IsFreeze || c.IsHalf || c.IsUnlocker)
                        throw new ArgumentException("This test scene supports ordinary tutorial characters only.");
                }
            }
        }

        public bool SelectLane(int lane)
        {
            if (State != GameState.Playing || lane < 0 || lane >= Lanes.Length || Lanes[lane].Count == 0) return false;
            int slot = Enumerable.Range(0,Slots.Length).Where(i => Slots[i] == null && SlotRemaining(i)==0).DefaultIfEmpty(-1).First();
            if (slot < 0) return false;
            Slots[slot] = Lanes[lane].Dequeue();
            Moves++;
            Revision++;
            Evaluate();
            return true;
        }

        // Exported slot thresholds are displayed units, while SpentAmmo is raw ammo.
        // Enabled only for the video scene; older prototypes keep their existing behavior.
        public int SlotRemaining(int index)
        {
            if(!useSlotUnlocks || Data.gridSlotNeedAmmoCount==null || index>=Data.gridSlotNeedAmmoCount.Length)return 0;
            return Math.Max(0,Data.gridSlotNeedAmmoCount[index]-SpentAmmo/Math.Max(1,Data.uiDivider));
        }

        public int Target(Shooter shooter)
        {
            if (shooter == null || shooter.Ammo <= 0) return -1;
            return Array.FindIndex(Regions, p => p.Open && p.Remaining > 0 && p.Data.ColorType == shooter.Color);
        }

        public List<Shot> Tick(int ammoPerShooter = 20, Func<int, bool> canShoot = null)
        {
            var shots = new List<Shot>();
            if (State != GameState.Playing || ammoPerShooter <= 0) return shots;
            OpenNeighbours();
            for (int slot = 0; slot < Slots.Length; slot++)
            {
                if (canShoot != null && !canShoot(slot)) continue;
                var shooter = Slots[slot];
                int target = Target(shooter);
                if (target < 0) continue;
                var region = Regions[target];
                int amount = Math.Min(ammoPerShooter, Math.Min(shooter.Ammo, region.Remaining));
                shooter.Ammo -= amount;
                region.Remaining -= amount;
                SpentAmmo += amount;
                shots.Add(new Shot { Slot = slot, Region = target, Color = shooter.Color, Amount = amount });
                if (shooter.Ammo == 0) Slots[slot] = null;
                if (region.Remaining == 0) OpenNeighbours();
                Revision++;
            }
            Evaluate();
            return shots;
        }

        void OpenNeighbours()
        {
            var finished = new HashSet<string>(Regions.Where(p => p.Remaining == 0 && (isCovered == null || isCovered(p))).Select(p => p.Data.name));
            foreach (var region in Regions)
                if (!region.Open && (region.Data.prerequests ?? new string[0]).Any(finished.Contains)) region.Open = true;
        }

        void Evaluate()
        {
            if (isCovered != null && Regions.Any(p => p.Remaining == 0 && !isCovered(p))) return;
            if (Remaining == 0) { State = GameState.Won; return; }
            if (Slots.Any(s => Target(s) >= 0)) return;
            if (Enumerable.Range(0,Slots.Length).All(i => Slots[i] != null || SlotRemaining(i)>0) || Lanes.All(l => l.Count == 0)) State = GameState.Lost;
        }

        public int HintLane()
        {
            for (int i = 0; i < Lanes.Length; i++) if (Lanes[i].Count > 0 && Target(Lanes[i].Peek()) >= 0) return i;
            // Choose the lane whose next useful color is nearest the front.
            int best = -1, distance = int.MaxValue;
            for (int i = 0; i < Lanes.Length; i++)
            {
                int d = 0;
                foreach (var s in Lanes[i]) { if (Target(s) >= 0 && d < distance) { best = i; distance = d; } d++; }
            }
            return best;
        }
    }
}


