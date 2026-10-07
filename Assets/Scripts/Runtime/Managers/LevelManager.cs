using System;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace SandJamTest.Scene3D
{
    public sealed class LevelManager : MonoBehaviour
    {
        public LevelCatalog Catalog;
        public int Index;
        public SandJamSceneController Controller;
        public VideoScreen Screen;
        public LevelProgressManager Progress {get;private set;}
        bool loading;
        public LevelEntry Current {get{return Catalog.Get(Index);}}
        void Awake()
        {
            Catalog.Validate();Catalog.Get(Index);
            bool test=Array.IndexOf(Environment.GetCommandLineArgs(),"--levelpack-smoke")>=0;
            Progress=new LevelProgressManager((test?"smoke.":"")+Catalog.ProgressId);
        }
        public bool RecordCompletion()
        {
            if(Controller.Game==null || Controller.Game.State!=GameState.Won)return false;
            Progress.MarkCompleted(Current.Number);return true;
        }
        public bool LoadNext()
        {
            if(!RecordCompletion())return false;
            if(Index+1>=Catalog.Levels.Length){Screen.Show(VideoScreen.Page.Home);return false;}
            return Select(Index+1);
        }
        public bool Select(int index)
        {
            if(loading || index<0 || index>=Catalog.Levels.Length)return false;
            var entry=Catalog.Get(index);
            string scene=string.IsNullOrEmpty(entry.PrefabResource)?entry.SceneName:LevelBootstrap.SceneName;
            if(!Application.CanStreamedLevelBeLoaded(scene)){Debug.LogError("Level scene is missing from Build Settings: "+scene);return false;}
            loading=true;LevelBootstrap.SelectedIndex=index;SceneManager.LoadScene(scene);return true;
        }
    }
}
