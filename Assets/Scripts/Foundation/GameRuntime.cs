using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JuanCapi
{
    [DefaultExecutionOrder(-900)]
    public sealed class GameRuntime : MonoBehaviour
    {
        public static GameRuntime Instance { get; private set; }
        public CampaignStore Store { get; private set; }
        public CampaignData Data { get; private set; }
        public GameSettings Settings { get; private set; }
        public GameInterface UI { get; private set; }
        public string DataDirectory { get; private set; }
        public PlayerController Player { get; private set; }
        public bool IsPractice { get; private set; }
        public bool Busy { get; private set; }
        public bool CanPlay => Data != null && Player != null && !Busy && UI != null && !UI.IsOpen && Data.healthQuarters > 0;
        public event Action Changed;
        private bool deathHandled;
        private float autoClock;
        private int ignoreInputFrame;
        public bool InputAllowed => CanPlay && Time.frameCount > ignoreInputFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            new GameObject("Juan Capi • Sistemas").AddComponent<GameRuntime>();
        }
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            DataDirectory = Path.Combine(Application.persistentDataPath, "CampaignV1");
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-juanDataPath");
            if (index >= 0 && index + 1 < args.Length) DataDirectory = Path.GetFullPath(args[index + 1]);
            Store = new CampaignStore(DataDirectory); Settings = GameSettings.Load(DataDirectory); Settings.Apply(true);
            UI = gameObject.AddComponent<GameInterface>();
            SceneManager.sceneLoaded += SceneLoaded;
        }
        private void OnDestroy() { SceneManager.sceneLoaded -= SceneLoaded; if (Instance == this) Instance = null; Time.timeScale = 1; }
        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive) return;
            UI.EnsureInput();
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (canvas.gameObject.scene == scene) canvas.gameObject.SetActive(false);
            foreach (var old in FindObjectsByType<PauseMenu>(FindObjectsSortMode.None)) old.enabled = false;
            foreach (var old in FindObjectsByType<GameManager>(FindObjectsSortMode.None)) old.enabled = false;
            foreach (var old in FindObjectsByType<HealthManager>(FindObjectsSortMode.None)) old.enabled = false;
            Player = FindFirstObjectByType<PlayerController>(); deathHandled = false; autoClock = 0;
            if (scene.name == "Menu") { Data = null; UI.MainMenu(); return; }
            if (FindFirstObjectByType<ArtPreviewScene>() != null) { Data = null; IsPractice = true; UI.ArtPreview(); return; }
            if (Player == null) { UI.Message("No se encontró al jugador en esta escena."); return; }
            if (Data == null)
            {
                // Direct editor scene entry never writes into a real player's campaign.
                Data = new CampaignData { sword = true, introSeen = true }; IsPractice = true;
            }
            Data.scene = scene.name;
            if(scene.name=="LV1")Data.region="Quebrada de la sierra";
            if (Data.hasPosition) Player.transform.position = new Vector3(Data.x, Data.y, Data.z);
            else CaptureSafePosition(Player.transform.position);
            Player.ApplyCampaign();
            var art=CampaignArt.Current;
            if(art!=null)
            {
                (Player.GetComponent<PlayerArtDriver>()??Player.gameObject.AddComponent<PlayerArtDriver>()).Install(art);
                foreach(var enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
                    (enemy.GetComponent<EnemyArtDriver>()??enemy.gameObject.AddComponent<EnemyArtDriver>()).Install(art);
            }
            var cameraFollow = FindFirstObjectByType<CameraFollow>();
            if (cameraFollow != null) cameraFollow.transform.position = Player.transform.position + cameraFollow.desplazamiento;
            InstallSceneInteractions(); UI.Play(); NotifyChanged();
        }
        public void NewCampaign(bool confirmed)
        {
            if (Busy) return;
            var initial = new CampaignData();
            if (!Store.StartNew(initial, confirmed, out string error)) { UI.Message(error); return; }
            IsPractice = false; Data = initial; LoadScene("LV1");
        }
        public void LoadRecord(SaveRecord record)
        {
            if (Busy || record == null) return;
            if (!Application.CanStreamedLevelBeLoaded(record.data.scene)) { UI.Message("Esta escena todavía no está instalada: " + record.data.scene); return; }
            Data = record.data.Clone(); IsPractice = false;
            if (Data.healthQuarters == 0) Data.healthQuarters = Math.Min(12, Data.MaximumHealth);
            LoadScene(Data.scene);
        }
        private void LoadScene(string scene) { StartCoroutine(ChangeScene(scene)); }
        private IEnumerator ChangeScene(string scene)
        {
            Busy = true; Time.timeScale = 0; UI.Loading();
            var operation = SceneManager.LoadSceneAsync(scene);
            if (operation == null) { Busy = false; UI.Message("No se pudo cargar la escena."); yield break; }
            while (!operation.isDone) yield return null;
            Busy = false; ignoreInputFrame = Time.frameCount + 1; Time.timeScale = UI.IsOpen ? 0 : 1;
        }
        public void ToMenu() { if (!Busy) LoadScene("Menu"); }
        private void Update()
        {
            if (Data == null || Player == null || Busy) return;
            if (Data.healthQuarters <= 0 && !deathHandled)
            {
                deathHandled = true; Data.deaths++; UI.Death(); return;
            }
            if (!CanPlay) return;
            WorldInteraction.Tick(this);
            Data.activeSeconds += Time.unscaledDeltaTime; autoClock += Time.unscaledDeltaTime;
            if (GameInput.Down("Consumible") && Data.Use(Data.assignedConsumable)) NotifyChanged();
            if (GameInput.Down("Cambiar arma") && Data.gun) { Data.equippedWeapon = Data.equippedWeapon == "sword" ? "gun" : "sword"; NotifyChanged(); }
            if (autoClock >= 120 && Player.EnSuelo && !Player.InDanger) { autoClock = 0; Save(false, "Avance por " + Data.region); }
        }
        private void OnApplicationFocus(bool focus) { if (!focus && CanPlay) UI.Pause(); }
        public void CaptureSafePosition(Vector3 position)
        {
            if (Data == null) return;
            Data.x = position.x; Data.y = position.y; Data.z = position.z; Data.hasPosition = true;
        }
        public void NotifyChanged() { if (Player != null) Player.ApplyCampaign(); Changed?.Invoke(); }
        public void ResumeInput() { ignoreInputFrame = Time.frameCount + 1; }
        public bool Save(bool manual, string label)
        {
            if (Busy || Data == null || Data.healthQuarters <= 0) return false;
            if (IsPractice) { UI.Toast("Sesión de prueba: no modifica la campaña."); return false; }
            if (Store.Save(Data, manual, label, CaptureThumbnail(), out string error))
            { UI.Toast(manual ? "Partida guardada" : "Autoguardado completado"); return true; }
            UI.Message(error); return false;
        }
        public bool Transaction(Func<CampaignData, bool> mutation, string label)
        {
            if (Data == null || Busy) return false;
            var next = Data.Clone();
            if (!mutation(next)) return false;
            if (!IsPractice && !Store.Save(next, false, label, CaptureThumbnail(), out string error)) { UI.Message(error); return false; }
            Data = next; NotifyChanged(); UI.Toast(label); return true;
        }
        public bool GrantReward(CampaignReward reward) => Transaction(d => d.Grant(reward), "Equipo obtenido: " + reward);
        public void Retry()
        {
            if (IsPractice) { Data.healthQuarters = 12; LoadScene(Data.scene); return; }
            var record = Store.Records().FirstOrDefault(r => r.data.campaignId == Data.campaignId);
            if (record == null) { UI.Message("No hay un guardado disponible."); return; }
            var recovered = record.data.Clone();
            recovered.activeSeconds = Math.Max(recovered.activeSeconds, Data.activeSeconds);
            recovered.deaths = Math.Max(recovered.deaths, Data.deaths);
            recovered.healthQuarters = Math.Min(12, recovered.MaximumHealth);
            if (!Store.Save(recovered, false, "Reintento", null, out string error)) { UI.Message(error); return; }
            Data = recovered; LoadScene(Data.scene);
        }
        public string CaptureThumbnail()
        {
            if (Camera.main == null || Application.isBatchMode) return null;
            // The camera image excludes the overlay menu, even when saving from pause.
            var rt = RenderTexture.GetTemporary(320, 180, 16);
            var previous = RenderTexture.active; var camera = Camera.main; var target = camera.targetTexture;
            Texture2D image = null;
            try
            {
                camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                image = new Texture2D(320, 180, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 320, 180), 0, 0); image.Apply();
                return Convert.ToBase64String(image.EncodeToJPG(65));
            }
            catch (Exception) { return null; }
            finally { camera.targetTexture = target; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); if (image != null) Destroy(image); }
        }
        private void InstallSceneInteractions()
        {
            if (Data.scene != "LV1") return;
            // The current level remains playable while its final art and encounters are produced in later phases.
            var spawn = new Vector3(-28.2f, -9.45f, -9);
            WorldInteraction.Create("CAPSULE", "Espada de plasma", "sword", spawn, new Color(.15f, .95f, .9f));
            WorldInteraction.Create("MARA", "Refugio de Mara", "shelter", spawn + Vector3.right * 3.1f, new Color(.9f, .72f, .32f));
            WorldInteraction.Create("B01_SUPPLIES", "Provisiones", "supplies", spawn + Vector3.right * 1.5f, new Color(.5f, .85f, .4f));
            var coins=FindObjectsByType<Moneda>(FindObjectsSortMode.None).OrderBy(c=>c.transform.position.x).Take(4).ToArray();
            foreach(var coin in coins)
            {
                coin.enabled=false;
                var pickup=coin.gameObject.AddComponent<CampaignPickup>();pickup.item="piece";
                pickup.persistentId="HEART_PIECE:"+WorldInteraction.StableId(coin.gameObject);
                var renderer=coin.GetComponent<SpriteRenderer>();if(renderer!=null)renderer.enabled=false;
                var icon=new GameObject("Pieza de corazón",typeof(RectTransform),typeof(Canvas));icon.transform.SetParent(coin.transform,false);
                var parentScale=coin.transform.lossyScale;
                icon.transform.localScale=new Vector3(.012f/Mathf.Max(.001f,Mathf.Abs(parentScale.x)),.012f/Mathf.Max(.001f,Mathf.Abs(parentScale.y)),1);
                icon.GetComponent<RectTransform>().sizeDelta=new Vector2(60,55);
                var canvas=icon.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.sortingOrder=10;
                var graphic=icon.AddComponent<HeartGraphic>();graphic.color=new Color(.96f,.3f,.5f);graphic.quarters=1;graphic.raycastTarget=false;
            }
        }
    }
}
