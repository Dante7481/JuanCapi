using System;
using UnityEngine;

namespace JuanCapi
{
    [CreateAssetMenu(menuName="Juan Capi/Arte de campaña")]
    public sealed class CampaignArt : ScriptableObject
    {
        public Sprite sierra, swamp, underwater, mine;
        public Sprite[] equipment, terrain, allies, enemies, aimBodies;
        public RuntimeAnimatorController playerLand, playerWater, caimanLand, caimanWater, rastreador, director;
        public RuntimeAnimatorController[] allyControllers, enemyControllers;
        public Sprite playerPortrait, caimanPortrait, rastreadorPortrait, directorPortrait, menuHero;
        public Material enemyMaterial;
        private static CampaignArt instance;
        public static CampaignArt Current => instance != null ? instance : instance = Resources.Load<CampaignArt>("Campaign/Art");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { instance = null; }
        public Sprite Icon(string key)
        {
            int index=Array.IndexOf(new[]{"sword","gun","shield","dash","fruit","cell","gold","scrap","drone","capsule","bench","crate","cage","valve","key","record"},key);
            return index>=0&&equipment!=null&&index<equipment.Length?equipment[index]:null;
        }
    }
}
