using System.Collections;
using UnityEngine;

namespace JuanCapi
{
    [DefaultExecutionOrder(80)]
    public sealed class SierraLevel:MonoBehaviour
    {
        public static SierraLevel Current { get; private set; }
        public RastreadorBoss boss;
        public SierraCamera view;
        public Transform drone;
        public AnimatedActor mara,stuntRastreador;
        public GameObject maraGate;
        public SierraProp[] props;
        public bool GuardDefeated { get; set; }
        public bool IntroRunning { get; private set; }
        private bool introDoneRequested,checkpointFour,intercepted;
        private float introClock,nextSmoke;
        private Vector3 fallAnchor=new Vector3(2,.08f,0);
        private GameRuntime Game=>GameRuntime.Instance;
        private void Awake(){Current=this;}
        private void OnDestroy(){if(Current==this)Current=null;}
        public void Prepare(CampaignData data)
        {
            Current=this;
            if(data.sierraLayout<5)
            {
                float x=data.dash?96:data.checkpoint=="CP_B04"?73:data.rescues.Contains("MARA")?34:data.sword?11:2;
                data.x=x;data.y=.08f;data.z=0;data.hasPosition=true;data.sierraLayout=5;
                if(data.dash&&!data.defeated.Contains("RASTREADOR"))data.defeated.Add("RASTREADOR");
            }
            // A save made inside an unresolved arena always resumes outside its entrance.
            if(!data.dash&&data.x>75){data.x=73;data.y=.08f;data.z=0;}
            fallAnchor=new Vector3(data.x,data.y,data.z);
        }
        public Vector3 SafeSavePosition(Vector3 position)
        {
            if(boss!=null&&boss.Fighting)return new Vector3(73,.08f,0);
            var ground=Physics2D.Raycast(position+Vector3.up*.15f,Vector2.down,.5f,1<<3);
            if(ground.collider!=null&&ground.normal.y>.5f&&ground.collider.bounds.size.x>1.2f)
                position.x=Mathf.Clamp(position.x,ground.collider.bounds.min.x+.6f,ground.collider.bounds.max.x-.6f);
            return position;
        }
        private IEnumerator Start()
        {
            while(Game==null||Game.Busy||Game.Data==null)yield return null;
            GuardDefeated=Game.Data.rescues.Contains("MARA");
            if(GuardDefeated)
                foreach(var e in FindObjectsByType<SierraEnemy>(FindObjectsSortMode.None))if(e.persistentId=="MARA_GUARD")e.gameObject.SetActive(false);
            maraGate.SetActive(!GuardDefeated);stuntRastreador.gameObject.SetActive(false);
            mara.Play("Idle");checkpointFour=Game.Data.checkpoint=="CP_B04"||Game.Data.dash;
            if(!Game.Data.introSeen&&!Game.Data.sword&&!Game.IsPractice)yield return Introduction();
            else {drone.position=new Vector3(11,.55f,0);view.directed=false;}
        }
        private IEnumerator Introduction()
        {
            IntroRunning=true;Game.Cinematic=true;introClock=0;
            boss.gameObject.SetActive(false);
            view.Cut(new Vector3(34,3.4f,-10));
            while(!introDoneRequested&&introClock<17)
            {
                if(Game.UI.ScreenName=="cinematic"||introClock==0)
                {
                    introClock+=Time.deltaTime;
                    if(introClock<5)
                    {
                        Game.UI.CinematicFrame("Mara: Busca ayuda. Lleva la herramienta y estas pruebas.",()=>CompleteIntro(true));
                        drone.position=Vector3.Lerp(new Vector3(34,3.6f,0),new Vector3(42,6,0),introClock/5);
                        mara.Play("Interact");
                    }
                    else if(introClock<9)
                    {
                        if(!stuntRastreador.gameObject.activeSelf){view.Cut(new Vector3(86,3.4f,-10));stuntRastreador.gameObject.SetActive(true);stuntRastreador.Play("Attack",true);}
                        Game.UI.CinematicFrame("Rastreador: ¡Ese registro no saldrá del valle!",()=>CompleteIntro(true));
                        drone.position=new Vector3(84+(introClock-5)*.3f,5.5f-(introClock-5)*.45f,0);
                        drone.rotation=Quaternion.Euler(0,0,(introClock-5)*15);
                        if(!intercepted&&introClock>5.8f){intercepted=true;PlasmaEffects.Beam(stuntRastreador.transform.position+new Vector3(-.7f,1.6f),drone.position,new Color(1,.66f,.2f));}
                        if(Time.time>=nextSmoke){nextSmoke=Time.time+.3f;PlasmaEffects.Burst(drone.position,new Color(.95f,.55f,.2f),.6f);}
                    }
                    else
                    {
                        if(stuntRastreador.gameObject.activeSelf){stuntRastreador.gameObject.SetActive(false);view.Cut(new Vector3(9,3.1f,-10));}
                        float t=Mathf.Clamp01((introClock-9)/4);
                        drone.position=Vector3.Lerp(new Vector3(17,10,0),new Vector3(11,.55f,0),t);
                        drone.rotation=Quaternion.Euler(0,0,Mathf.Lerp(35,0,t));
                        if(introClock<13&&Time.time>=nextSmoke){nextSmoke=Time.time+.22f;PlasmaEffects.Burst(drone.position,new Color(.57f,.49f,.39f,.55f),.55f);}
                        Game.UI.CinematicFrame(introClock<13?"Dron de auxilio · descenso de emergencia":"Mara, por radio: Juan, usa la espada. Estoy en el taller.",()=>CompleteIntro(true));
                    }
                }
                yield return null;
            }
            if(!introDoneRequested)CompleteIntro(false);
        }
        public void CompleteIntro(bool skipped)
        {
            if(!IntroRunning||Game.Busy)return;
            if(!Game.Transaction(d=>
            {
                d.introSeen=true;d.x=skipped?11:2;d.y=.08f;d.z=0;d.hasPosition=true;
                if(skipped&&!d.sword){d.Grant(CampaignReward.Sword);if(!d.collected.Contains("CAPSULE"))d.collected.Add("CAPSULE");}
                return true;
            },skipped?"Espada de plasma recuperada":"Quebrada de la sierra"))return;
            introDoneRequested=true;IntroRunning=false;Game.Cinematic=false;
            drone.position=new Vector3(11,.55f,0);drone.rotation=Quaternion.identity;
            stuntRastreador.gameObject.SetActive(false);boss.gameObject.SetActive(true);view.directed=false;
            Game.Player.transform.position=new Vector3(skipped?11:2,.08f,0);Game.Player.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;
            Game.UI.Play();if(skipped)ShowSwordReward();
        }
        private void Update()
        {
            if(Game?.CanPlay!=true)return;
            var p=Game.Player.transform.position;
            if(p.y<-4.5f)
            {
                if(p.x>101&&Game.Data.dash){Game.Player.transform.position=new Vector3(102,.08f,0);Game.Player.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;}
                else Game.Player.RespawnAt(fallAnchor);
                return;
            }
            if(Game.Player.EnSuelo&&!Game.Player.InDanger&&p.x<104)
            {
                var ground=Physics2D.Raycast(p+Vector3.up*.15f,Vector2.down,.5f,1<<3);
                if(ground.collider!=null&&ground.normal.y>.5f)fallAnchor=SafeSavePosition(p)+Vector3.up*.05f;
            }
            if(p.x>70&&!checkpointFour&&Game.Data.rescues.Contains("MARA"))
            {
                checkpointFour=Game.Transaction(d=>{d.checkpoint="CP_B04";d.x=73;d.y=.08f;d.z=0;d.hasPosition=true;return true;},"Puesto de vigilancia");
            }
            SierraProp nearest=null;float distance=2.3f;
            foreach(var prop in props)
            {
                if(prop==null||!prop.gameObject.activeInHierarchy||!prop.Usable)continue;
                float length=Vector2.Distance(p,prop.transform.position);
                if(length<distance){nearest=prop;distance=length;}
            }
            Game.UI.PromptIcon(nearest!=null?nearest.Hint:null);
            if(nearest!=null&&GameInput.Down("Interactuar"))nearest.Use();
            if(Game.Data.dash&&p.x>96&&p.x<112)Game.UI.Lesson("Dash","dash");
            else if(p.x<8||p.x>46&&p.x<63)Game.UI.Lesson("Saltar","jump");
            else if(Game.Data.sword&&(p.x>15&&p.x<20||p.x>27&&p.x<35&&!Game.Data.rescues.Contains("MARA")))Game.UI.Lesson("Atacar","sword");
            else Game.UI.Lesson(null,null);
        }
        public void MaraRescued()
        {GuardDefeated=true;maraGate.SetActive(false);mara.Play("Celebrate",true);}
        public void ShowSwordReward(){PlasmaEffects.Burst(Game.Player.transform.position+Vector3.up,new Color(.2f,.95f,1),1.4f);}
        public void ShowDashReward(Vector3 position)
        {
            var go=new GameObject("Impulsor recuperado");go.transform.position=position;
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=CampaignArt.Current.Icon("dash");sr.sortingOrder=40;
            go.transform.localScale=Vector3.one*(1/sr.sprite.bounds.size.y);go.AddComponent<FloatingArtwork>();Destroy(go,4);
            PlasmaEffects.Burst(position,new Color(.3f,1,1),1.8f);
        }
        public bool FinishLevel()
        {
            if(!Game.CanPlay||!Game.Data.dash||!Game.Data.rescues.Contains("MARA"))return false;
            if(!Game.Data.sierraFinished&&!Game.Transaction(d=>{d.sierraFinished=true;d.sierraSeconds=d.activeSeconds;d.checkpoint="CP_B06";return true;},"Quebrada superada"))return false;
            Game.UI.Lesson(null,null);Game.UI.SierraResults();return true;
        }
    }
}
