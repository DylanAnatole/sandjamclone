namespace SandJamTest
{
    public enum GameState { Playing, Won, Lost }
    public sealed class Shooter
    {
        public readonly int Color, InitialAmmo;
        public int Ammo;
        public Shooter Partner { get; internal set; }
        public Shooter(CharacterData data) { Color = data.ColorType; Ammo = InitialAmmo = data.AmmoCount; }
    }
    public sealed class Region
    {
        public readonly PartData Data;
        public int Remaining;
        public bool Open;
        public Region(PartData data) { Data = data; Remaining = data.amount; Open = data.isOpenedAtStart; }
    }
    public struct Shot
    {
        public int Slot, Region, Color, Amount;
    }

}

