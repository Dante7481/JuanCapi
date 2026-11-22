using System.Collections.Generic;
using UnityEngine;
namespace JuanCapi
{
    public sealed class CampaignAudio:MonoBehaviour
    {
        public static CampaignAudio Current {get;private set;}
        private readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        private readonly Dictionary<string,float> last=new Dictionary<string,float>();
        private AudioSource[] music,voices;private AudioSource ambient,ui;private string track,environment;private int active;private float blend=1,nextStep;private string previousState;private int swing;private PlayerController previousPlayer;private bool paused;
        public string Track=>track;public string EnvironmentTrack=>environment;
        public int PlayedEffects {get;private set;} public int LoadedClips=>clips.Count;
        private GameRuntime Game=>GameRuntime.Instance;
        private AudioSource Source(string name,bool loop)
        {var go=new GameObject(name);go.transform.SetParent(transform,false);var s=go.AddComponent<AudioSource>();s.playOnAwake=false;s.loop=loop;s.spatialBlend=0;s.volume=0;return s;}
        private void Awake()
        {
            Current=this;foreach(var c in Resources.LoadAll<AudioClip>("Audio"))clips[c.name]=c;
            music=new[]{Source("Música A",true),Source("Música B",true)};ambient=Source("Ambiente",true);ui=Source("Interfaz",false);voices=new AudioSource[8];for(int i=0;i<voices.Length;i++)voices[i]=Source("Efecto "+i,false);
        }
        private void OnDestroy(){if(Current==this)Current=null;}
        public static void Play(string name,bool interfaceSound=false,bool priority=false)=>Current?.Effect(name,interfaceSound,priority);
        private void Effect(string name,bool interfaceSound,bool priority)
        {
            if(!clips.TryGetValue(name,out var clip)||!interfaceSound&&Game?.CanPlay!=true)return;
            if(last.TryGetValue(name,out float time)&&Time.unscaledTime-time<(name=="warning"?.35f:.055f))return;last[name]=Time.unscaledTime;
            var source=interfaceSound?ui:System.Array.Find(voices,s=>!s.isPlaying);if(source==null){if(!priority)return;source=voices[0];source.Stop();}
            source.volume=(Game?.Settings.effects??.8f)*(name=="warning"?.4f:interfaceSound?.18f:.17f);source.pitch=name=="step"||name=="metal"||name=="swim"?Random.Range(.94f,1.06f):1;source.clip=clip;source.Play();PlayedEffects++;
        }
        private void Update()
        {
            if(Game?.UI==null)return;
            bool boss=SierraLevel.Current?.boss.Fighting==true||SwampLevel.Current?.boss.Fighting==true||MineLevel.Current?.boss.Fighting==true;
            string desired=Game.Data==null?"menu":Game.Data.completed&&Game.Cinematic?"epilogue":boss?"boss":Game.Data.scene=="LV1"?"sierra":Game.Data.scene=="MinerTown"?"mine":Game.Player?.aquatic==true?"water":"swamp";
            if(track!=desired&&clips.TryGetValue(desired,out var clip)){track=desired;active=1-active;music[active].clip=clip;music[active].Play();blend=0;}
            blend=Mathf.MoveTowards(blend,1,Time.unscaledDeltaTime*.6f);float gain=Game.Settings.music*(Game.UI.ScreenName=="cinematic"?.24f:Game.UI.IsOpen&&Game.Data!=null?.15f:.25f);
            music[active].volume=gain*blend;music[1-active].volume=gain*(1-blend);if(blend==1&&music[1-active].isPlaying)music[1-active].Stop();
            string env=Game.Data==null?"wind":Game.Player?.aquatic==true?"underwater":Game.Data.scene=="MinerTown"?"factory":Game.Data.scene=="LV1"?"wind":"wetland";
            if(environment!=env&&clips.TryGetValue(env,out var ac)){environment=env;ambient.clip=ac;ambient.Play();}
            ambient.volume=Game.Settings.ambience*.12f;bool pause=Game.Data!=null&&!Game.CanPlay&&!Game.Cinematic;
            if(pause!=paused){paused=pause;foreach(var s in voices){if(pause)s.Pause();else s.UnPause();}if(pause)ambient.Pause();else ambient.UnPause();}
            if(Game.Player==null||!Game.CanPlay)return;
            var p=Game.Player;if(previousPlayer!=p){previousPlayer=p;previousState=null;swing=p.AttackSequence;}
            string state=p.VisualState;
            if(state!=previousState){if(state=="Jump")Play("jump");if(state=="Dash")Play("dash");if(previousState=="Fall"&&p.EnSuelo)Play("land");previousState=state;}
            if(swing!=p.AttackSequence){swing=p.AttackSequence;Play("sword");}
            if((state=="Run"||state=="Swim"||state=="Up"||state=="Down")&&Time.time>=nextStep){nextStep=Time.time+(p.aquatic?.48f:.3f);Play(p.aquatic?"swim":Game.Data.scene=="MinerTown"?"metal":"step");}
        }
    }
}
