using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using UnityEditor.Build.Reporting;

namespace JuanCapi.Editor
{
    // The source sheets are not grids. Adjacent tails and weapon trails have
    // overlapping bounding rectangles, so a rectangular sprite cannot isolate them.
    public static class PreciseSpriteGeometry
    {
        static string Output => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Entregas/RecortesPrecisos"));
        sealed class Island
        {
            public readonly List<int> pixels = new List<int>();
            public int left = int.MaxValue, bottom = int.MaxValue, right, top;
            public Rect Rect => new Rect(left, bottom, right - left, top - bottom);
        }
        sealed class Sheet
        {
            public readonly Texture2D texture;
            public readonly Color32[] pixels;
            public readonly int[] core, owner;
            public readonly List<Island> islands = new List<Island> { null };
            public int Width => texture.width;
            public int Height => texture.height;

            public Sheet(Texture2D texture)
            {
                this.texture = texture;
                pixels = texture.GetPixels32();
                core = new int[pixels.Length];
                var queue = new Queue<int>();
                for (int start = 0; start < pixels.Length; start++)
                {
                    if (core[start] != 0 || pixels[start].a <= 20) continue;
                    var island = new Island(); int id = islands.Count;
                    islands.Add(island); core[start] = id; queue.Enqueue(start);
                    while (queue.Count > 0)
                    {
                        int p = queue.Dequeue(), x = p % Width, y = p / Width;
                        island.pixels.Add(p);
                        island.left = Math.Min(island.left, x); island.right = Math.Max(island.right, x + 1);
                        island.bottom = Math.Min(island.bottom, y); island.top = Math.Max(island.top, y + 1);
                        if (x > 0) Visit(p - 1); if (x + 1 < Width) Visit(p + 1);
                        if (y > 0) Visit(p - Width); if (y + 1 < Height) Visit(p + Width);
                    }
                    void Visit(int p)
                    {
                        if (core[p] != 0 || pixels[p].a <= 20 || Band(p) != Band(start)) return;
                        core[p] = id; queue.Enqueue(p);
                    }
                }
                owner = new int[pixels.Length];
                // Seed every pose, including unused poses. Flood only unassigned
                // space; the opaque outline of a neighbouring pose is never cut.
                for (int id = 1; id < islands.Count; id++)
                    if (islands[id].pixels.Count >= 1500)
                        foreach (int p in islands[id].pixels) { owner[p] = id; queue.Enqueue(p); }
                if (texture.name == "CapybaraTerrestre")
                {
                    // Detached tip of the following (unused) overhead swing.
                    // It is closer to the preceding trail than to its own body.
                    int overhead = core[(Height - 1 - 430) * Width + 770];
                    if (overhead == 0) throw new InvalidOperationException("Missing overhead sword pose");
                    foreach (var part in islands.Skip(1))
                        if (part.pixels.Count < 1500 && part.Rect.center.x > 690 && part.Rect.center.x < 735 && Height - part.Rect.center.y > 360 && Height - part.Rect.center.y < 410)
                            foreach (int p in part.pixels) { owner[p] = overhead; queue.Enqueue(p); }
                }
                while (queue.Count > 0)
                {
                    int p = queue.Dequeue(), x = p % Width, y = p / Width;
                    if (x > 0) Grow(p - 1); if (x + 1 < Width) Grow(p + 1);
                    if (y > 0) Grow(p - Width); if (y + 1 < Height) Grow(p + Width);
                    void Grow(int next)
                    {
                        if (owner[next] != 0 || Band(next) != Band(p)) return;
                        owner[next] = owner[p]; queue.Enqueue(next);
                    }
                }
            }

