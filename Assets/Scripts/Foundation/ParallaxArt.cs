using UnityEngine;

namespace JuanCapi
{
    public sealed class ParallaxArt : MonoBehaviour
    {
        [Range(0,1)] public float factor=.65f;
        private Vector3 origin,cameraOrigin;
        private Transform cameraTransform;
        private void Start()
        {
            if(Camera.main==null)return;
            cameraTransform=Camera.main.transform;origin=transform.position;cameraOrigin=cameraTransform.position;
        }
        private void LateUpdate()
        {
            if(cameraTransform==null)return;
            var delta=cameraTransform.position-cameraOrigin;
            transform.position=origin+new Vector3(delta.x*factor,delta.y*.3f,0);
        }
    }
}
