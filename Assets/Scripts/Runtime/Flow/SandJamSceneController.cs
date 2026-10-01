using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    public sealed partial class SandJamSceneController : MonoBehaviour
    {
        [Header("Level and scene references")]
        public TextAsset LevelJson;
        public bool HidePrototypeHud;
        public bool EnableSlotUnlocks;
        public float ViewAspect = .6f;
        public Camera GameCamera;
        public SceneRegionView[] Regions;
        public SceneActorView[] Characters;
        public Transform[] LaneStarts;
        public Transform[] StashSlots;
        public Transform ProjectileRoot;
        public GameObject ProjectilePrefab;
        public Material[] ProjectileMaterials;
        public int[] MaterialColorIds;
        public Font InterfaceFont;
        [Header("Gameplay tuning")]
        public float QueueSpacing = 1.20f;
        public float ShotInterval = .08f;
        public int AmmoPerShot = 20;
        [Header("Original sounds")]
        public AudioClip SelectSound;
        public AudioClip CompleteSound;
        public AudioClip VictorySound;
        public Texture2D PlayButtonTexture;
        public SandGame Game { get; private set; }
        readonly Dictionary<Shooter, SceneActorView> actors = new Dictionary<Shooter, SceneActorView>();
        readonly List<Bolt> bolts = new List<Bolt>();
        readonly Queue<GameObject> boltPool = new Queue<GameObject>();
        SandBoardTextureView boardTexture;
        LevelData level;
        AudioSource sound;
        GUIStyle textStyle;
        float accumulator, hintTime, messageTime;
        int hintLane = -1;
        bool paused, fast, muted, smokeMode;
        int lastScreenWidth, lastScreenHeight;
        string error, message = "Chọn nhân vật đỏ ở đầu hàng để bắt đầu.", smokeOutput;
        static readonly Color Ink = new Color(.13f,.23f,.30f), Accent = new Color(.07f,.47f,.44f);
        sealed class Bolt { public GameObject Object; public Vector3 From, To; public float Time; }

        public void ConfigureFiveSlots()
        {
            if (StashSlots.Length < 5) throw new InvalidOperationException("Five stash anchors are required.");
            var parent = StashSlots[0].parent;
            foreach (Transform child in parent.Cast<Transform>().ToArray())
            {
                if (child.name == "Slot 6 platform" || child.name == "Slot 6 anchor" || child.name == "Slot index 6") { child.gameObject.SetActive(false); if(Application.isPlaying) Destroy(child.gameObject); else DestroyImmediate(child.gameObject); }
                for (int i=0;i<5;i++)
                    if(child.name=="Slot "+(i+1)+" platform" || child.name=="Slot "+(i+1)+" anchor" || child.name=="Slot index "+(i+1))
                    { var position=child.position; position.x=-3f+i*1.5f; child.position=position; }
            }
            StashSlots=StashSlots.Take(5).ToArray();
            parent.name="Stash - 5 positions";
        }

        void Awake()
        {
            ConfigureFiveSlots();
            Application.targetFrameRate = 60;
            FitCamera();
            sound = GetComponent<AudioSource>();
            var args = Environment.GetCommandLineArgs();
            int flag = Array.IndexOf(args, "--scene-smoke-output");
            if (flag >= 0 && flag + 1 < args.Length) { smokeMode = true; smokeOutput = args[flag + 1]; }
            try
            {
                level = JsonUtility.FromJson<LevelData>(LevelJson.text);
                if (Regions.Length != level.parts.Length || Characters.Length != level.laneData.Sum(l => l.ColorAmmoDatas.Length) || StashSlots.Length != 5)
                    throw new InvalidOperationException("Scene references do not match the tutorial.");
                var boardObject=new GameObject("Sand Board - Grid Texture SpriteRenderer");
                boardTexture=boardObject.AddComponent<SandBoardTextureView>();
                boardTexture.Initialize(level,Regions[0].Geometry.transform.parent.parent);
                foreach(var region in Regions) region.AttachBoard(boardTexture);
                var obstacles=Regions[0].Geometry.transform.parent.parent.Find("Obstacle tiles");
                if(obstacles) obstacles.GetComponent<MeshRenderer>().enabled=false;
                Restart();
            }
            catch (Exception ex) { error = ex.Message; Debug.LogException(ex); }
        }

        void Start()
        {
            if (error == null) Synchronize(0);
            if (smokeMode) StartCoroutine(GuardSmoke(SmokeTest()));
        }

        public void Restart()
        {
            Game = new SandGame(level, StashSlots.Length, r => Regions[Array.IndexOf(level.parts, r.Data)].SettledCount == r.Data.rows.Length, EnableSlotUnlocks);
            actors.Clear(); foreach (var region in Regions) { region.Divider = level.uiDivider; region.ResetFlow(); }
            for (int lane = 0; lane < Game.Lanes.Length; lane++)
            {
                var queue = Game.Lanes[lane].ToArray();
                for (int i = 0; i < queue.Length; i++)
                {
                    var actor = Characters.Single(a => a.SourceLane == lane && a.SourceOrder == i);
                    actor.Divider = level.uiDivider; actor.Bind(queue[i], LaneStarts[lane].position + Vector3.down * QueueSpacing * i);
                    actors.Add(queue[i], actor);
                }
            }
            foreach (var bolt in bolts) ReturnBolt(bolt.Object);
            bolts.Clear();
            accumulator = 0; paused = false; hintLane = -1;
            sound.Stop();
            message = "Chọn nhân vật đỏ ở đầu hàng để bắt đầu.";
            messageTime = Time.unscaledTime + 6;
        }

        void Update()
        {
            FitCamera();
            if (error != null || Game == null || smokeMode) return;
            if (Input.GetKeyDown(KeyCode.R)) Restart();
            if (Input.GetKeyDown(KeyCode.Space)) paused = !paused;
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectLane(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SelectLane(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SelectLane(2);
            if (Input.GetMouseButtonDown(0) && !paused && Game.State == GameState.Playing)
            {
                RaycastHit hit;
                if (Physics.Raycast(GameCamera.ScreenPointToRay(Input.mousePosition), out hit, 100, 1 << 8))
                    SelectActor(hit.collider.GetComponentInParent<SceneActorView>());
            }
            if (!paused) Advance(Mathf.Min(Time.unscaledDeltaTime, .1f) * (fast ? 2 : 1));
        }

        void FitCamera()
        {
            if (lastScreenWidth == Screen.width && lastScreenHeight == Screen.height) return;
            lastScreenWidth = Screen.width; lastScreenHeight = Screen.height;
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            if (aspect > ViewAspect) { float width = ViewAspect / aspect; GameCamera.rect = new Rect((1-width)/2,0,width,1); }
            else { float height = aspect / ViewAspect; GameCamera.rect = new Rect(0,(1-height)/2,1,height); }
            GameCamera.aspect = ViewAspect;
        }

        public bool SelectActor(SceneActorView actor)
        {
            if (actor == null || actor.Shooter == null || paused || Game.State != GameState.Playing) return false;
            var lane = Game.Lanes[actor.SourceLane];
            if (lane.Count == 0 || lane.Peek() != actor.Shooter)
            {
                Notify("Chỉ chọn nhân vật đứng đầu mỗi hàng."); return false;
            }
            return SelectLane(actor.SourceLane);
        }

        public bool SelectLane(int lane)
        {
            if (paused || Game.State != GameState.Playing) return false;
            if (!Game.SelectLane(lane)) { Notify("Ô chờ đã đầy. Đợi nhân vật dùng hết đạn."); return false; }
            Play(SelectSound, .25f);
            hintLane = -1;
            Synchronize(0);
            Notify("Nhân vật sẽ tự bắn khi đến ô chờ và có màu đang mở.");
            return true;
        }

        void Notify(string text) { message = text; messageTime = Time.unscaledTime + 3.5f; }
        void Play(AudioClip clip, float volume) { if (!muted && !smokeMode && clip != null) sound.PlayOneShot(clip, volume); }
        bool CanShoot(int slot) { var shooter = Game.Slots[slot]; return shooter != null && actors[shooter].AtRest; }

        public void Advance(float delta)
        {
            Synchronize(delta);
            if (Game.State == GameState.Playing)
            {
                accumulator += delta;
                while (accumulator >= ShotInterval)
                {
                    accumulator -= ShotInterval;
                    var before = Game.Slots.Select(s => s == null ? null : actors[s]).ToArray();
                    int completed = Game.Regions.Count(r => r.Remaining == 0);
                    var previousState = Game.State;
                    foreach (var shot in Game.Tick(AmmoPerShot, CanShoot))
                        SpawnBolt(before[shot.Slot].AimPoint, Regions[shot.Region].Target.position + Vector3.back * .15f, shot.Color);
                    if (previousState != GameState.Won && Game.State == GameState.Won) Play(VictorySound, .45f);
                    else if (completed < Game.Regions.Count(r => r.Remaining == 0)) Play(CompleteSound, .3f);
                }
            }
            Synchronize(0);
            foreach (var region in Regions) region.Advance(delta);
            for (int i = bolts.Count - 1; i >= 0; i--)
            {
                var bolt = bolts[i]; bolt.Time += delta;
                float t = Mathf.Clamp01(bolt.Time / .24f);
                bolt.Object.transform.position = Vector3.Lerp(bolt.From, bolt.To, t) + Vector3.back * Mathf.Sin(t * Mathf.PI) * .8f;
                if (t >= 1) { ReturnBolt(bolt.Object); bolts.RemoveAt(i); }
            }
        }

        void Synchronize(float delta)
        {
            for (int lane = 0; lane < Game.Lanes.Length; lane++)
            {
                int order = 0;
                foreach (var shooter in Game.Lanes[lane]) actors[shooter].MoveTo(LaneStarts[lane].position + Vector3.down * QueueSpacing * order++);
            }
            for (int slot = 0; slot < Game.Slots.Length; slot++)
                if (Game.Slots[slot] != null) actors[Game.Slots[slot]].MoveTo(StashSlots[slot].position);
            foreach (var pair in actors)
            {
                if (pair.Key.Ammo == 0) pair.Value.Leave();
                pair.Value.Advance(delta, Game.Slots.Contains(pair.Key) && Game.Target(pair.Key) >= 0);
            }
            foreach (var view in Regions) view.Apply(Game.Regions[view.PartIndex]);
        }

        void SpawnBolt(Vector3 from, Vector3 to, int color)
        {
            var obj = boltPool.Count > 0 ? boltPool.Dequeue() : Instantiate(ProjectilePrefab, ProjectileRoot);
            obj.GetComponent<Renderer>().sharedMaterial = ProjectileMaterials[Array.IndexOf(MaterialColorIds, color)];
            obj.transform.position = from; obj.SetActive(true);
            bolts.Add(new Bolt { Object = obj, From = from, To = to });
        }
        void ReturnBolt(GameObject obj) { obj.SetActive(false); boltPool.Enqueue(obj); }

    }
}