            int Band(int p)
            {
                int topY = Height - 1 - p / Width, x = p % Width;
                if (texture.name == "CapybaraTerrestre")
                    return topY < (x < 320 ? 380 : 350) ? 0 : topY < 508 ? 1 : 2;
                if (texture.name == "Caiman_Aparicion")
                {
                    if (topY >= 345) return 2;
                    float seam = Mathf.Lerp(1214, 1148, Mathf.InverseLerp(235, 285, topY));
                    return x < seam ? 0 : 1;
                }
                if (texture.name != "Caiman_Derrotado_Escape") return 0;
                // Dust bridges the run row and the jump row in the source atlas.
                int band = 0;
                foreach (int seam in new[] { 192, 394, 602, 770, 914 }) if (topY >= seam) band++;
                return band;
            }

            public int Pose(Sprite sprite)
            {
                if (sprite.name == "CaimanTerrestre_Appear5") return core[(Height - 1 - 200) * Width + 1320];
                var counts = new int[islands.Count]; var r = sprite.rect;
                for (int y = (int)r.yMin; y < r.yMax; y++)
                    for (int x = (int)r.xMin; x < r.xMax; x++)
                    {
                        int id = core[y * Width + x];
                        if (id > 0 && islands[id].pixels.Count >= 1500) counts[id]++;
                    }
                return Array.IndexOf(counts, counts.Max());
            }
        }

        static bool Target(string path) =>
            path.StartsWith("Assets/Animations/Amazonia/CapybaraTerrestre/Melee_") ||
            (path.StartsWith("Assets/Animations/V2/CaimanTerrestre_") &&
             (!path.Contains("_Appear") || path.EndsWith("_Appear5.asset")));

