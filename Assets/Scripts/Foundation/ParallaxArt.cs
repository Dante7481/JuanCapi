using UnityEngine;

namespace JuanCapi
{
    public sealed class ParallaxArt : MonoBehaviour
    {
        [Range(0,1)] public float factor=.65f;
        private Vector3 origin,cameraOrigin;
        private Transform cameraTransform;
        private SpriteRenderer artwork;
        private void Start()
        {
            if(Camera.main==null)return;
            cameraTransform=Camera.main.transform;origin=transform.position;cameraOrigin=cameraTransform.position;artwork=GetComponent<SpriteRenderer>();
        }
        private void LateUpdate()
        {
            if(cameraTransform==null)return;
            var delta=cameraTransform.position-cameraOrigin;
            transform.position=origin+new Vector3(delta.x*factor,delta.y*.3f,0);
            if(artwork!=null&&artwork.sprite!=null&&Camera.main.orthographic)
            {
                var camera=Camera.main;float halfY=camera.orthographicSize,halfX=halfY*camera.aspect;
                var size=artwork.sprite.bounds.size;float scale=Mathf.Max(halfY*2.12f/size.y,halfX*2.12f/size.x);
                transform.localScale=Vector3.one*scale;
                float marginX=size.x*scale*.5f-halfX,marginY=size.y*scale*.5f-halfY;
                var p=transform.position;p.x=Mathf.Clamp(p.x,cameraTransform.position.x-marginX,cameraTransform.position.x+marginX);p.y=Mathf.Clamp(p.y,cameraTransform.position.y-marginY,cameraTransform.position.y+marginY);transform.position=p;
            }
        }
    }
}
