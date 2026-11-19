using UnityEngine;
namespace JuanCapi
{
    public sealed class SierraCamera:MonoBehaviour
    {
        public bool directed;
        public Vector3 shot;
        private void LateUpdate()
        {
            var level=SierraLevel.Current;var player=GameRuntime.Instance?.Player;
            if(player==null)return;
            if(GameRuntime.Instance.UI.ScreenName!="game"&&GameRuntime.Instance.UI.ScreenName!="cinematic")return;
            Vector3 desired=directed?shot:new Vector3(Mathf.Clamp(player.transform.position.x+2.4f,8,114),Mathf.Clamp(player.transform.position.y+3.1f,3.1f,5.6f),-10);
            if(!directed&&level!=null&&level.boss!=null&&level.boss.Fighting)desired=new Vector3(85,3.2f,-10);
            transform.position=Vector3.Lerp(transform.position,desired,1-Mathf.Exp(-5*Time.unscaledDeltaTime));
        }
        public void Cut(Vector3 target){shot=target;directed=true;transform.position=target;}
    }
}
