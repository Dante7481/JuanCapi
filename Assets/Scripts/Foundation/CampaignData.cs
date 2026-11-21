using System;
using System.Collections.Generic;
using UnityEngine;

namespace JuanCapi
{
    public enum CampaignReward { Sword, Dash, PlasmaGun, PlasmaShield, Director }

    [Serializable]
    public sealed class CampaignData
    {
        public string campaignId = Guid.NewGuid().ToString("N");
        public string scene = "LV1";
        public string region = "Quebrada de la sierra";
        public string checkpoint = "CP_B01";
        public bool hasPosition;
        public float x, y, z;
        public int maxHearts = 3;
        public int healthQuarters = 12;
        public int heartPieces;
        public int gold, scrap, fruit, cells;
        public bool sword, dash, gun, shield, completed, introSeen;
        public bool fastReload, reinforcedShield, maintenanceKey;
        public int ammunition = 3;
        public float reloadRemaining, shieldEnergy = 100;
        public string equippedWeapon = "sword";
        public string assignedConsumable = "fruit";
        public double activeSeconds;
        public int deaths;
        public int sierraLayout;
        public bool sierraFinished;
        public double sierraSeconds;
        public int swampLayout;
        public bool swampFinished;
        public double swampStarted, swampSeconds;
        public List<string> collected = new List<string>();
        public List<string> defeated = new List<string>();
        public List<string> rescues = new List<string>();
        public List<string> records = new List<string>();
        public List<string> purchases = new List<string>();

