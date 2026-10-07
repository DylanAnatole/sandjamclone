using UnityEngine;
namespace SandJamTest
{
    public sealed class LevelProgressManager
    {
        readonly string prefix;
        public LevelProgressManager(string profile){prefix="SandJam.Levels."+profile+".";}
        public bool IsCompleted(int number){return PlayerPrefs.GetInt(prefix+number,0)==1;}
        public void MarkCompleted(int number){if(IsCompleted(number))return;PlayerPrefs.SetInt(prefix+number,1);PlayerPrefs.Save();}
        public void Clear(int number){PlayerPrefs.DeleteKey(prefix+number);PlayerPrefs.Save();}
    }
}
