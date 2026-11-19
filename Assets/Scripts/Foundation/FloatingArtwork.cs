using UnityEngine;

namespace JuanCapi
{
    public sealed class FloatingArtwork : MonoBehaviour
    {
        private Vector3 origin;
        private float phase;
        private void Start(){origin=transform.localPosition;phase=transform.position.x*.73f;}
        private void Update(){transform.localPosition=origin+Vector3.up*Mathf.Sin(Time.time*2.5f+phase)*.07f;}
    }
}