        public int MaximumHealth => maxHearts * 4;
        public int MaximumShield => reinforcedShield ? 125 : 100;
        public CampaignData Clone() => JsonUtility.FromJson<CampaignData>(JsonUtility.ToJson(this));
        public bool Heal(int quarters)
        {
            if (quarters <= 0 || healthQuarters <= 0 || healthQuarters >= MaximumHealth) return false;
            healthQuarters = Math.Min(MaximumHealth, healthQuarters + quarters);
            return true;
        }
        public bool Collect(string id, string item, int amount = 1)
        {
            if (string.IsNullOrWhiteSpace(id) || collected.Contains(id) || amount <= 0) return false;
            switch (item)
            {
                case "gold": if (gold > 9999 - amount) return false; gold += amount; break;
                case "scrap": if (scrap > 999 - amount) return false; scrap += amount; break;
                case "fruit": if (fruit + amount > 3) return false; fruit += amount; break;
                case "cell": if (cells + amount > 3) return false; cells += amount; break;
                case "heart": if (!Heal(4 * amount)) return false; break;
                case "piece":
                    if (maxHearts >= 20) return false;
                    heartPieces += amount;
                    while (heartPieces >= 4 && maxHearts < 20) { heartPieces -= 4; maxHearts++; healthQuarters = MaximumHealth; }
                    if (maxHearts == 20) heartPieces = 0;
                    break;
                case "container":
                    if (maxHearts >= 20) return false;
                    maxHearts = Math.Min(20, maxHearts + amount); healthQuarters = MaximumHealth; break;
                case "record": if (records.Count >= 6) return false; records.Add(id); break;
                default: return false;
            }
            collected.Add(id); return true;
        }
        public bool Use(string item)
        {
            if (healthQuarters <= 0) return false;
            if (item == "fruit" && fruit > 0 && Heal(12)) { fruit--; return true; }
            if (item == "cell" && cells > 0 && shield && shieldEnergy < MaximumShield)
            { cells--; shieldEnergy = Math.Min(MaximumShield, shieldEnergy + 50); return true; }
            return false;
        }
        public bool Grant(CampaignReward reward)
        {
            switch (reward)
            {
                case CampaignReward.Sword:
                    if (sword) return false; sword = true; checkpoint = "CP_B02"; break;
                case CampaignReward.Dash:
                    if (!sword || dash) return false; dash = true; checkpoint = "CP_B05";
                    Collect("BOSS_RASTREADOR_HEART", "container"); break;
                case CampaignReward.PlasmaGun:
                    if (!dash || gun) return false; gun = true; checkpoint = "CP_P05"; ammunition = 3; break;
                case CampaignReward.PlasmaShield:
                    if (!gun || shield) return false; shield = true; checkpoint = "CP_A05";
                    Collect("BOSS_CAIMAN_HEART", "container"); break;
                case CampaignReward.Director:
                    if (!shield || completed) return false; completed = true; checkpoint = "CP_M06"; break;
                default: return false;
            }
            return true;
        }
        public string PurchaseProblem(string id, string shelter)
        {
            if (!rescues.Contains("MARA")) return "El banco se abre al ayudar a Mara.";
            if (id == "reload" && (!gun || fastReload)) return "Requiere el disparador sin mejorar.";
            if (id == "shield" && (!shield || reinforcedShield)) return "Requiere el escudo sin mejorar.";
            if (id == "fruit" && fruit >= 3) return "Ya tienes tres frutas.";
            if (id == "cell" && (!shield || cells >= 3)) return "Requiere escudo y espacio en provisiones.";
            int price = id == "fruit" ? 15 : id == "cell" ? 20 : 80;
            int material = id == "fruit" || id == "cell" ? 0 : 15;
            if (id != "fruit" && id != "cell" && id != "reload" && id != "shield") return "Mejora desconocida.";
            if (gold < price || scrap < material) return "No tienes suficientes recursos.";
            int stock = purchases.FindAll(p => p.StartsWith(shelter + ":" + id + ":", StringComparison.Ordinal)).Count;
            if ((id == "fruit" && stock >= 2) || (id == "cell" && stock >= 1)) return "Agotado en este refugio.";
            return null;
        }
        public bool Purchase(string id, string shelter)
        {
            if (PurchaseProblem(id, shelter) != null) return false;
            gold -= id == "fruit" ? 15 : id == "cell" ? 20 : 80;
            if (id == "reload" || id == "shield") scrap -= 15;
            if (id == "fruit") fruit++;
            if (id == "cell") cells++;
            if (id == "reload") fastReload = true;
            if (id == "shield") { reinforcedShield = true; shieldEnergy += 25; }
            purchases.Add(shelter + ":" + id + ":" + purchases.Count);
            return true;
        }
        public bool IsValid(out string reason)
        {
            reason = "Datos de campaña inválidos.";
            if (string.IsNullOrEmpty(campaignId) || campaignId.Length > 64 || string.IsNullOrEmpty(region) || region.Length > 80) return false;
            if (scene != "LV1" && scene != "AmazoniaTerrestre" && scene != "AmazoniaAcuatica" && scene != "MinerTown") return false;
            if (!System.Text.RegularExpressions.Regex.IsMatch(checkpoint ?? "", @"^CP_[BPAM]0[1-6]$")) return false;
            if (maxHearts < 3 || maxHearts > 20 || healthQuarters < 0 || healthQuarters > MaximumHealth || heartPieces < 0 || heartPieces > 3) return false;
            if (gold < 0 || gold > 9999 || scrap < 0 || scrap > 999 || fruit < 0 || fruit > 3 || cells < 0 || cells > 3) return false;
            if ((dash && !sword) || (gun && !dash) || (shield && !gun) || (completed && !shield) || (fastReload && !gun) || (reinforcedShield && !shield)) return false;
            if (equippedWeapon != "sword" && equippedWeapon != "gun" || equippedWeapon == "gun" && !gun) return false;
            if (assignedConsumable != "fruit" && assignedConsumable != "cell") return false;
            if (ammunition < 0 || ammunition > 3 || !Finite(reloadRemaining) || reloadRemaining < 0 || reloadRemaining > 6 || !Finite(shieldEnergy) || shieldEnergy < 0 || shieldEnergy > MaximumShield) return false;
            if (!Finite(x) || !Finite(y) || !Finite(z) || Math.Abs(x) > 100000 || Math.Abs(y) > 100000 || Math.Abs(z) > 10000) return false;
            if (double.IsNaN(activeSeconds) || double.IsInfinity(activeSeconds) || activeSeconds < 0 || deaths < 0) return false;
            if (sierraLayout < 0 || double.IsNaN(sierraSeconds) || double.IsInfinity(sierraSeconds) || sierraSeconds < 0 || (sierraFinished && !dash)) return false;
            if (swampLayout < 0 || double.IsNaN(swampStarted) || double.IsInfinity(swampStarted) || swampStarted < 0 || double.IsNaN(swampSeconds) || double.IsInfinity(swampSeconds) || swampSeconds < 0 || (swampFinished && !shield)) return false;
            foreach (var list in new[] { collected, defeated, rescues, records, purchases })
            {
                if (list == null || list.Count > 10000 || new HashSet<string>(list).Count != list.Count) return false;
                foreach (var id in list) if (string.IsNullOrWhiteSpace(id) || id.Length > 160) return false;
            }
            if (records.Count > 6 || rescues.Count > 3) return false;
            reason = null; return true;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    [Serializable] public sealed class SaveRecord
    {
        public string id, savedUtc, kind, label, thumbnail;
        public long revision;
        public CampaignData data;
    }
    [Serializable] public sealed class CampaignArchive
    {
        public int schemaVersion = 3;
        public long revision;
        public List<SaveRecord> manual = new List<SaveRecord>();
        public List<SaveRecord> automatic = new List<SaveRecord>();
        public CampaignArchive Clone() => JsonUtility.FromJson<CampaignArchive>(JsonUtility.ToJson(this));
    }
}
