using UnityEngine;
namespace JuanCapi
{
    public sealed class MineCamera:MonoBehaviour
    {
        public bool directed;private Camera view;
        private void Awake(){view=GetComponent<Camera>();}
        public void Cut(Vector3 position,float size=5.6f){directed=true;transform.position=position;view.orthographicSize=size;}
        private void LateUpdate()
        {
            var game=GameRuntime.Instance;var level=MineLevel.Current;if(directed||game?.Player==null||level==null||game.UI.IsOpen)return;
            bool fight=level.boss.Fighting;float size=fight?7:5.6f;
            view.orthographicSize=Mathf.Lerp(view.orthographicSize,size,1-Mathf.Exp(-5*Time.deltaTime));float half=view.orthographicSize*view.aspect;
            var p=game.Player.transform.position;Vector3 target=fight?new Vector3(142,4,-10):new Vector3(Mathf.Clamp(p.x+2,half,165-half),game.Player.aquatic?-2:3.1f,-10);
            transform.position=Vector3.Lerp(transform.position,target,1-Mathf.Exp(-5*Time.deltaTime));
        }
    }
}