        [MenuItem("Juan Capi/Arte/Separar siluetas de espada y caimán")]
        public static void Bake()
        {
            Directory.CreateDirectory(Output);
            var paths = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Animations" })
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith(".asset") && Target(p)).OrderBy(p => p).ToArray();
            var footprints = AssetDatabase.LoadAssetAtPath<SpriteFootprints>("Assets/Resources/Campaign/Footprints.asset");
            var notes = new List<string>();
            var cache = new Dictionary<Texture2D, Sheet>();
            foreach (string path in paths)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (!cache.TryGetValue(sprite.texture, out var sheet))
                {
                    var imp = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite.texture));
                    if (!imp.isReadable) { imp.isReadable = true; imp.SaveAndReimport(); }
                    sheet = new Sheet(sprite.texture); cache.Add(sprite.texture, sheet);
                }
                int id = sheet.Pose(sprite);
                if (id == 0) throw new InvalidOperationException("No silhouette: " + path);
                var island = sheet.islands[id];
                if (island.Rect.width > sprite.rect.width * 1.8f || island.Rect.height > sprite.rect.height * 1.8f)
                    throw new InvalidOperationException("Connected poses require explicit separation: " + path + " " + island.Rect);
                var accepted = new HashSet<int> { id };
                // The lifted boulder is intentionally detached from the hand.
                if (sprite.name == "CaimanTerrestre_Rock2")
                    for (int other = 1; other < sheet.islands.Count; other++)
                    {
                        var part = sheet.islands[other];
                        if (other != id && part.pixels.Count >= 1500 && part.pixels.Count < island.pixels.Count * .6f && sprite.rect.Contains(part.Rect.center))
                            accepted.Add(other);
                    }
                var old = sprite.rect;
                var anchor = old.position + sprite.pivot;
                int left = sheet.Width, bottom = sheet.Height, right = 0, top = 0;
                for (int p = 0; p < sheet.pixels.Length; p++)
                {
                    if (sheet.pixels[p].a == 0 || !accepted.Contains(sheet.owner[p])) continue;
                    int x = p % sheet.Width, y = p / sheet.Width;
                    left = Math.Min(left, x); right = Math.Max(right, x + 1);
                    bottom = Math.Min(bottom, y); top = Math.Max(top, y + 1);
                }
                left = Math.Max(0, left - 2); right = Math.Min(sheet.Width, right + 2);
                bottom = Math.Max(0, bottom - 2); top = Math.Min(sheet.Height, top + 2);
                var rect = new Rect(left, bottom, right - left, top - bottom);
                var pivot = new Vector2((anchor.x - left) / rect.width, (anchor.y - bottom) / rect.height);
                var replacement = Sprite.Create(sprite.texture, rect, pivot, sprite.pixelsPerUnit, 0, SpriteMeshType.Tight);
                var vertices = new List<Vector2>(); var triangles = new List<ushort>();
                // Merge identical scanline spans into rectangles. Geometry follows
                // the space between silhouettes, retaining transparent edge pixels
                // for bilinear filtering without importing a neighbouring pose.
                var active = new Dictionary<Vector2Int, RectInt>();
                for (int y = bottom; y <= top; y++)
                {
                    var spans = new HashSet<Vector2Int>();
                    if (y < top)
                        for (int x = left; x < right;)
                        {
                            if (!accepted.Contains(sheet.owner[y * sheet.Width + x])) { x++; continue; }
                            int start = x++;
                            while (x < right && accepted.Contains(sheet.owner[y * sheet.Width + x])) x++;
                            spans.Add(new Vector2Int(start, x));
                        }
                    foreach (var key in active.Keys.Where(k => !spans.Contains(k)).ToArray())
                    { AddRect(active[key]); active.Remove(key); }
                    foreach (var span in spans)
                    {
                        if (active.TryGetValue(span, out var run)) { run.height++; active[span] = run; }
                        else active[span] = new RectInt(span.x, y, span.y - span.x, 1);
                    }
                }
                // Use the editor data API, also used by Unity's sprite importer;
                // OverrideGeometry is restricted to the player loop in this editor.
                using (var positions = new NativeArray<Vector3>(vertices.Select(v => (Vector3)((v - replacement.pivot) / replacement.pixelsPerUnit)).ToArray(), Allocator.Temp))
                using (var uv = new NativeArray<Vector2>(vertices.Select(v => new Vector2((left + v.x) / sheet.Width, (bottom + v.y) / sheet.Height)).ToArray(), Allocator.Temp))
                using (var indices = new NativeArray<ushort>(triangles.ToArray(), Allocator.Temp))
                {
                    replacement.SetVertexCount(vertices.Count);
                    replacement.SetVertexAttribute(VertexAttribute.Position, positions);
                    replacement.SetVertexAttribute(VertexAttribute.TexCoord0, uv);
                    replacement.SetIndices(indices);
                }
                replacement.name = sprite.name;
                EditorUtility.CopySerialized(replacement, sprite);
                UnityEngine.Object.DestroyImmediate(replacement); EditorUtility.SetDirty(sprite);
                // Preserve the existing world-space sole and all animation GUIDs.
                // Changing the rectangle does not change the anchor in the sheet.
                if (footprints != null)
                    for (int f = 0; f < footprints.entries.Length; f++)
                    {
                        var entry = footprints.entries[f]; if (entry.sprite != sprite) continue;
                        int sole = bottom;
                        for (int y = bottom; y < top; y++)
                        {
                            int count = 0;
                            for (int x = left; x < right; x++)
                            {
                                int p = y * sheet.Width + x; var c = sheet.pixels[p];
                                if (accepted.Contains(sheet.owner[p]) && c.a > 180 && Math.Min(c.r, Math.Min(c.g, c.b)) < 220 && c.r > c.g * .8f && c.b < c.r * 1.1f) count++;
                            }
                            if (count >= Math.Max(4, rect.width * .035f)) { sole = y; break; }
                        }
                        entry.bottom = (sole - anchor.y) / sprite.pixelsPerUnit; footprints.entries[f] = entry;
                    }
                notes.Add(sprite.name + " | " + old + " -> " + rect + " | core=" + island.pixels.Count + " | vertices=" + vertices.Count);
                void AddRect(RectInt run)
                {
                    if (vertices.Count + 4 >= ushort.MaxValue) throw new InvalidOperationException("Sprite mesh too large: " + path);
                    ushort v = (ushort)vertices.Count;
                    vertices.Add(new Vector2(run.xMin - left, run.yMin - bottom));
                    vertices.Add(new Vector2(run.xMax - left, run.yMin - bottom));
                    vertices.Add(new Vector2(run.xMax - left, run.yMax - bottom));
                    vertices.Add(new Vector2(run.xMin - left, run.yMax - bottom));
                    triangles.Add(v); triangles.Add((ushort)(v + 1)); triangles.Add((ushort)(v + 2));
                    triangles.Add(v); triangles.Add((ushort)(v + 2)); triangles.Add((ushort)(v + 3));
                }
            }
            if (footprints != null) EditorUtility.SetDirty(footprints);
            AssetDatabase.SaveAssets();
            File.WriteAllLines(Path.Combine(Output, "siluetas.txt"), notes);
            Debug.Log("Precise sprite silhouettes: " + paths.Length);
        }

        public static void BakeAndPreview() { Bake(); Preview(); }
        public static void Preview()
        {
            Directory.CreateDirectory(Output);
            var paths = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Animations" })
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith(".asset") && Target(p)).OrderBy(p => p).ToArray();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Sprite review").AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 4;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.09f, .13f, .15f, 1);
            camera.transform.position = new Vector3(0, 0, -10);
            var materials = new Dictionary<Texture2D, Material>();
            var actors = new List<GameObject>();
            for (int page = 0; page * 8 < paths.Length; page++)
            {
                foreach (var actor in actors) UnityEngine.Object.DestroyImmediate(actor); actors.Clear();
                for (int slot = 0; slot < 8 && page * 8 + slot < paths.Length; slot++)
                {
                    var s = AssetDatabase.LoadAssetAtPath<Sprite>(paths[page * 8 + slot]);
                    if (!materials.TryGetValue(s.texture, out var material))
                    {
                        material = new Material(Shader.Find("Sprites/Default")) { mainTexture = s.texture };
                        materials.Add(s.texture, material);
                    }
                    for (int side = 0; side < 2; side++)
                    {
                        var go = new GameObject(s.name); actors.Add(go);
                        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = s; sr.sharedMaterial = material; sr.flipX = side == 1;
                        float scale = Mathf.Min(2.2f / s.bounds.size.x, 1.55f / s.bounds.size.y);
                        go.transform.localScale = Vector3.one * scale;
                        float x = -9.1f + (slot % 4) * 5.2f + side * 2.45f, y = 1.95f - (slot / 4) * 4;
                        go.transform.position = new Vector3(x - (side == 1 ? -s.bounds.center.x : s.bounds.center.x) * scale, y - s.bounds.center.y * scale);
                    }
                    var label = new GameObject("Name"); actors.Add(label);
                    var text = label.AddComponent<TextMesh>(); text.text = s.name; text.fontSize = 36; text.characterSize = .085f; text.anchor = TextAnchor.MiddleCenter;
                    label.transform.position = new Vector3(-7.875f + (slot % 4) * 5.2f, .7f - (slot / 4) * 4, -1);
                }
                var rt = new RenderTexture(2080, 800, 24); camera.targetTexture = rt; camera.Render();
                var previous = RenderTexture.active; RenderTexture.active = rt;
                var shot = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false); shot.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); shot.Apply();
                File.WriteAllBytes(Path.Combine(Output, "poses-" + page + ".png"), shot.EncodeToPNG());
                RenderTexture.active = previous; camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(shot); UnityEngine.Object.DestroyImmediate(rt);
            }
            foreach (var material in materials.Values) UnityEngine.Object.DestroyImmediate(material);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        public static void Build()
        {
            bool release = Environment.GetCommandLineArgs().Contains("-juanRelease");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = Path.Combine(Output, release ? "Juego/Windows/JuanCapi.exe" : "Pruebas/Windows/JuanCapi.exe"),
                target = BuildTarget.StandaloneWindows64, options = release ? BuildOptions.None : BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Sprite validation build failed");
        }
        public static void BakePreviewAndBuild() { Bake(); Preview(); Build(); }
    }
}
