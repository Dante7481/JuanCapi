using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace JuanCapi
{
    [Serializable] public sealed class KeyBinding { public string action; public KeyCode key; public KeyBinding(string a, KeyCode k) { action = a; key = k; } }
    [Serializable] public sealed class GameSettings
    {
        public float master = .8f, music = .7f, effects = .8f;
        public float uiScale = 1, subtitleScale = 1;
        public bool cameraShake = true, reducedFlashes, fullscreen;
        public int width = 1280, height = 720;
        public bool vsync = true;
        public List<KeyBinding> bindings = Defaults();
        public static List<KeyBinding> Defaults() => new List<KeyBinding>
        {
            new KeyBinding("Izquierda", KeyCode.A), new KeyBinding("Derecha", KeyCode.D),
            new KeyBinding("Saltar", KeyCode.Space), new KeyBinding("Atacar", KeyCode.Mouse0),
            new KeyBinding("Dash", KeyCode.LeftShift), new KeyBinding("Escudo", KeyCode.Mouse1),
            new KeyBinding("Interactuar", KeyCode.E), new KeyBinding("Consumible", KeyCode.F),
            new KeyBinding("Cambiar arma", KeyCode.Q), new KeyBinding("Nadar arriba", KeyCode.W),
            new KeyBinding("Nadar abajo", KeyCode.S)
        };
        public KeyCode Key(string action) => bindings.Find(b => b.action == action)?.key ?? KeyCode.None;
        public bool Rebind(string action, KeyCode key)
        {
            if (key == KeyCode.None || key == KeyCode.Escape || key == KeyCode.Tab || key == KeyCode.Return || !Enum.IsDefined(typeof(KeyCode), key)) return false;
            var binding = bindings.Find(b => b.action == action); if (binding == null) return false;
            var duplicate = bindings.Find(b => b.key == key && b != binding);
            if (duplicate != null) duplicate.key = binding.key;
            binding.key = key; return true;
        }
        public void Normalize()
        {
            master = float.IsNaN(master) ? .8f : Mathf.Clamp01(master);
            music = float.IsNaN(music) ? .7f : Mathf.Clamp01(music);
            effects = float.IsNaN(effects) ? .8f : Mathf.Clamp01(effects);
            uiScale = uiScale >= 1.125f ? 1.25f : 1;
            subtitleScale = subtitleScale >= 1.125f ? 1.25f : 1;
            if (width < 800 || width > 7680 || height < 600 || height > 4320) { width = 1280; height = 720; }
            var defaults = Defaults();
            if (bindings == null || bindings.Count != defaults.Count) { bindings = defaults; return; }
            var keys = new HashSet<KeyCode>(); var actions = new HashSet<string>();
            foreach (var b in bindings)
                if (b == null || !defaults.Exists(d => d.action == b.action) || !actions.Add(b.action) || !keys.Add(b.key) ||
                    !Enum.IsDefined(typeof(KeyCode), b.key) || b.key == KeyCode.None || b.key == KeyCode.Escape || b.key == KeyCode.Tab || b.key == KeyCode.Return)
                { bindings = defaults; return; }
        }
        public static GameSettings Load(string folder)
        {
            try { var result = JsonUtility.FromJson<GameSettings>(File.ReadAllText(Path.Combine(folder, "settings.json"))); result.Normalize(); return result; }
            catch { return new GameSettings(); }
        }
        public bool Save(string folder, out string error)
        {
            try
            {
                Normalize(); Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, "settings.json");
                File.WriteAllText(path + ".tmp", JsonUtility.ToJson(this, true));
                if (File.Exists(path)) File.Replace(path + ".tmp", path, path + ".bak"); else File.Move(path + ".tmp", path);
                error = null; return true;
            }
            catch (Exception e) { error = "No se guardaron los ajustes: " + e.Message; return false; }
        }
        public void Apply(bool display = false)
        {
            AudioListener.volume = master; QualitySettings.vSyncCount = vsync ? 1 : 0;
            Application.targetFrameRate = vsync ? -1 : 60;
            if (display && !Application.isBatchMode) Screen.SetResolution(width, height, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }
    }
    public static class GameInput
    {
        public static bool Down(string action) => Input.GetKeyDown(Key(action));
        public static bool Held(string action) => Input.GetKey(Key(action));
        public static KeyCode Key(string action) => GameRuntime.Instance?.Settings.Key(action) ?? KeyCode.None;
        public static string Label(string action)
        {
            var key = Key(action);
            if (key == KeyCode.Mouse0) return "Clic izq.";
            if (key == KeyCode.Mouse1) return "Clic der.";
            if (key == KeyCode.Space) return "Espacio";
            if (key == KeyCode.LeftShift) return "Shift";
            return key.ToString();
        }
    }
}
