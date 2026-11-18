using UnityEngine;

namespace JuanCapi
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class GameAudioChannel : MonoBehaviour
    {
        public bool music;
        [Range(0,1)] public float sourceVolume=1;
        private AudioSource source;
        private void Awake(){source=GetComponent<AudioSource>();}
        private void Update()
        {
            var settings=GameRuntime.Instance?.Settings;
            if(settings!=null)source.volume=sourceVolume*(music?settings.music:settings.effects);
        }
    }
}
