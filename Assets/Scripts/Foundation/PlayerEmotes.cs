using UnityEngine;
namespace JuanCapi
{
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerEmotes:MonoBehaviour
    {
        public string State {get;private set;}
        public bool Active=>State!=null;
        public static readonly string[] States={"Wazaaaa","Uiia","SixSeven"};
        AudioSource voice;float until;
        public bool Play(int index)
        {
            var game=GameRuntime.Instance;
            if(index<0||index>=3||game==null||!game.CanPlay||game.Player.isDead)return false;
            Cancel();State=States[index];
            if(voice==null){voice=gameObject.AddComponent<AudioSource>();voice.playOnAwake=false;voice.spatialBlend=0;}
            voice.clip=Resources.Load<AudioClip>("Audio/Emotes/"+new[]{"emote_wazaaaa","emote_uiia","emote_six_seven"}[index]);
            voice.volume=game.Settings.effects*.65f;voice.Play();until=Time.time+(voice.clip!=null?voice.clip.length:2.5f);
            GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;
            GetComponent<PlayerArtDriver>().actor.Play(State,true);return true;
        }
        void Update()
        {
            if(!Active)return;var game=GameRuntime.Instance;
            if(game==null||!game.CanPlay||game.Player.isDead||game.Player.VisualState=="Hurt"||Time.time>=until||
               GameInput.Held("Izquierda")||GameInput.Held("Derecha")||GameInput.Held("Nadar arriba")||GameInput.Held("Nadar abajo")||
               GameInput.Down("Saltar")||GameInput.Down("Dash")||GameInput.Down("Atacar")||GameInput.Down("Escudo"))Cancel();
        }
        public void Cancel(){State=null;if(voice!=null)voice.Stop();}
        void OnDisable(){Cancel();}
    }
}
