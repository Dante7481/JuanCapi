using UnityEngine;
namespace JuanCapi
{
    public sealed class SwampCamera:MonoBehaviour
    {
        public bool aquatic;
        private Camera view;
        private void Awake(){view=GetComponent<Camera>();}
        private void LateUpdate()
        {
            var game=GameRuntime.Instance;var level=SwampLevel.Current;if(game?.Player==null||level==null||game.UI.IsOpen)return;
            bool fight=level.boss.Fighting;float size=fight?(aquatic?8:6.7f):aquatic?6:5.6f;
            view.orthographicSize=Mathf.Lerp(view.orthographicSize,size,1-Mathf.Exp(-5*Time.deltaTime));
            float half=view.orthographicSize*view.aspect;
            bool escaping=!aquatic&&level.boss.StateName=="Retreat";
            Vector3 p=escaping?level.boss.transform.position:game.Player.transform.position;var desired=new Vector3(Mathf.Clamp(p.x+2,half,aquatic?110-half:128-half),aquatic?Mathf.Clamp(p.y+.7f,view.orthographicSize,20-view.orthographicSize):3.1f,-10);
            if(fight)desired=new Vector3(aquatic?78:94,aquatic?9:4,-10);
            transform.position=Vector3.Lerp(transform.position,desired,1-Mathf.Exp(-5*Time.deltaTime));
        }
    }
}
