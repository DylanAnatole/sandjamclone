using System;
using UnityEngine;
namespace SandJamTest.Scene3D
{
    // One scene; only the selected level prefab is loaded, not every prefab in the catalog.
    public sealed class LevelBootstrap : MonoBehaviour
    {
        public const string SceneName="SandJamGame";
        public static int SelectedIndex;
        public LevelCatalog Catalog;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSelection(){SelectedIndex=0;}
        void Awake()
        {
            Catalog.Validate();SelectedIndex=Mathf.Clamp(SelectedIndex,0,Catalog.Levels.Length-1);
            var entry=Catalog.Get(SelectedIndex);var prefab=Resources.Load<GameObject>(entry.PrefabResource);
            if(!prefab)throw new InvalidOperationException("Missing level prefab: "+entry.PrefabResource);
            Instantiate(prefab,transform);
        }
    }
}
