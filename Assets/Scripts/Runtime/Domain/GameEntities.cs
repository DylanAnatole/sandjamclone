namespace SandJamTest
{
    public enum GameState { Playing, Won, Lost }
    public sealed class Shooter
    {
        public readonly int Color, InitialAmmo;
        public int Ammo;
        public int FreezeRemaining { get; internal set; }
        public bool IsFrozen { get { return FreezeRemaining > 0; } }
        public Shooter Partner { get; internal set; }
        public Shooter(CharacterData data) { Color = data.ColorType; Ammo = InitialAmmo = data.AmmoCount; FreezeRemaining = data.IsFreeze ? data.FreezeCount : 0; }
    }
    public sealed class Region
    {
        public readonly PartData Data;
        public int Remaining;
        public bool Open;
        public bool Revealed;
        public bool InformationVisible { get { return Open || Revealed; } }
        public Region(PartData data) { Data = data; Remaining = data.amount; Open = data.isOpenedAtStart; }
    }
    public struct Shot
    {
        public int Slot, Region, Color, Amount;
    }

}

