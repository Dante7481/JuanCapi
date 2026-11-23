using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace JuanCapi.Editor
{
    public static class MineBuilder
    {
        private static CampaignArt art;private static Transform root;private static readonly List<MineProp> props=new List<MineProp>();
        [MenuItem("Juan Capi/Nivel 3/Construir MinerTown")]
        public static void Build()
        {
            art=CampaignArt.Current;props.Clear();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            root=new GameObject("MinerTown · cantera industrial").transform;var level=root.gameObject.AddComponent<MineLevel>();
            var cam=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener),typeof(MineCamera));cam.tag="MainCamera";cam.transform.position=new Vector3(10,3.1f,-10);
            var camera=cam.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=5.6f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.63f,.54f,.43f);level.view=cam.GetComponent<MineCamera>();
            Picture("Cantera a cielo abierto",art.mine,new Vector3(27,3.6f,5),new Vector2(66,22),-100).gameObject.AddComponent<ParallaxArt>().factor=.82f;
            Ground(-3,25,0);Ground(32.6f,46,0);Ground(46,72,-7);Ground(72,165,0);
            Platform(42,45,1.2f);Platform(100,105,.8f);Platform(105,110,1.6f);Platform(110,116,2.4f);Platform(116,121,1.6f);Platform(121,125,2.4f);
            Platform(133.5f,136,.65f);Platform(149,151.5f,.65f);
            Wall("Límite oeste",-3.5f,0,30);Wall("Límite este",165.5f,0,30);
            Prop("Banco del acceso",MinePropKind.Bench,"MARA_SERVICE","bench",5,.6f,1.8f,1.1f);
            Prop("Provisiones de servicio",MinePropKind.Supply,"M01_SUPPLY","crate",7.7f,.55f,1.2f,1.1f);
            var receiver=Prop("Receptor del acceso",MinePropKind.Receiver,"M01_REFLECT","cell",14,1.1f,1.1f,1.3f,true);Emitter(receiver,"M01_REFLECT");
            level.entryGate=Wall("Compuerta de acceso",18,0,4.5f);
            Picture("Descarga · tubería inicial",art.terrain[10],new Vector3(2,1.2f),new Vector2(2.2f,2.4f),2);
            for(int i=0;i<5;i++){Picture("Rodillo de cinta",art.terrain[8],new Vector3(33.7f+i*1.4f,-.15f),new Vector2(1.45f,.42f),7);}
            Enemy("Guardia del depósito",36,0,false);
            Press(41,level);
            Pickup("SEC_M01_GOLD","gold",20,43.5f,1.95f);Pickup("SEC_M01_SCRAP","scrap",5,44.3f,1.95f);Pickup("SEC_M01_PIECE","piece",1,42.5f,1.95f);
            Pickup("M02_FRUIT","fruit",1,45,.7f);
            // Industrial duct uses metal and pipework; the panorama remains an exposed quarry.
            Picture("Conducto · agua",Backing,new Vector3(59,-3.2f),new Vector2(26,7.8f),-30).color=new Color(.05f,.27f,.3f,.95f);
            for(int i=0;i<9;i++)Picture("Conducto · panel del fondo",art.terrain[9],new Vector3(47.5f+i*2.9f,-4),new Vector2(3,6),-20).color=new Color(.32f,.65f,.68f,.4f);
            Block("Conducto · techo",new Vector2(58.5f,.7f),new Vector2(19,4.6f),9);
            for(int i=0;i<4;i++)Picture("Conducto · tubería",art.terrain[10],new Vector3(50+i*5,-5.4f),new Vector2(2,2),-10);
            Picture("Superficie del agua",Backing,new Vector3(59,.7f),new Vector2(26,.05f),4).color=new Color(.3f,.9f,.95f,.7f);
            var fish=new GameObject("Dron de conducto");fish.transform.SetParent(root,false);fish.transform.position=new Vector3(54,-4.8f);var enemy=fish.AddComponent<SwampEnemy>();enemy.aquatic=true;
            enemy.actor=Actor("Dron · arte",art.enemyControllers[4],art.enemies[4],Vector3.zero,.8f,fish.transform);enemy.actor.sprite.sharedMaterial=art.enemyMaterial;
            var hit=fish.AddComponent<BoxCollider2D>();hit.isTrigger=true;hit.size=new Vector2(1.5f,.8f);
            var wetReceiver=Prop("Emisor del conducto",MinePropKind.Receiver,"M03_REFLECT","cell",64,-4.5f,1,1.1f,true);Emitter(wetReceiver,"M03_REFLECT");
            Prop("Cierre diagonal del conducto",MinePropKind.Target,"M03_TARGET","valve",67,-5.4f,.75f,.85f,true);
            level.waterGate=Wall("Compuerta del conducto",69,-7,10);
            Pickup("SEC_M02_GOLD","gold",25,58,-5.4f);Pickup("SEC_M02_SCRAP","scrap",5,59,-5.4f);Pickup("REC_M01","record",1,60,-5.4f);
            Pickup("M03_CELL","cell",1,74,.7f);
            var cable=Prop("Cable de la sala de válvulas",MinePropKind.Cable,"M04_CABLE","sword",79.5f,1.3f,.6f,2.6f,true);
            cable.GetComponentInChildren<SpriteRenderer>().enabled=false;
            for(int i=0;i<3;i++)Picture("Cable · abrazadera",art.terrain[10],new Vector3(0,-.9f+i*.9f),new Vector2(.6f,.92f),11,cable.transform);
            var lumaReceiver=Prop("Receptor de la celda",MinePropKind.Receiver,"M04_REFLECT","cell",82.2f,2.8f,1.1f,1.3f,true);Emitter(lumaReceiver,"M04_REFLECT");
            level.lumaGate=Wall("Puerta de Luma",84,0,4.5f);level.controlGate=Wall("Acceso al control",94,0,4.5f);
            level.luma=Actor("Luma",art.allyControllers[2],art.allies[2],new Vector3(87,0),1);
            var rescue=Prop("Luma · rescate",MinePropKind.Luma,"LUMA","key",87,1,1,1);rescue.GetComponentInChildren<SpriteRenderer>().enabled=false;
            Prop("Banco de Luma",MinePropKind.Bench,"LUMA","bench",91,.6f,1.8f,1.1f);
            Picture("Sala de válvulas · cubierta",art.terrain[8],new Vector3(87.5f,4.2f),new Vector2(9,.7f),-3);
            Picture("Sala de válvulas · consola",art.terrain[10],new Vector3(90,2),new Vector2(1.8f,2),-4);
            Pickup("REC_M02","record",1,88.5f,.7f);
            var supply=Prop("Provisiones del ascenso",MinePropKind.Supply,"M04_SUPPLY","crate",96,.6f,1.3f,1.2f);supply.scrap=8;
            Enemy("Guardia del ascenso",108,1.6f,false);Enemy("Dron del control",119,3.5f,true);
            Pickup("M05_FRUIT","fruit",1,126,.7f);Pickup("SEC_M03_GOLD","gold",25,122.5f,3.1f);Pickup("SEC_M03_SCRAP","scrap",5,123.5f,3.1f);
            var bossGO=new GameObject("Director · exotraje");bossGO.transform.SetParent(root,false);bossGO.transform.position=new Vector3(144,0);
            var boss=bossGO.AddComponent<DirectorBoss>();level.boss=boss;boss.actor=Actor("Director",art.director,art.directorPortrait,Vector3.zero,1,bossGO.transform);
            var bossHit=bossGO.AddComponent<BoxCollider2D>();bossHit.isTrigger=true;bossHit.offset=new Vector2(0,1.7f);bossHit.size=new Vector2(2.7f,3.5f);
            boss.entrance=Wall("Arena · entrada",129,0,5.5f);boss.exit=Wall("Arena · salida",155,0,5.5f);boss.entrance.SetActive(false);boss.exit.SetActive(false);
            var valve=Prop("Válvula principal",MinePropKind.FinalValve,"M06_VALVE","valve",161,1.05f,1.7f,2.1f);level.valve=valve.GetComponentInChildren<SpriteRenderer>().transform;
            var lights=new List<SpriteRenderer>();foreach(float x in new[]{18f,41,72,94,128,156})lights.Add(Picture("Baliza de descarga",art.terrain[11],new Vector3(x,3),new Vector2(.65f,1.5f),5));level.dischargeLights=lights.ToArray();
            Epilogue(level);CreatePlayer();level.props=props.ToArray();
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/MinerTown.unity");
            EditorBuildSettings.scenes=new[]{"Menu","LV1","AmazoniaTerrestre","AmazoniaAcuatica","MinerTown","Art/Sierra","Art/Pantano","Art/PantanoAcuatico","Art/MinerTown"}.Select(n=>new EditorBuildSettingsScene("Assets/Scenes/"+n+".unity",true)).ToArray();AssetDatabase.SaveAssets();
        }
        private static Sprite Backing=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Campaign/Sprites/TerrainBacking.asset");
        private static SpriteRenderer Picture(string name,Sprite sprite,Vector3 position,Vector2 size,int order,Transform parent=null)
        {var go=new GameObject(name);go.transform.SetParent(parent??root,false);go.transform.localPosition=position;var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=order;go.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);return r;}
        private static void Ground(float left,float right,float top)
        {
            var go=new GameObject("Suelo de cantera");go.transform.SetParent(root,false);go.transform.position=new Vector3((left+right)*.5f,top-2.5f);go.layer=3;go.AddComponent<BoxCollider2D>().size=new Vector2(right-left,5);
            Picture("Relleno de cantera",Backing,new Vector3((left+right)*.5f,top-2.5f),new Vector2(right-left,5),-3).color=new Color(.34f,.27f,.2f);
            for(float x=left;x<right;x+=3){float w=Mathf.Min(3,right-x);Picture("Muro de contención",art.terrain[9],new Vector3(x+w*.5f,top-1.5f),new Vector2(w+.025f,3.1f),-1);Picture("Pasarela",art.terrain[8],new Vector3(x+w*.5f,top-.22f),new Vector2(w+.03f,.55f),2);}
        }
        private static void Platform(float left,float right,float top){var go=Block("Pasarela elevada",new Vector2((left+right)*.5f,top-.2f),new Vector2(right-left,.4f),8);go.GetComponent<BoxCollider2D>().usedByEffector=true;var e=go.AddComponent<PlatformEffector2D>();e.useOneWay=true;e.useOneWayGrouping=true;e.surfaceArc=165;}
        private static GameObject Block(string name,Vector2 position,Vector2 size,int tile)
        {
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=position;go.layer=3;go.AddComponent<BoxCollider2D>().size=size;
            float section=tile==9?3:5;
            for(float x=0;x<size.x;x+=section)
            {float width=Mathf.Min(section,size.x-x);Picture("Material",art.terrain[tile],new Vector3(x+width*.5f-size.x*.5f,0),new Vector2(width+.015f,size.y),3,go.transform);}
            return go;
        }
        private static GameObject Wall(string name,float x,float bottom,float height)
        {
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=new Vector3(x,bottom);go.layer=3;var c=go.AddComponent<BoxCollider2D>();c.offset=Vector2.up*height*.5f;c.size=new Vector2(.55f,height);
            for(float y=0;y<height;y+=1.5f){float h=Mathf.Min(1.5f,height-y);Picture("Tramo de metal",art.terrain[9],new Vector3(0,y+h*.5f),new Vector2(.6f,h+.02f),8,go.transform);}return go;
        }
        private static AnimatedActor Actor(string name,RuntimeAnimatorController controller,Sprite sprite,Vector3 position,float scale,Transform parent=null)
        {var a=AnimatedActor.Create(name,parent??root,controller,sprite,scale);a.transform.localPosition=position;return a;}
        private static MineProp Prop(string name,MinePropKind kind,string id,string icon,float x,float y,float width,float height,bool hitbox=false)
        {
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=new Vector3(x,y);var p=go.AddComponent<MineProp>();p.kind=kind;p.persistentId=id;p.icon=icon;props.Add(p);
            Picture("Arte",art.Icon(icon),Vector3.zero,new Vector2(width,height),12,go.transform);
            if(hitbox){var c=go.AddComponent<BoxCollider2D>();c.size=new Vector2(width,height);c.isTrigger=kind!=MinePropKind.Cable;if(!c.isTrigger)go.layer=3;}return p;
        }
        private static void Emitter(MineProp prop,string stop){var e=prop.gameObject.AddComponent<MineEmitter>();e.stopFlag=stop;}
        private static void Enemy(string name,float x,float y,bool drone)
        {
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=new Vector3(x,y);var e=go.AddComponent<MineEnemy>();e.drone=drone;e.health=drone?3:9;
            int index=drone?1:5;e.actor=Actor(name+" · arte",art.enemyControllers[index],art.enemies[index],Vector3.zero,drone?.8f:1,go.transform);e.actor.sprite.sharedMaterial=art.enemyMaterial;
            var c=go.AddComponent<BoxCollider2D>();c.isTrigger=true;c.offset=drone?Vector2.zero:Vector2.up*.8f;c.size=drone?new Vector2(1.3f,1):new Vector2(1.35f,1.8f);
        }
        private static void Press(float x,MineLevel level)
        {
            var go=new GameObject("Prensa de mineral");go.transform.SetParent(root,false);go.transform.position=new Vector3(x,0);var press=go.AddComponent<MinePress>();press.safePosition=new Vector3(x-3,.08f);
            press.head=Picture("Pistón",art.terrain[9],Vector3.up*4,new Vector2(2.1f,1),15,go.transform).transform;
            press.lamp=Picture("Aviso de prensa",art.terrain[11],new Vector3(-1.6f,2.8f),new Vector2(.5f,1.2f),15,go.transform);
            Picture("Soporte de prensa",art.terrain[10],new Vector3(0,5),new Vector2(2.2f,2.1f),10,go.transform);
        }
        private static void Pickup(string id,string item,int amount,float x,float y)
        {
            var go=new GameObject(id);go.transform.SetParent(root,false);go.transform.position=new Vector3(x,y);var p=go.AddComponent<CampaignPickup>();p.persistentId=id;p.item=item;p.amount=amount;var c=go.AddComponent<CircleCollider2D>();c.isTrigger=true;c.radius=.42f;
            if(item=="piece")
            {var icon=new GameObject("Pieza de corazón",typeof(RectTransform),typeof(Canvas));icon.transform.SetParent(go.transform,false);icon.transform.localScale=Vector3.one*.012f;icon.GetComponent<RectTransform>().sizeDelta=new Vector2(48,44);var canvas=icon.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.sortingOrder=25;var heart=icon.AddComponent<HeartGraphic>();heart.quarters=1;heart.color=new Color(1,.3f,.5f);heart.raycastTarget=false;}
            else Picture("Objeto",art.Icon(item),Vector3.zero,new Vector2(.65f,.65f),24,go.transform).gameObject.AddComponent<FloatingArtwork>();
        }
        private static void CreatePlayer()
        {
            var go=new GameObject("Juan",typeof(Rigidbody2D),typeof(CapsuleCollider2D));go.tag="Player";go.transform.position=new Vector3(3,.08f);go.transform.localScale=new Vector3(.625f,.625f,1);
            var rb=go.GetComponent<Rigidbody2D>();rb.gravityScale=1.7f;rb.freezeRotation=true;rb.collisionDetectionMode=CollisionDetectionMode2D.Continuous;rb.interpolation=RigidbodyInterpolation2D.Interpolate;
            var c=go.GetComponent<CapsuleCollider2D>();c.offset=new Vector2(0,1.25f);c.size=new Vector2(1.65f,2.5f);c.sharedMaterial=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Resources/Campaign/Movimiento.physicsMaterial2D");
            var p=go.AddComponent<PlayerController>();p.layerSuelo=1<<3;p.fuerzaSalto=7.2f;p.longitudRaycast=.2f;var driver=go.AddComponent<PlayerArtDriver>();driver.Install(art);go.AddComponent<SierraSword>();go.AddComponent<WeaponCombat>();
        }
        private static void Epilogue(MineLevel level)
        {
            var go=new GameObject("Epílogo · recuperación de la cuenca");go.transform.SetParent(root,false);go.transform.position=new Vector3(210,0);level.epilogueRoot=go;
            Picture("Cuenca · semanas después",art.sierra,new Vector3(0,2.2f,4),new Vector2(45,15),-100,go.transform);
            for(int i=0;i<8;i++)Picture("Orilla en recuperación",art.terrain[0],new Vector3(-12+i*3.5f,0),new Vector2(3.55f,1),0,go.transform);
            Actor("Juan · epílogo",art.playerLand,art.playerPortrait,new Vector3(-5,.78f),.84f,go.transform);
            for(int i=0;i<3;i++)Actor(new[]{"Mara · epílogo","Tico · epílogo","Luma · epílogo"}[i],art.allyControllers[i],art.allies[i],new Vector3(-1+i*3,0),1,go.transform);
            Picture("Residuos recogidos",art.Icon("crate"),new Vector3(7,.55f),new Vector2(1.6f,1.1f),12,go.transform);
            Picture("Taller de recuperación",art.Icon("bench"),new Vector3(-8,.6f),new Vector2(2,1.2f),12,go.transform);go.SetActive(false);
        }
    }
}
