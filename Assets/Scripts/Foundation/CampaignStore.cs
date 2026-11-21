using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace JuanCapi
{
    // All records live in one atomic archive: a new campaign or a reward cannot partially commit.
    public sealed class CampaignStore
    {
        public const int FormatVersion = 3;
        [Serializable] private sealed class Envelope { public int version = FormatVersion; public string payload, sha256; }
        public const int AutoSaveCount = 5;
        public readonly string FilePath;
        private CampaignArchive archive = new CampaignArchive();
        private readonly Action beforeReplace;
        private readonly object gate = new object();
        public string Error { get; private set; }
        public bool RecoveryAvailable { get; private set; }
        public bool IsBlocked => Error != null;
        public bool HasRecords => archive.manual.Count > 0 || archive.automatic.Count > 0;
        public CampaignStore(string directory, Action beforeReplace = null)
        {
            FilePath = Path.Combine(directory, "campaign.json"); this.beforeReplace = beforeReplace; Refresh();
        }
        public List<SaveRecord> Records()
        {
            var copy = archive.Clone();
            var records = new List<SaveRecord>(copy.automatic);
            records.AddRange(copy.manual);
            return records.OrderByDescending(r => r.revision).ToList();
        }
        public void Refresh()
        {
            lock (gate)
            {
                Error = null; RecoveryAvailable = false; archive = new CampaignArchive();
                if (!File.Exists(FilePath) && !File.Exists(FilePath + ".bak")) return;
                try { archive = Read(FilePath); }
                catch (Exception e)
                {
                    Error = e.Message;
                    // A future schema must never silently fall back to old data.
                    if (e is NotSupportedException) return;
                    try { Read(FilePath + ".bak"); RecoveryAvailable = true; } catch { }
                }
            }
        }
        public bool Recover(out string error)
        {
            lock (gate)
            {
                try
                {
                    if (!RecoveryAvailable) throw new IOException("No hay un respaldo compatible.");
                    var recovered = Read(FilePath + ".bak");
                    if (File.Exists(FilePath)) File.Copy(FilePath, FilePath + ".damaged-" + DateTime.UtcNow.Ticks);
                    Write(recovered);
                    // Recovery must leave a usable backup too, not the damaged primary replaced above.
                    File.Copy(FilePath, FilePath + ".bak", true);
                    archive = recovered; Error = null; RecoveryAvailable = false; error = null; return true;
                }
                catch (Exception e) { error = e.Message; return false; }
            }
        }
        public bool StartNew(CampaignData initial, bool replaceConfirmed, out string error)
        {
            lock (gate)
            {
                if (IsBlocked) { error = Error; return false; }
                if (HasRecords && !replaceConfirmed) { error = "Confirma que deseas reemplazar la campaña actual."; return false; }
                var fresh = new CampaignArchive { revision = archive.revision };
                Add(fresh, initial, false, "Inicio de la aventura", null);
                return Commit(fresh, out error);
            }
        }
        public bool Save(CampaignData data, bool manual, string label, string thumbnail, out string error)
        {
            lock (gate)
            {
                if (IsBlocked) { error = Error; return false; }
                var currentId = Records().FirstOrDefault()?.data.campaignId;
                if (currentId != null && currentId != data.campaignId) { error = "La sesión pertenece a otra campaña."; return false; }
                var next = archive.Clone(); Add(next, data, manual, label, thumbnail);
                return Commit(next, out error);
            }
        }
        private static void Add(CampaignArchive target, CampaignData data, bool manual, string label, string thumbnail)
        {
            var record = new SaveRecord { id = Guid.NewGuid().ToString("N"), savedUtc = DateTime.UtcNow.ToString("o"),
                kind = manual ? "Manual" : "Autoguardado", label = label, thumbnail = thumbnail,
                revision = ++target.revision, data = data.Clone() };
            if (manual) { target.manual.Clear(); target.manual.Add(record); }
            else { target.automatic.Insert(0, record); if (target.automatic.Count > AutoSaveCount) target.automatic.RemoveAt(target.automatic.Count - 1); }
        }
        private bool Commit(CampaignArchive next, out string error)
        {
            try { Validate(next); Write(next); archive = next; error = null; return true; }
            catch (Exception e) { error = "No se guardó la partida: " + e.Message; return false; }
        }
        private void Write(CampaignArchive next)
        {
            next.schemaVersion=FormatVersion;
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            string payload = JsonUtility.ToJson(next);
            string encoded = JsonUtility.ToJson(new Envelope { payload = payload, sha256 = Hash(payload) });
            string temporary = FilePath + ".tmp";
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(encoded); stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
            }
            Read(temporary);
            beforeReplace?.Invoke();
            if (File.Exists(FilePath)) File.Replace(temporary, FilePath, FilePath + ".bak");
            else File.Move(temporary, FilePath);
        }
        private static CampaignArchive Read(string path)
        {
            if (!File.Exists(path)) throw new IOException("No se encuentra el archivo principal.");
            if (new FileInfo(path).Length > 12000000) throw new IOException("El archivo excede el tamaño permitido.");
            var envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(path, Encoding.UTF8));
            if (envelope == null) throw new IOException("Archivo ilegible.");
            if (envelope.version > FormatVersion) throw new NotSupportedException("Partida de una versión más reciente. No se sobrescribirá.");
            if (envelope.version < 1 || envelope.payload == null || Hash(envelope.payload) != envelope.sha256) throw new IOException("Archivo incompleto o dañado.");
            var result = JsonUtility.FromJson<CampaignArchive>(envelope.payload); Validate(result); return result;
        }
        private static void Validate(CampaignArchive data)
        {
            if (data == null) throw new IOException("No hay datos de campaña.");
            if (data.schemaVersion > FormatVersion) throw new NotSupportedException("Partida de una versión más reciente. No se sobrescribirá.");
            if (data.schemaVersion < 1 || data.revision < 1 || data.automatic == null || data.automatic.Count > AutoSaveCount || data.manual == null || data.manual.Count > 1) throw new IOException("Historial inválido.");
            var records = new List<SaveRecord>(data.automatic); records.AddRange(data.manual);
            if (records.Count == 0) throw new IOException("No hay registros válidos.");
            string id = records[0]?.data?.campaignId;
            var identities = new HashSet<string>();
            foreach (var r in records)
            {
                if (r == null || r.data == null || !r.data.IsValid(out _) || r.data.campaignId != id ||
                    string.IsNullOrEmpty(r.id) || !identities.Add(r.id) || r.revision < 1 || r.revision > data.revision ||
                    !DateTime.TryParse(r.savedUtc, out _) || (r.kind != "Manual" && r.kind != "Autoguardado") ||
                    r.label == null || r.label.Length > 160 || (r.thumbnail?.Length ?? 0) > 1000000)
                    throw new IOException("El registro no supera la validación.");
            }
            if (data.manual.Any(r => r.kind != "Manual") || data.automatic.Any(r => r.kind != "Autoguardado")) throw new IOException("Tipo de registro incorrecto.");
        }
        private static string Hash(string data)
        {
            using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(data)));
        }
    }
}
