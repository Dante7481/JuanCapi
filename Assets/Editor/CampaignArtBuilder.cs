using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using JuanCapi;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace JuanCapi.Editor
{
    public static class CampaignArtBuilder
    {
        private const string Root="Assets/Art/Campaign/";
        private static CampaignArt art;
        private static readonly List<string> checks=new List<string>();
        private static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Entregas/Fase4"));
        [MenuItem("Juan Capi/Arte/Preparar recursos y escenas")]
        public static void Build()
        {
            Directory.CreateDirectory(Output);
            foreach(string dir in new[]{"Sprites","Animations","Prefabs"})Directory.CreateDirectory(Root+dir);
            Directory.CreateDirectory("Assets/Resources/Campaign");Directory.CreateDirectory("Assets/Scenes/Art");AssetDatabase.Refresh();
            foreach(string path in Directory.GetFiles(Root+"Generated","*.png"))
            {
                var imp=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
                imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.filterMode=FilterMode.Point;
                imp.mipmapEnabled=false;imp.npotScale=TextureImporterNPOTScale.None;imp.maxTextureSize=4096;
                imp.textureCompression=TextureImporterCompression.Uncompressed;imp.alphaIsTransparency=true;imp.isReadable=true;imp.SaveAndReimport();
            }
            art=AssetDatabase.LoadAssetAtPath<CampaignArt>("Assets/Resources/Campaign/Art.asset");
            if(art==null){art=ScriptableObject.CreateInstance<CampaignArt>();AssetDatabase.CreateAsset(art,"Assets/Resources/Campaign/Art.asset");}
            art.sierra=Load<Sprite>(Root+"Generated/SierraPanorama.png");art.mine=Load<Sprite>(Root+"Generated/MinerTownPanorama.png");
            art.swamp=Load<Sprite>("Assets/Art/V2/Sprites/Fondos/FondoTerrestre.png");art.underwater=Load<Sprite>("Assets/Art/V2/Sprites/Fondos/FondoAcuatico.png");
            art.equipment=Slice("Equipment",4,4,true,100);art.terrain=Slice("Terrain",4,3,true,100);
            art.menuHero=Slice("MenuHero",1,1,true,100)[0];
            art.aimBodies=Slice("AimBodies",4,2,true,Load<Texture2D>(Root+"Generated/AimBodies.png").height*.23f);
            var tracker=Slice("Rastreador",4,4,false,Load<Texture2D>(Root+"Generated/Rastreador.png").height/4f/2.7f);
            var director=Slice("Director",4,4,false,Load<Texture2D>(Root+"Generated/Director.png").height/4f/3.5f);
            var allies=Slice("Allies",4,3,false,Load<Texture2D>(Root+"Generated/Allies.png").height/3f/2.1f);
            var enemies=Slice("Enemies",4,6,false,128);
            art.allies=new[]{allies[0],allies[4],allies[8]};art.enemies=Enumerable.Range(0,6).Select(i=>enemies[i*4]).ToArray();
            art.enemyMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"EnemySprites.mat");
            if(art.enemyMaterial==null){art.enemyMaterial=new Material(Shader.Find("JuanCapi/WhiteKeySprite"));AssetDatabase.CreateAsset(art.enemyMaterial,Root+"EnemySprites.mat");}
            art.playerLand=Load<RuntimeAnimatorController>("Assets/Animations/Amazonia/CapybaraTerrestre/Controller.controller");
            art.playerWater=Load<RuntimeAnimatorController>("Assets/Animations/Amazonia/CapybaraAcuatico/Controller.controller");
            art.caimanLand=Load<RuntimeAnimatorController>("Assets/Animations/V2/CaimanTerrestre.controller");art.caimanWater=Load<RuntimeAnimatorController>("Assets/Animations/V2/CaimanAcuatico.controller");
            art.playerPortrait=Load<Sprite>("Assets/Animations/Amazonia/CapybaraTerrestre/Idle_0.asset");
            art.caimanPortrait=Load<Sprite>("Assets/Animations/V2/CaimanTerrestre_Idle0.asset");art.rastreadorPortrait=tracker[0];art.directorPortrait=director[0];
            art.rastreador=BossController("Rastreador",tracker);art.director=BossController("Director",director);
            art.allyControllers=new RuntimeAnimatorController[3];art.enemyControllers=new RuntimeAnimatorController[6];
            for(int i=0;i<3;i++)
            {
                string name=new[]{"Mara","Tico","Luma"}[i];var controller=Controller(name);var poses=allies.Skip(i*4).Take(4).ToArray();
                Clip(controller,name,"Idle",new[]{poses[0],poses[1],poses[0]},2,true);
                Clip(controller,name,"Interact",new[]{poses[2]},4,true);Clip(controller,name,"Celebrate",new[]{poses[2],poses[3]},4,true);
                art.allyControllers[i]=controller;ActorPrefab(name,controller,poses[0]);
            }
            for(int i=0;i<6;i++)
            {
                string name=new[]{"Vigilante","DronAereo","Tirador","Pez","DronAcuatico","GuardiaIndustrial"}[i];var controller=Controller(name);var poses=enemies.Skip(i*4).Take(4).ToArray();
                Clip(controller,name,"Idle",new[]{poses[0]},4,true);Clip(controller,name,"Run",new[]{poses[0],poses[1]},7,true);
                Clip(controller,name,"Telegraph",new[]{poses[2]},4,false);Clip(controller,name,"Attack",new[]{poses[2],poses[3],poses[3],poses[0]},8,false);
                Clip(controller,name,"Hurt",new[]{poses[0]},4,false);Clip(controller,name,"Death",new[]{poses[0]},4,false);
                art.enemyControllers[i]=controller;ActorPrefab(name,controller,poses[0],art.enemyMaterial);
            }
            ActorPrefab("JuanTerrestre",art.playerLand,art.playerPortrait);ActorPrefab("JuanAcuatico",art.playerWater,Load<Sprite>("Assets/Animations/Amazonia/CapybaraAcuatico/Idle_0.asset"));
            ActorPrefab("CaimanTerrestre",art.caimanLand,art.caimanPortrait);ActorPrefab("CaimanAcuatico",art.caimanWater,Load<Sprite>("Assets/Animations/V2/CaimanAcuatico_Idle0.asset"));
            ActorPrefab("Rastreador",art.rastreador,tracker[0]);ActorPrefab("Director",art.director,director[0]);
            for(int i=0;i<art.equipment.Length;i++)SpritePrefab("Objeto"+i,art.equipment[i]);
            for(int i=0;i<art.terrain.Length;i++)SpritePrefab("Terreno"+i,art.terrain[i]);
            var shield=PlasmaShieldArtwork.Create(null,art.Icon("shield"));
            string shieldMaterialPath=Root+"PlasmaLines.mat";var shieldMaterial=AssetDatabase.LoadAssetAtPath<Material>(shieldMaterialPath);
            if(shieldMaterial==null){shieldMaterial=shield.rim.sharedMaterial;AssetDatabase.CreateAsset(shieldMaterial,shieldMaterialPath);}else{UnityEngine.Object.DestroyImmediate(shield.rim.sharedMaterial);shield.rim.sharedMaterial=shieldMaterial;}
            PrefabUtility.SaveAsPrefabAsset(shield.gameObject,Root+"Prefabs/EscudoPlasma.prefab");UnityEngine.Object.DestroyImmediate(shield.gameObject);
            EditorUtility.SetDirty(art);AssetDatabase.SaveAssets();
            ReskinLevel();
            for(int i=0;i<4;i++)PreviewScene(i);
            EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");
            EditorBuildSettings.scenes=new[]{"Menu","LV1","Art/Sierra","Art/Pantano","Art/PantanoAcuatico","Art/MinerTown"}.Select(n=>new EditorBuildSettingsScene("Assets/Scenes/"+n+".unity",true)).ToArray();
            foreach(string path in Directory.GetFiles(Root+"Generated","*.png"))
            {var imp=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));imp.isReadable=false;imp.SaveAndReimport();}
            AssetDatabase.SaveAssets();File.WriteAllLines(Path.Combine(Output,"preparacion-arte.log"),checks);
            Debug.Log("CAMPAIGN ART READY: "+checks.Count+" checks");
        }
        private static T Load<T>(string path)where T:UnityEngine.Object
        {var value=AssetDatabase.LoadAssetAtPath<T>(path);if(value==null)throw new Exception("Falta recurso: "+path);return value;}
        private static Sprite[] Slice(string name,int columns,int rows,bool trim,float ppu)
        {
            var texture=Load<Texture2D>(Root+"Generated/"+name+".png");var pixels=texture.GetPixels32();var sprites=new Sprite[columns*rows];
            int cw=texture.width/columns,ch=texture.height/rows;
            if(name!="Enemies"&&name!="SierraPanorama"&&name!="MinerTownPanorama")
            {
                int transparent=pixels.Count(p=>p.a<10);if(transparent<pixels.Length*.12f)throw new Exception("Fondo no transparente: "+name);
                checks.Add(name+": transparencia real "+(100f*transparent/pixels.Length).ToString("F1")+" %");
            }
            for(int i=0;i<sprites.Length;i++)
            {
                int sx=(i%columns)*cw,sy=texture.height-(i/columns+1)*ch;
                var rect=new Rect(sx,sy,cw,ch);var pivot=new Vector2(.5f,.04f);
                if(trim)
                {
                    int left=cw,right=0,bottom=ch,top=0;
                    for(int y=0;y<ch;y++)for(int x=0;x<cw;x++)if(pixels[(sy+y)*texture.width+sx+x].a>24)
                    {left=Math.Min(left,x);right=Math.Max(right,x);bottom=Math.Min(bottom,y);top=Math.Max(top,y);}
                    if(right<=left||top<=bottom)throw new Exception("Celda vacía: "+name+" "+i);
                    rect=new Rect(sx+left,sy+bottom,right-left+1,top-bottom+1);pivot=Vector2.one*.5f;
                }
                string path=Root+"Sprites/"+name+"_"+i.ToString("00")+".asset";
                var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if(existing==null){existing=Sprite.Create(texture,rect,pivot,ppu,0,SpriteMeshType.FullRect);existing.name=name+"_"+i.ToString("00");AssetDatabase.CreateAsset(existing,path);}
                sprites[i]=existing;
            }
            checks.Add(name+": "+sprites.Length+" recortes");return sprites;
        }
        private static AnimatorController Controller(string name)
        {string path=Root+"Animations/"+name+".controller";return AssetDatabase.LoadAssetAtPath<AnimatorController>(path)??AnimatorController.CreateAnimatorControllerAtPath(path);}
        private static RuntimeAnimatorController BossController(string name,Sprite[] poses)
        {
            var controller=Controller(name);
            Clip(controller,name,"Idle",poses.Take(4).ToArray(),5,true);Clip(controller,name,"Run",poses.Skip(4).Take(4).ToArray(),8,true);
            Clip(controller,name,"Telegraph",poses.Skip(8).Take(2).ToArray(),4,false);Clip(controller,name,"Attack",poses.Skip(8).Take(4).ToArray(),8,false);
            Clip(controller,name,"Recovery",new[]{poses[11]},4,false);Clip(controller,name,"Hurt",new[]{poses[12]},4,false);
            Clip(controller,name,"Death",poses.Skip(12).Take(4).ToArray(),4,false);return controller;
        }
        private static void Clip(AnimatorController controller,string actor,string state,Sprite[] poses,float fps,bool loop)
        {
            string path=Root+"Animations/"+actor+"_"+state+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(clip==null){clip=new AnimationClip{name=state};AssetDatabase.CreateAsset(clip,path);}
            clip.frameRate=fps;var keys=new ObjectReferenceKeyframe[poses.Length+1];
            for(int i=0;i<poses.Length;i++)keys[i]=new ObjectReferenceKeyframe{time=i/fps,value=poses[i]};
            keys[poses.Length]=new ObjectReferenceKeyframe{time=poses.Length/fps,value=poses[loop?0:poses.Length-1]};
            AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="Visual",type=typeof(SpriteRenderer),propertyName="m_Sprite"},keys);
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;AnimationUtility.SetAnimationClipSettings(clip,settings);
            var machine=controller.layers[0].stateMachine;var node=machine.states.FirstOrDefault(s=>s.state.name==state).state??machine.AddState(state);node.motion=clip;
            if(state=="Idle")machine.defaultState=node;EditorUtility.SetDirty(clip);EditorUtility.SetDirty(controller);
        }
        private static void ActorPrefab(string name,RuntimeAnimatorController controller,Sprite first,Material material=null)
        {
            var actor=AnimatedActor.Create(name,null,controller,first);if(material!=null)actor.sprite.sharedMaterial=material;
            PrefabUtility.SaveAsPrefabAsset(actor.gameObject,Root+"Prefabs/"+name+".prefab");UnityEngine.Object.DestroyImmediate(actor.gameObject);
        }
        private static void SpritePrefab(string name,Sprite sprite)
        {var go=new GameObject(name,typeof(SpriteRenderer));go.GetComponent<SpriteRenderer>().sprite=sprite;PrefabUtility.SaveAsPrefabAsset(go,Root+"Prefabs/"+name+".prefab");UnityEngine.Object.DestroyImmediate(go);}
        private static SpriteRenderer Place(Transform parent,string name,Sprite sprite,Vector3 position,Vector2 size,int order)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=position;
            var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=order;
            go.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);return r;
        }
        private static void ReskinLevel()
        {
            string scenePath="Assets/Scenes/LV1.unity",backup=Path.Combine(Output,"LV1-antes-de-arte.unity");
            if(!File.Exists(backup))File.Copy(scenePath,backup);
            var scene=EditorSceneManager.OpenScene(scenePath);var previous=GameObject.Find("Entorno de sierra");if(previous!=null)UnityEngine.Object.DestroyImmediate(previous);
            var root=new GameObject("Entorno de sierra");
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                if(renderer.name.ToLowerInvariant().Contains("fondo"))renderer.enabled=false;
            var camera=Camera.main;var player=UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if(camera==null||player==null)throw new Exception("LV1 sin cámara o jugador");
            var bg=Place(root.transform,"Sierra · panorama",art.sierra,new Vector3(player.transform.position.x+12,player.transform.position.y+5.8f,5),new Vector2(81,27),-100);
            bg.gameObject.AddComponent<ParallaxArt>().factor=.65f;
            var rock=art.terrain[1];string fillPath=Root+"Sprites/SierraRockFill.asset";
            const string solidPath=Root+"Sprites/TerrainBacking.asset";
            var backing=AssetDatabase.LoadAssetAtPath<Sprite>(solidPath);
            if(backing==null)
            {
                var texture=new Texture2D(2,2);texture.SetPixels(new[]{Color.white,Color.white,Color.white,Color.white});texture.Apply();
                AssetDatabase.CreateAsset(texture,Root+"Sprites/TerrainBackingTexture.asset");backing=Sprite.Create(texture,new Rect(0,0,2,2),Vector2.one*.5f,2,0,SpriteMeshType.FullRect);AssetDatabase.CreateAsset(backing,solidPath);
            }
            var fill=AssetDatabase.LoadAssetAtPath<Sprite>(fillPath);
            if(fill==null)
            {
                var r=rock.rect;fill=Sprite.Create(rock.texture,new Rect(r.x+r.width*.15f,r.y+r.height*.19f,r.width*.7f,r.height*.4f),Vector2.one*.5f,100,0,SpriteMeshType.FullRect);
                fill.name="SierraRockFill";AssetDatabase.CreateAsset(fill,fillPath);
            }
            foreach(var map in UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            {
                var mapRenderer=map.GetComponent<TilemapRenderer>();if(mapRenderer==null)continue;
                var bounds=map.cellBounds;checks.Add("Tilemap "+map.name+": "+bounds);
                if(map.GetComponent<TilemapCollider2D>()==null)continue;
                mapRenderer.enabled=false;
                Vector3 axis=map.CellToWorld(Vector3Int.right)-map.CellToWorld(Vector3Int.zero);
                Vector3 up=map.CellToWorld(Vector3Int.up)-map.CellToWorld(Vector3Int.zero);
                float width=Mathf.Abs(axis.x),height=Mathf.Abs(up.y);if(width<.01f||height<.01f)continue;
                for(int y=bounds.yMin;y<bounds.yMax;y++)
                for(int x=bounds.xMin;x<bounds.xMax;x++)
                {
                    var cell=new Vector3Int(x,y,0);if(!map.HasTile(cell))continue;
                    bool surface=!map.HasTile(cell+Vector3Int.up);int run=1;
                    while(run<3&&x+run<bounds.xMax&&map.HasTile(cell+Vector3Int.right*run)&&!map.HasTile(cell+Vector3Int.right*run+Vector3Int.up)==surface)run++;
                    var position=map.GetCellCenterWorld(cell);position.x+=(run-1)*width*.5f;position.z=0;
                    Place(root.transform,"Masa de roca",backing,position,new Vector2(width*run+.04f,height+.04f),-4).color=new Color(.22f,.23f,.23f);
                    Place(root.transform,"Roca",fill,position,new Vector2(width*run+.04f,height+.04f),-3);
                    if(surface)Place(root.transform,"Borde transitable",art.terrain[0],position+Vector3.up*(height*.5f-.23f),new Vector2(width*run+.06f,.6f),-2);
                    x+=run-1;
                }
            }
            (player.GetComponent<PlayerArtDriver>()??player.gameObject.AddComponent<PlayerArtDriver>()).Install(art);
            player.GetComponent<PlayerArtDriver>().actor.transform.localPosition=new Vector3(0,1.02f,0);
            foreach(var enemy in UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
                (enemy.GetComponent<EnemyArtDriver>()??enemy.gameObject.AddComponent<EnemyArtDriver>()).Install(art);
            EditorSceneManager.SaveScene(scene);checks.Add("LV1: entorno sustituido, colisiones y progreso conservados");
        }
        private static void PreviewScene(int biome)
        {
            string name=new[]{"Sierra","Pantano","PantanoAcuatico","MinerTown"}[biome];
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("Ambiente · "+name);root.AddComponent<ArtPreviewScene>().biome=name;
            var cameraObject=new GameObject("Main Camera",typeof(Camera));cameraObject.tag="MainCamera";var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=6;
            camera.transform.position=new Vector3(0,3.6f,-10);camera.backgroundColor=new Color(.06f,.1f,.12f);camera.clearFlags=CameraClearFlags.SolidColor;cameraObject.AddComponent<AudioListener>();
            var background=new[]{art.sierra,art.swamp,art.underwater,art.mine}[biome];
            Place(root.transform,"Panorama",background,new Vector3(4,4.5f,5),new Vector2(54,18),-100);
            int tile=biome==0?0:biome==3?8:4;
            if(biome!=2)for(int i=0;i<6;i++)Place(root.transform,"Camino",art.terrain[tile],new Vector3(-15+i*6,-.55f,0),new Vector2(6.1f,1.2f),-1);
            var player=AnimatedActor.Create("Juan",root.transform,biome==2?art.playerWater:art.playerLand,art.playerPortrait);
            player.transform.position=new Vector3(-5,biome==2?3:.77f,0);var demo=player.gameObject.AddComponent<ActorShowcase>();demo.actor=player;demo.states=biome==2?new[]{"Idle","Swim","Up","Down"}:new[]{"Idle","Run","Melee","Dash"};
            RuntimeAnimatorController bossController=new[]{art.rastreador,art.caimanLand,art.caimanWater,art.director}[biome];
            Sprite bossSprite=new[]{art.rastreadorPortrait,art.caimanPortrait,Load<Sprite>("Assets/Animations/V2/CaimanAcuatico_Idle0.asset"),art.directorPortrait}[biome];
            var boss=AnimatedActor.Create("Encuentro",root.transform,bossController,bossSprite,biome==1||biome==2?1.6f:1);boss.transform.position=new Vector3(6,biome==1?1.55f:biome==2?3:0,0);boss.Face(-1);
            demo=boss.gameObject.AddComponent<ActorShowcase>();demo.actor=boss;demo.states=biome==0||biome==3?new[]{"Idle","Run","Telegraph","Attack"}:biome==1?new[]{"Idle","Run","Telegraph","Claw"}:new[]{"Idle","Swim","Telegraph","Charge"};
            int ally=biome==0?0:biome==3?2:1;
            var npc=AnimatedActor.Create(new[]{"Mara","Tico","Luma"}[ally],root.transform,art.allyControllers[ally],art.allies[ally]);npc.transform.position=new Vector3(-1,biome==2?6.1f:0,0);
            if(biome==2)Place(root.transform,"Cámara de aire · soporte",art.terrain[6],new Vector3(-1,5.7f,0),new Vector2(3,.8f),-1);
            string prop=biome==0?"drone":biome==3?"valve":"crate";
            Place(root.transform,"Objeto",art.Icon(prop),new Vector3(-8,.5f,0),new Vector2(1.8f,1.2f),10);
            if(biome==3)Place(root.transform,"Baliza",art.terrain[11],new Vector3(9,1,0),new Vector2(.7f,2),9);
            else if(biome!=2)Place(root.transform,"Vegetación",art.terrain[biome==0?3:7],new Vector3(-9,.4f,0),new Vector2(2.8f,1.4f),8);
            if(biome==3)
            {
                var shield=PrefabUtility.InstantiatePrefab(Load<GameObject>(Root+"Prefabs/EscudoPlasma.prefab")) as GameObject;
                shield.transform.SetParent(player.transform,false);shield.transform.localPosition=Vector3.zero;
            }
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/Art/"+name+".unity");checks.Add("Escena de arte: "+name);
        }
    }
}
