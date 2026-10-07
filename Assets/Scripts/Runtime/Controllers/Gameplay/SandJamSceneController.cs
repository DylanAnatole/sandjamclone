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
        public bool BoosterInputBlocked { get; set; }
        void OnDestroy(){if(projectiles!=null)projectiles.Dispose();}
        int suppressedInputFrame = -1;
        public void SuppressInputThisFrame() { suppressedInputFrame = Time.frameCount; }
        public bool SwapFrontRows()
        {
            if (paused || Game == null || !Game.SwapFrontRows()) return false;
            Synchronize(0); Play(SelectSound, .25f); return true;
        }
        public bool SelectPriority(Shooter shooter)
        {
            if (paused || Game == null || !Game.SelectPriority(shooter)) return false;
            Synchronize(0); Play(SelectSound, .25f); return true;
        }
        public string SelectionFeedback { get { return Time.unscaledTime < selectionFeedbackUntil ? selectionFeedback : ""; } }
        string selectionFeedback;
        float selectionFeedbackUntil;
        CharacterQueueController characterQueue;
        ProjectileManager projectiles;
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
                level = LevelDataManager.Load(LevelJson);
                if (Regions.Length != level.parts.Length || Characters.Length != level.laneData.Sum(l => l.ColorAmmoDatas.Length) || StashSlots.Length != 5)
                    throw new InvalidOperationException("Scene references do not match the tutorial.");
                var boardObject=new GameObject("Sand Board - Grid Texture SpriteRenderer");
                boardTexture=boardObject.AddComponent<SandBoardTextureView>();
                boardTexture.Initialize(level,Regions[0].Geometry.transform.parent.parent);
                foreach(var region in Regions) region.AttachBoard(boardTexture);
                var obstacles=Regions[0].Geometry.transform.parent.parent.Find("Obstacle tiles");
                if(obstacles) obstacles.GetComponent<MeshRenderer>().enabled=false;
                projectiles = new ProjectileManager(ProjectileRoot, ProjectilePrefab, ProjectileMaterials, MaterialColorIds);
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
            foreach (var region in Regions) { region.Divider = level.uiDivider; region.ResetFlow(); }
            characterQueue = new CharacterQueueController(Characters, LaneStarts, StashSlots, QueueSpacing);
            characterQueue.Bind(Game, level);
            projectiles.Reset();
            accumulator = 0; paused = false; hintLane = -1;
            selectionFeedback = ""; selectionFeedbackUntil = 0;
            sound.Stop();
            message = "Chọn nhân vật đỏ ở đầu hàng để bắt đầu.";
            messageTime = Time.unscaledTime + 6;
        }

        void Update()
        {
            FitCamera();
            if (error != null || Game == null || smokeMode || BoosterInputBlocked || suppressedInputFrame == Time.frameCount) return;
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
            if (actor.Shooter.Partner != null && lane.Count > 0 && lane.Peek() == actor.Shooter.Partner)
                return SelectLane(actor.SourceLane);
            if (lane.Count == 0 || lane.Peek() != actor.Shooter)
            {
                Notify("Chỉ chọn nhân vật đứng đầu mỗi hàng."); return false;
            }
            return SelectLane(actor.SourceLane);
        }

        public bool SelectLane(int lane)
        {
            if (paused || Game.State != GameState.Playing) return false;
            if (!Game.SelectLane(lane))
            {
                var first = lane >= 0 && lane < Game.Lanes.Length && Game.Lanes[lane].Count > 0 ? Game.Lanes[lane].Peek() : null;
                var frozen = first != null && first.IsFrozen ? first : first != null && first.Partner != null && first.Partner.IsFrozen ? first.Partner : null;
                bool linked = lane >= 0 && lane < Game.Lanes.Length && Game.Lanes[lane].Count > 0 && Game.Lanes[lane].Peek().Partner != null;
                selectionFeedback = frozen != null ? "Còn " + frozen.FreezeRemaining + " lớp băng · Đưa hộp khác lên trước" : linked ? "Chưa đủ chỗ hoặc cặp chưa ra đầu hàng" : "Ô chờ đã đầy";
                selectionFeedbackUntil = Time.unscaledTime + 3;
                Notify(selectionFeedback); return false;
            }
            selectionFeedback = "";
            Play(SelectSound, .25f);
            hintLane = -1;
            Synchronize(0);
            Notify("Nhân vật sẽ tự bắn khi đến ô chờ và có màu đang mở.");
            return true;
        }

        void Notify(string text) { message = text; messageTime = Time.unscaledTime + 3.5f; }
        void Play(AudioClip clip, float volume) { if (!muted && !smokeMode && clip != null) sound.PlayOneShot(clip, volume); }
        bool CanShoot(int slot) { return characterQueue.CanShoot(Game, slot); }

        public void Advance(float delta)
        {
            Synchronize(delta);
            if (Game.State == GameState.Playing)
            {
                accumulator += delta;
                while (accumulator >= ShotInterval)
                {
                    accumulator -= ShotInterval;
                    int completed = Game.Regions.Count(r => r.Remaining == 0);
                    var previousState = Game.State;
                    foreach (var shot in Game.Tick(AmmoPerShot, CanShoot))
                        projectiles.Spawn(Regions[shot.Region], shot.Color, shot.Slot);
                    if (previousState != GameState.Won && Game.State == GameState.Won) Play(VictorySound, .45f);
                    else if (completed < Game.Regions.Count(r => r.Remaining == 0)) Play(CompleteSound, .3f);
                }
            }
            Synchronize(0);
            foreach (var region in Regions) region.BeginAdvance(delta);
            Unity.Jobs.JobHandle.ScheduleBatchedJobs();
            foreach (var region in Regions) region.FinishAdvance();
            projectiles.Advance(delta);
        }

        void Synchronize(float delta)
        {
            characterQueue.Synchronize(Game, delta);
            foreach (var view in Regions) view.Apply(Game.Regions[view.PartIndex]);
        }


    }
}


