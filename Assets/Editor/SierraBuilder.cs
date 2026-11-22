using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JuanCapi.Editor
{
    public static class SierraBuilder
    {
        private static CampaignArt art;
        private static Transform environment;
        private static readonly List<SierraProp> props=new List<SierraProp>();
        public static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Entregas/Fase5"));
        [MenuItem("Juan Capi/Nivel 1/Construir quebrada")]
        public static void BuildLevel()
        {
            Directory.CreateDirectory(Output);
            const string path="Assets/Scenes/LV1.unity";
            string backup=Path.Combine(Output,"LV1-antes-fase5.unity");if(!File.Exists(backup))File.Copy(path,backup);
            art=CampaignArt.Current;props.Clear();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("Quebrada de la sierra");var level=root.AddComponent<SierraLevel>();environment=root.transform;
            var cameraGO=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener),typeof(SierraCamera));cameraGO.tag="MainCamera";
            var camera=cameraGO.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=5.6f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.43f,.64f,.72f);
            cameraGO.transform.position=new Vector3(8,3.1f,-10);level.view=cameraGO.GetComponent<SierraCamera>();
            // The background moves only a fraction of the traversed distance; its entire visible range remains inside the panorama.
            var bg=Picture("Cordillera y quebrada",art.sierra,new Vector3(16,2.2f,4),new Vector2(45,15),-100);
            bg.gameObject.AddComponent<ParallaxArt>().factor=.85f;
            Platform(-5,13,0);Platform(15,45,0);Platform(49,59,0);Platform(62,104,0);Platform(112.4f,124,0);
            Platform(5.3f,7.6f,.8f,true);Platform(21,24,1,true);
            Platform(49.5f,52,1,true);Platform(53.5f,56,2,true);Platform(57.2f,59.3f,3,true);
            Platform(80,82,.65f,true);Platform(89.8f,91.8f,.65f,true);
            Platform(116.4f,119.4f,1,true);
            Gate("Límite de llegada",-5,7,false);Gate("Límite de salida",124,7,false);
            for(int i=0;i<18;i++)
            {
                float x=-2+i*6.7f;if(x>44&&x<49||x>59&&x<62||x>103&&x<112)continue;
                Picture("Ichu y pizarra",art.terrain[3],new Vector3(x,.25f,0),new Vector2(1.6f+i%3*.3f,.6f+i%2*.2f),i%3==0?22:2);
            }
            // Landmarks use the same wood, rope and cut stone vocabulary as the source game's props.
            Picture("Taller · cubierta",art.terrain[2],new Vector3(34,3.4f,1),new Vector2(8,.7f),-5);
            Picture("Taller · soporte izquierdo",art.terrain[1],new Vector3(30.5f,1.4f,1),new Vector2(.55f,3.1f),-6);
            Picture("Taller · soporte derecho",art.terrain[1],new Vector3(37.5f,1.4f,1),new Vector2(.55f,3.1f),-6);
            Picture("Puente de salida · cuerda izquierda",art.terrain[2],new Vector3(102,-.15f,0),new Vector2(3.8f,.6f),1);
            Picture("Puente de salida · cuerda derecha",art.terrain[2],new Vector3(113,-.15f,0),new Vector2(3.3f,.6f),1);
            CreatePlayer();
            var drone=Prop("Dron de Mara",SierraPropKind.Drone,"CAPSULE","drone",11,.55f,1.4f,1);level.drone=drone.transform;
            Prop("Valla de corte",SierraPropKind.Barrier,"B02_BARRIER","crate",18,0,1.2f,3.1f,true);
            var cage=Prop("Jaula de Mara",SierraPropKind.Cage,"MARA_CAGE","cage",33,0,2.3f,2.3f);
            cage.GetComponentInChildren<SpriteRenderer>().enabled=false;cage.gameObject.AddComponent<SierraCageArt>();
            var cageCollider=cage.gameObject.AddComponent<BoxCollider2D>();cageCollider.isTrigger=true;cageCollider.offset=Vector2.up;cageCollider.size=new Vector2(2,2);
            level.mara=Actor("Mara",art.allyControllers[0],art.allies[0],new Vector3(33,0,0),1);
            level.mara.sprite.sortingOrder=10;
            Prop("Banco de Mara",SierraPropKind.Bench,"MARA","bench",36,0,2,1.1f);
            Prop("Suministros del taller",SierraPropKind.Supplies,"B01_SUPPLIES","crate",39,0,1.2f,.9f);
            level.maraGate=Gate("Paso del taller",71,4,true);
            Enemy("MARA_GUARD",29,25,31,false);Enemy("DRONE_B04",51.5f,50,56,true);Enemy("GUARD_B04_A",66,63,67,false);Enemy("GUARD_B04_B",69,67.5f,70,false);
            Pickup("SEC_B01_GOLD","gold",15,22,1.6f);Pickup("SEC_B01_FRUIT","fruit",1,23,1.7f);Pickup("SEC_B01_PIECE","piece",1,24,1.8f);
            Pickup("SEC_B02_GOLD","gold",20,57.7f,3.6f);Pickup("SEC_B02_SCRAP","scrap",5,58.7f,3.6f);Pickup("SEC_B02_PIECE","piece",1,55.4f,2.6f);
            Pickup("SEC_B03_GOLD","gold",15,117,1.7f);Pickup("REC_B02","record",1,118.3f,1.7f);Pickup("SEC_B03_PIECE","piece",1,119.2f,1.7f);
            Pickup("REC_B01","record",1,34.5f,.65f);Pickup("B03_FRUIT","fruit",1,26,.65f);Pickup("B04_FRUIT","fruit",1,73,.65f);
            var bossGO=new GameObject("Rastreador · encuentro");bossGO.transform.SetParent(environment,false);bossGO.transform.position=new Vector3(88,0,0);
            var boss=bossGO.AddComponent<RastreadorBoss>();level.boss=boss;
            boss.actor=Actor("Rastreador",art.rastreador,art.rastreadorPortrait,Vector3.zero,1,bossGO.transform);
            var hurt=bossGO.AddComponent<BoxCollider2D>();hurt.isTrigger=true;hurt.offset=new Vector2(0,1.1f);hurt.size=new Vector2(1.5f,2.3f);
            boss.leftGate=Gate("Arena · entrada",75.7f,4,true);boss.rightGate=Gate("Arena · salida",95,4,true);
            level.stuntRastreador=Actor("Rastreador · intercepción",art.rastreador,art.rastreadorPortrait,new Vector3(88,0,0),1);level.stuntRastreador.Face(-1);
            Prop("Ruta al pantano",SierraPropKind.Exit,"B06_EXIT","valve",122,0,1.5f,1.7f);
            Picture("Señal del río",art.Icon("record"),new Vector3(121,2.7f,0),new Vector2(.8f,.9f),9);
            level.props=props.ToArray();
            EditorSceneManager.SaveScene(scene,path);AssetDatabase.SaveAssets();
            EditorBuildSettings.scenes=new[]{"Menu","LV1","AmazoniaTerrestre","AmazoniaAcuatica","MinerTown","Art/Sierra","Art/Pantano","Art/PantanoAcuatico","Art/MinerTown"}.Select(n=>"Assets/Scenes/"+n+".unity").Where(File.Exists).Select(n=>new EditorBuildSettingsScene(n,true)).ToArray();
            Debug.Log("SIERRA BUILT: 6 tramos, 3 secretos, 2 registros, 4 enemigos, Rastreador.");
        }
        private static void CreatePlayer()
        {
            var go=new GameObject("Juan");go.tag="Player";go.transform.position=new Vector3(2,.08f,0);go.transform.localScale=new Vector3(.625f,.625f,1);
            var rb=go.AddComponent<Rigidbody2D>();rb.gravityScale=1.7f;rb.freezeRotation=true;rb.collisionDetectionMode=CollisionDetectionMode2D.Continuous;rb.interpolation=RigidbodyInterpolation2D.Interpolate;
            var body=go.AddComponent<CapsuleCollider2D>();body.offset=new Vector2(0,1.25f);body.size=new Vector2(1.65f,2.5f);
            const string materialPath="Assets/Resources/Campaign/Movimiento.physicsMaterial2D";
            var movement=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(materialPath);
            if(movement==null){movement=new PhysicsMaterial2D("Movimiento sin adherencia"){friction=0,bounciness=0};AssetDatabase.CreateAsset(movement,materialPath);}
            body.sharedMaterial=movement;
            var player=go.AddComponent<PlayerController>();player.layerSuelo=1<<3;player.fuerzaSalto=7.2f;player.longitudRaycast=.2f;
            var driver=go.AddComponent<PlayerArtDriver>();driver.Install(art);driver.actor.transform.localPosition=new Vector3(0,1.25f,0);
            go.AddComponent<SierraSword>();
        }
        private static SpriteRenderer Picture(string name,Sprite sprite,Vector3 position,Vector2 size,int order,Transform parent=null)
        {
            var go=new GameObject(name);go.transform.SetParent(parent??environment,false);go.transform.localPosition=position;
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingOrder=order;
            go.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);return sr;
        }
        private static void Platform(float left,float right,float top,bool ledge=false)
        {
            var go=new GameObject(ledge?"Cornisa":"Suelo de quebrada");go.transform.SetParent(environment,false);go.layer=3;
            float depth=ledge?.42f:5;go.transform.position=new Vector3((left+right)*.5f,top-depth*.5f,0);
            var col=go.AddComponent<BoxCollider2D>();col.size=new Vector2(right-left,depth);
            if(!ledge)
            {
                var backing=Picture("Roca continua",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Campaign/Sprites/TerrainBacking.asset"),new Vector3((left+right)*.5f,top-2.65f,0),new Vector2(right-left,5),-2);
                backing.color=new Color(.28f,.29f,.27f);
                for(int row=0;row<3;row++)for(float x=left;x<right;x+=3.6f)
                    Picture("Estrato de pizarra",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Campaign/Sprites/SierraRockFill.asset"),new Vector3(x+Mathf.Min(3.6f,right-x)*.5f,top-1.3f-row*1.5f,0),new Vector2(Mathf.Min(3.6f,right-x)+.03f,1.6f),-1);
            }
            for(float x=left;x<right;x+=3.5f)
                Picture("Borde de ichu",art.terrain[0],new Vector3(x+Mathf.Min(3.5f,right-x)*.5f,top-.42f,0),new Vector2(Mathf.Min(3.5f,right-x)+.04f,ledge?.85f:1.05f),1);
        }
        private static GameObject Gate(string name,float x,float height,bool visible)
        {
            var go=new GameObject(name);go.transform.SetParent(environment,false);go.transform.position=new Vector3(x,0,0);go.layer=3;
            var c=go.AddComponent<BoxCollider2D>();c.offset=Vector2.up*(height*.5f);c.size=new Vector2(.5f,height);
            if(visible)Picture("Tranca",art.terrain[1],Vector3.up*(height*.5f),new Vector2(.6f,height),12,go.transform);
            return go;
        }
        private static SierraProp Prop(string name,SierraPropKind kind,string id,string icon,float x,float y,float width,float height,bool solid=false)
        {
            var go=new GameObject(name);go.transform.SetParent(environment,false);go.transform.position=new Vector3(x,y,0);
            var prop=go.AddComponent<SierraProp>();prop.kind=kind;prop.persistentId=id;props.Add(prop);
            if(kind==SierraPropKind.Barrier)
                for(int i=0;i<3;i++)Picture("Cajón de corte",art.Icon(icon),Vector3.up*(.52f+i),new Vector2(1.4f,1.05f),12,go.transform);
            else Picture("Arte",art.Icon(icon),Vector3.up*(height*.5f),new Vector2(width,height),kind==SierraPropKind.Cage?15:12,go.transform);
            if(solid){go.layer=3;var c=go.AddComponent<BoxCollider2D>();c.offset=Vector2.up*(height*.5f);c.size=new Vector2(width,height);}
            return prop;
        }
        private static AnimatedActor Actor(string name,RuntimeAnimatorController controller,Sprite sprite,Vector3 position,float scale,Transform parent=null)
        {
            var actor=AnimatedActor.Create(name,parent??environment,controller,sprite,scale);actor.transform.localPosition=position;return actor;
        }
        private static void Enemy(string id,float x,float left,float right,bool flying)
        {
            var go=new GameObject(id);go.transform.SetParent(environment,false);go.transform.position=new Vector3(x,flying?1.1f:0,0);
            var enemy=go.AddComponent<SierraEnemy>();enemy.persistentId=id;enemy.flying=flying;enemy.left=left;enemy.right=right;
            int index=flying?1:0;enemy.actor=Actor(id+" · arte",art.enemyControllers[index],art.enemies[index],Vector3.zero,.85f,go.transform);enemy.actor.sprite.sharedMaterial=art.enemyMaterial;
            var c=go.AddComponent<BoxCollider2D>();c.isTrigger=true;c.offset=new Vector2(0,.65f);c.size=new Vector2(1.25f,1.5f);
        }
        private static void Pickup(string id,string item,int amount,float x,float y)
        {
            var go=new GameObject(id);go.transform.SetParent(environment,false);go.transform.position=new Vector3(x,y,0);
            var p=go.AddComponent<CampaignPickup>();p.persistentId=id;p.item=item;p.amount=amount;
            var c=go.AddComponent<CircleCollider2D>();c.isTrigger=true;c.radius=.42f;
            if(item=="piece")
            {
                var icon=new GameObject("Pieza de corazón",typeof(RectTransform),typeof(Canvas));icon.transform.SetParent(go.transform,false);icon.transform.localScale=Vector3.one*.012f;
                icon.GetComponent<RectTransform>().sizeDelta=new Vector2(48,44);var canvas=icon.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.sortingOrder=25;
                var heart=icon.AddComponent<HeartGraphic>();heart.quarters=1;heart.color=new Color(1,.3f,.5f);heart.raycastTarget=false;
            }
            else Picture("Objeto",art.Icon(item),Vector3.zero,new Vector2(.65f,.65f),24,go.transform).gameObject.AddComponent<FloatingArtwork>();
        }
    }
}
