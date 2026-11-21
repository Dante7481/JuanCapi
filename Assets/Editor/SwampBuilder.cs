using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JuanCapi.Editor
{
    public static class SwampBuilder
    {
        private static CampaignArt art;
        private static Transform root;
        private static readonly List<SwampProp> props=new List<SwampProp>();
        [MenuItem("Juan Capi/Nivel 2/Construir pantano")]
        public static void Build()
        {
            art=CampaignArt.Current;BuildScene(false);BuildScene(true);
            EditorBuildSettings.scenes=new[]{"Menu","LV1","AmazoniaTerrestre","AmazoniaAcuatica","Art/Sierra","Art/Pantano","Art/PantanoAcuatico","Art/MinerTown"}.Select(n=>new EditorBuildSettingsScene("Assets/Scenes/"+n+".unity",true)).ToArray();
            AssetDatabase.SaveAssets();
        }
        private static void BuildScene(bool water)
        {
            props.Clear();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            root=new GameObject(water?"Pantano · aguas profundas":"Pantano · orilla de los vertidos").transform;
            var level=root.gameObject.AddComponent<SwampLevel>();level.aquatic=water;
            var cameraGO=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener),typeof(SwampCamera));cameraGO.tag="MainCamera";
            var camera=cameraGO.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=water?6:5.6f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=water?new Color(.04f,.18f,.22f):new Color(.22f,.31f,.24f);
            cameraGO.transform.position=new Vector3(10,water?9:3.1f,-10);cameraGO.GetComponent<SwampCamera>().aquatic=water;
            var original=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Campaign/Imported/"+(water?"AmazoniaAcuatica":"AmazoniaTerrestre")+"Original.prefab"));
            PrefabUtility.UnpackPrefabInstance(original,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);original.transform.SetParent(root,false);
            foreach(var t in original.GetComponentsInChildren<Transform>(true))if(t.gameObject.layer==8)t.gameObject.layer=3;
            foreach(var sr in original.GetComponentsInChildren<SpriteRenderer>(true))sr.sortingLayerID=0;
            var bg=original.transform.Find("Panorama original");bg.gameObject.AddComponent<ParallaxArt>().factor=water?.68f:.75f;
            // Reuse the source composition and platform art, adapting its reach to Juan's movement.
            foreach(var t in original.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Límite")||t.name=="Techo").ToArray())Object.DestroyImmediate(t.gameObject);
            if(!water)
            {
                Object.DestroyImmediate(original.transform.Find("Suelo").gameObject);
                Relocate(original,"Tronco inicial",8,.65f);Relocate(original,"Raíz elevada",12,1.5f);
                Relocate(original,"Plataforma de combate A",86,.8f);Relocate(original,"Plataforma de combate B",99,.8f);Relocate(original,"Muelle",122,-.15f);
                Ground(0,21,0,false);Ground(28.6f,47,0,false);Ground(54.6f,128,0,false);
                Block("Cobertura de raíz",new Vector2(62,.55f),new Vector2(2,1.1f),true,6);
                Prop("Mara · banco portátil",SwampPropKind.Bench,"MARA","bench",8,.5f,1.8f,1.1f);
                Actor("Mara",art.allyControllers[0],art.allies[0],new Vector3(6,0),1);
                Prop("Suministros de la orilla",SwampPropKind.Supply,"P01_SUPPLY","crate",16,.5f,1.2f,1);
                var cable=Prop("Cable de retención",SwampPropKind.Cable,"P03_CABLE","sword",75,1.7f,.6f,3.4f,true);
                cable.GetComponentInChildren<SpriteRenderer>().sprite=art.terrain[6];
                Enemy("Tirador de los vertidos",66,false,false);Enemy("Vigía de la orilla",38,false,false);
                Pickup("SEC_P01_GOLD","gold",20,12,2.5f);Pickup("SEC_P01_SCRAP","scrap",5,13,2.5f);
                Pickup("P02_FRUIT","fruit",1,57,.7f);Pickup("REC_P01","record",1,77,.7f);
                Boss(level,false,new Vector2(96,0));
                Prop("Condensador · disparador",SwampPropKind.Gun,"P05_GUN","gun",105,.85f,1.4f,1.2f);
                Prop("Banco de adaptación",SwampPropKind.Bench,"MARA","bench",108,.6f,1.8f,1.2f);
                Prop("Blanco alto",SwampPropKind.Target,"P05_TARGET_1","cell",114.5f,3.5f,.8f,.8f,true);
                Prop("Blanco bajo",SwampPropKind.Target,"P05_TARGET_2","cell",119,1.2f,.8f,.8f,true);
                Prop("Blanco diagonal",SwampPropKind.Target,"P05_TARGET_3","cell",111.8f,5,.8f,.8f,true);
                Prop("Bajada al pantano",SwampPropKind.Dive,"P06_DIVE","valve",125,.8f,1.4f,1.6f);
                Picture("Manglar",art.terrain[7],new Vector3(59,.7f),new Vector2(3,1.8f),2);
            }
            else
            {
                Ground(66,110,0,true);
                // Native submerged ruins remain in their original positions.
                Enemy("Vigía sumergido",19,true,false);Enemy("Anguila mecánica",51,true,true);
                AirBell(level);
                Prop("Cierre izquierdo",SwampPropKind.Latch,"A03_LATCH_1","cell",31,13.6f,.7f,.7f,true);
                Prop("Cierre derecho",SwampPropKind.Latch,"A03_LATCH_2","cell",37,16.2f,.7f,.7f,true);
                Prop("Tico · válvula exterior",SwampPropKind.Tico,"TICO","valve",34,12.7f,1,1);
                Prop("Suministros sumergidos",SwampPropKind.Supply,"A02_SUPPLY","crate",29,8,1.2f,1);
                Prop("Banco sellado",SwampPropKind.Bench,"MARA","bench",40,13,1.7f,1.1f);
                Picture("Banco · ménsula de ruina",art.terrain[5],new Vector3(40,11.8f),new Vector2(2.4f,1.4f),4);
                level.rescueGate=Block("Compuerta de Tico",new Vector2(62,10),new Vector2(.65f,20),true,6);
                Pickup("SEC_P02_GOLD","gold",20,23,15.2f);Pickup("SEC_P02_PIECE","piece",1,24.3f,15.2f);
                Pickup("SEC_P03_GOLD","gold",20,57,3);Pickup("SEC_P03_SCRAP","scrap",5,58,3);
                Pickup("REC_A01","record",1,38,13.2f);Pickup("A03_FRUIT","fruit",1,60,9);
                Block("Ruina del guardián · baja",new Vector2(70,3.8f),new Vector2(2.2f,1.4f),true,5);
                Block("Ruina del guardián · alta",new Vector2(77,13),new Vector2(2.5f,1.1f),true,5);
                Block("Ruina del guardián · salida",new Vector2(86,4.5f),new Vector2(2.1f,1.3f),true,5);
                Boss(level,true,new Vector2(81,9));
                Prop("Abrazadera · escudo",SwampPropKind.Shield,"A05_SHIELD","shield",93,8,1.2f,1.4f);
                var receiver=Prop("Emisor de prueba",SwampPropKind.Receiver,"A06_REFLECTED","cell",103,9,1.2f,1.4f,true);receiver.gameObject.AddComponent<ShieldPracticeEmitter>();
                Prop("Conducto hacia MinerTown",SwampPropKind.Exit,"A06_EXIT","valve",107,9,1.4f,2);
                for(int i=0;i<5;i++)Picture("Corriente del canal",art.terrain[7],new Vector3(17+i*1.8f,1.5f),new Vector2(1.4f,1.1f),0);
            }
            Block("Límite oeste",new Vector2(-.5f,10),new Vector2(1,30),false,0);
            Block("Límite este",new Vector2(water?110.5f:128.5f,10),new Vector2(1,30),false,0);
            if(water)Block("Superficie",new Vector2(55,20.5f),new Vector2(112,1),false,0);
            CreatePlayer(water);level.props=props.ToArray();
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/"+(water?"AmazoniaAcuatica":"AmazoniaTerrestre")+".unity");
        }
        private static void Relocate(GameObject original,string name,float x,float y)
        {var t=original.transform.Find(name);t.position=new Vector3(x,y,0);}
        private static void CreatePlayer(bool water)
        {
            var go=new GameObject("Juan",typeof(Rigidbody2D),typeof(CapsuleCollider2D));go.tag="Player";go.transform.position=new Vector3(3,water?9:.08f);go.transform.localScale=new Vector3(.625f,.625f,1);
            var rb=go.GetComponent<Rigidbody2D>();rb.gravityScale=water?0:1.7f;rb.freezeRotation=true;rb.collisionDetectionMode=CollisionDetectionMode2D.Continuous;rb.interpolation=RigidbodyInterpolation2D.Interpolate;
            var c=go.GetComponent<CapsuleCollider2D>();c.offset=new Vector2(0,1.25f);c.size=water?new Vector2(2.7f,1.55f):new Vector2(1.65f,2.5f);c.direction=water?CapsuleDirection2D.Horizontal:CapsuleDirection2D.Vertical;c.sharedMaterial=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Resources/Campaign/Movimiento.physicsMaterial2D");
            var p=go.AddComponent<PlayerController>();p.aquatic=water;p.layerSuelo=1<<3;p.fuerzaSalto=7.2f;p.longitudRaycast=.2f;
            var driver=go.AddComponent<PlayerArtDriver>();driver.Install(art);driver.actor.transform.localPosition=new Vector3(0,1.25f,0);
            go.AddComponent<SierraSword>();go.AddComponent<WeaponCombat>();
        }
        private static SpriteRenderer Picture(string name,Sprite sprite,Vector3 position,Vector2 size,int order,Transform parent=null)
        {
            var go=new GameObject(name);go.transform.SetParent(parent??root,false);go.transform.localPosition=position;
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingOrder=order;go.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);return sr;
        }
        private static GameObject Block(string name,Vector2 position,Vector2 size,bool visible,int tile)
        {
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=position;go.layer=3;
            go.AddComponent<BoxCollider2D>().size=size;
            if(visible)
            {
                if(size.y>3&&size.x<1)
                    for(float y=-size.y*.5f;y<size.y*.5f;y+=1.5f)
                    {float height=Mathf.Min(1.5f,size.y*.5f-y);Picture("Tramo de compuerta",art.terrain[tile],new Vector3(0,y+height*.5f),new Vector2(size.x,height+.02f),5,go.transform);}
                else Picture("Material",art.terrain[tile],Vector3.zero,size,5,go.transform);
            }
            return go;
        }
        private static void Ground(float left,float right,float top,bool water)
        {
            Block("Fondo transitable",new Vector2((left+right)*.5f,top-2.5f),new Vector2(right-left,5),false,0);
            Picture("Tierra continua",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Campaign/Sprites/TerrainBacking.asset"),new Vector3((left+right)*.5f,top-2.6f),new Vector2(right-left,5),-2).color=new Color(.18f,.25f,.18f);
            for(float x=left;x<right;x+=3.5f)Picture("Borde de musgo",art.terrain[water?5:4],new Vector3(x+Mathf.Min(3.5f,right-x)*.5f,top-.4f),new Vector2(Mathf.Min(3.5f,right-x)+.03f,.95f),1);
        }
        private static SwampProp Prop(string name,SwampPropKind kind,string id,string icon,float x,float y,float width,float height,bool hitbox=false)
        {
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=new Vector3(x,y);
            var p=go.AddComponent<SwampProp>();p.kind=kind;p.persistentId=id;p.icon=icon;props.Add(p);
            Picture("Arte",art.Icon(icon),Vector3.zero,new Vector2(width,height),12,go.transform);
            if(hitbox){var c=go.AddComponent<BoxCollider2D>();c.size=new Vector2(width,height);c.isTrigger=kind!=SwampPropKind.Cable;if(!c.isTrigger)go.layer=3;}
            return p;
        }
        private static AnimatedActor Actor(string name,RuntimeAnimatorController controller,Sprite sprite,Vector3 p,float scale,Transform parent=null)
        {var a=AnimatedActor.Create(name,parent??root,controller,sprite,scale);a.transform.localPosition=p;return a;}
        private static void Enemy(string name,float x,bool water,bool charger)
        {
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=new Vector3(x,water?8:0);
            var e=go.AddComponent<SwampEnemy>();e.aquatic=water;e.charger=charger;int index=water?charger?4:3:2;
            e.actor=Actor(name+" · arte",art.enemyControllers[index],art.enemies[index],Vector3.zero,.9f,go.transform);e.actor.sprite.sharedMaterial=art.enemyMaterial;
            var c=go.AddComponent<BoxCollider2D>();c.isTrigger=true;c.offset=water?Vector2.zero:Vector2.up*.65f;c.size=water?new Vector2(1.8f,1):new Vector2(1.2f,1.4f);
        }
        private static void Boss(SwampLevel level,bool water,Vector2 position)
        {
            var go=new GameObject("Caimán · encuentro");go.transform.SetParent(root,false);go.transform.position=position;
            var b=go.AddComponent<CaimanEncounter>();b.aquatic=water;b.health=b.Maximum;level.boss=b;
            b.actor=Actor("Caimán · original",water?art.caimanWater:art.caimanLand,water?AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Animations/V2/CaimanAcuatico_Idle0.asset"):art.caimanPortrait,new Vector3(0,water?0:1.35f),1.5f,go.transform);
            var c=go.AddComponent<BoxCollider2D>();c.isTrigger=true;c.offset=water?Vector2.zero:Vector2.up*1.2f;c.size=water?new Vector2(3.8f,1.8f):new Vector2(2.5f,2.4f);
            b.entrance=Block("Arena · entrada",new Vector2(water?65.5f:81,water?10:2.7f),new Vector2(.5f,water?20:5.4f),true,6);
            b.exit=Block("Arena · salida",new Vector2(water?91:104,water?10:2.7f),new Vector2(.5f,water?20:5.4f),true,6);
            b.entrance.SetActive(false);b.exit.SetActive(false);
        }
        private static void Pickup(string id,string item,int amount,float x,float y)
        {
            var go=new GameObject(id);go.transform.SetParent(root,false);go.transform.position=new Vector3(x,y);var p=go.AddComponent<CampaignPickup>();p.persistentId=id;p.item=item;p.amount=amount;go.AddComponent<CircleCollider2D>().isTrigger=true;go.GetComponent<CircleCollider2D>().radius=.42f;
            if(item=="piece")
            {
                var icon=new GameObject("Pieza de corazón",typeof(RectTransform),typeof(Canvas));icon.transform.SetParent(go.transform,false);icon.transform.localScale=Vector3.one*.012f;icon.GetComponent<RectTransform>().sizeDelta=new Vector2(48,44);var canvas=icon.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.sortingOrder=25;var h=icon.AddComponent<HeartGraphic>();h.quarters=1;h.color=new Color(1,.3f,.5f);h.raycastTarget=false;
            }
            else Picture("Objeto",art.Icon(item),Vector3.zero,new Vector2(.65f,.65f),24,go.transform).gameObject.AddComponent<FloatingArtwork>();
        }
        private static void AirBell(SwampLevel level)
        {
            Block("Campana · suelo seco",new Vector2(34,14.5f),new Vector2(5,.5f),true,6);
            // A warm, enclosed dry pocket makes the bird's position readable against the blue water.
            Picture("Campana · aire",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Campaign/Sprites/TerrainBacking.asset"),new Vector3(34,16.4f),new Vector2(5,3.3f),-1).color=new Color(.65f,.7f,.42f,.65f);
            Block("Campana · pared izquierda",new Vector2(31.4f,16.4f),new Vector2(.3f,4),true,6);
            Block("Campana · pared derecha",new Vector2(36.6f,16.4f),new Vector2(.3f,4),true,6);
            Block("Campana · techo",new Vector2(34,18.5f),new Vector2(5.5f,.5f),true,6);
            level.tico=Actor("Tico · refugio de aire",art.allyControllers[1],art.allies[1],new Vector3(34,14.75f),1);
        }
    }
}
