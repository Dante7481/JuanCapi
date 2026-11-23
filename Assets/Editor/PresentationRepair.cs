using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace JuanCapi.Editor
{
    public static class PresentationRepair
    {
        static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Entregas/CorreccionesVisuales"));
        static readonly Dictionary<Texture2D,Color32[]> pixels=new Dictionary<Texture2D,Color32[]>();
        static readonly List<string> notes=new List<string>();
        [MenuItem("Juan Capi/Revisar presentación y plataformas")]
        public static void Repair()
        {
            Directory.CreateDirectory(Output);pixels.Clear();notes.Clear();
            bool alreadyRepaired=AssetDatabase.LoadAssetAtPath<SpriteFootprints>("Assets/Resources/Campaign/Footprints.asset")!=null;
            var paths=AssetDatabase.FindAssets("t:Sprite",new[]{"Assets/Animations","Assets/Art/Campaign/Sprites"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.EndsWith(".asset")).Distinct().ToArray();
            var sprites=paths.Select(AssetDatabase.LoadAssetAtPath<Sprite>).Where(s=>s!=null).ToArray();
            foreach(var tex in sprites.Select(s=>s.texture).Distinct())
            {
                var importer=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex)) as TextureImporter;if(importer==null)continue;
                importer.isReadable=true;importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=8192;importer.SaveAndReimport();
                pixels[tex]=tex.GetPixels32();
            }
            foreach(var s in sprites)
            {
                string path=AssetDatabase.GetAssetPath(s);if(alreadyRepaired||!path.StartsWith("Assets/Animations/"))continue;
                if(!pixels.ContainsKey(s.texture))continue;
                Rect old=s.rect;int l=(int)old.xMin,r=(int)old.xMax,b=(int)old.yMin,t=(int)old.yMax;
                var data=pixels[s.texture];int width=s.texture.width,height=s.texture.height;
                bool Vertical(int x,int lo,int hi)=>Enumerable.Range(Math.Max(0,lo),Math.Max(0,Math.Min(height,hi)-Math.Max(0,lo))).Count(y=>data[y*width+x].a>40)>2;
                bool Horizontal(int y,int lo,int hi)=>Enumerable.Range(Math.Max(0,lo),Math.Max(0,Math.Min(width,hi)-Math.Max(0,lo))).Count(x=>data[y*width+x].a>40)>2;
                int mx=Mathf.CeilToInt(old.width*.16f),my=Mathf.CeilToInt(old.height*.22f);
                // Expand only an occupied edge; retain the original pivot in texture space.
                for(int i=0;i<mx&&l>0&&Vertical(l,b,t);i++)l--;
                for(int i=0;i<mx&&r<width&&Vertical(r-1,b,t);i++)r++;
                for(int i=0;i<my&&b>0&&Horizontal(b,l,r);i++)b--;
                for(int i=0;i<my&&t<height&&Horizontal(t-1,l,r);i++)t++;
                if(l==old.x&&r==old.xMax&&b==old.y&&t==old.yMax)continue;
                var rect=new Rect(l,b,r-l,t-b);var pivot=new Vector2((old.x+s.pivot.x-l)/rect.width,(old.y+s.pivot.y-b)/rect.height);
                var repaired=Sprite.Create(s.texture,rect,pivot,s.pixelsPerUnit,0,SpriteMeshType.FullRect);repaired.name=s.name;
                EditorUtility.CopySerialized(repaired,s);UnityEngine.Object.DestroyImmediate(repaired);EditorUtility.SetDirty(s);notes.Add(path+": "+old+" -> "+rect);
            }
            var feet=AssetDatabase.LoadAssetAtPath<SpriteFootprints>("Assets/Resources/Campaign/Footprints.asset");
            if(feet==null){feet=ScriptableObject.CreateInstance<SpriteFootprints>();AssetDatabase.CreateAsset(feet,"Assets/Resources/Campaign/Footprints.asset");}
            feet.entries=sprites.Where(s=>pixels.ContainsKey(s.texture)).Select(s=>
            {
                var rect=s.rect;int bottom=(int)rect.y;bool found=false;var data=pixels[s.texture];
                for(int y=(int)rect.yMin;y<(int)rect.yMax&&!found;y++)
                {
                    int count=0;for(int x=(int)rect.xMin;x<(int)rect.xMax;x++){var p=data[y*s.texture.width+x];if(p.a>180&&Mathf.Min(p.r,Mathf.Min(p.g,p.b))<220&&p.r>p.g*.8f&&p.b<p.r*1.1f)count++;}
                    if(count>=Mathf.Max(4,rect.width*.035f)){bottom=y;found=true;}
                }
                return new SpriteFootprints.Entry{sprite=s,bottom=(bottom-rect.y-s.pivot.y)/s.pixelsPerUnit};
            }).ToArray();EditorUtility.SetDirty(feet);
            foreach(string p in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Art/Campaign/Generated","Assets/Art/V2/Sprites/Fondos","Assets/Art/Amazonia/Originals"}).Select(AssetDatabase.GUIDToAssetPath))
            {var imp=AssetImporter.GetAtPath(p) as TextureImporter;if(imp==null)continue;imp.filterMode=FilterMode.Bilinear;imp.maxTextureSize=8192;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();}
            AssetDatabase.SaveAssets();SierraBuilder.BuildLevel();SwampBuilder.Build();MineBuilder.Build();
            foreach(string scene in new[]{"LV1","AmazoniaTerrestre","AmazoniaAcuatica","MinerTown"})
            {
                var loaded=EditorSceneManager.OpenScene("Assets/Scenes/"+scene+".unity");
                if(scene.StartsWith("Amazonia"))RepairSwamp(scene=="AmazoniaAcuatica");
                foreach(var actor in UnityEngine.Object.FindObjectsByType<AnimatedActor>(FindObjectsInactive.Include))
                {
                    var c=actor.animator.runtimeAnimatorController;if(c==null)continue;
                    actor.groundFeet=!c.name.Contains("Acuatico")&&!c.name.Contains("Dron")&&!c.name.Contains("Pez");
                    if(actor.GetComponentInParent<PlayerController>()!=null)actor.groundOffset=-actor.transform.localPosition.y/actor.transform.localScale.y;
                    if(actor.GetComponentInParent<CaimanEncounter>()!=null&&!c.name.Contains("Acuatico"))actor.groundOffset=-.9f;
                    if(actor.groundFeet&&actor.sprite.sprite!=null)actor.sprite.transform.localPosition=new Vector3(0,actor.groundOffset-feet.Bottom(actor.sprite.sprite),0);
                }
                EditorSceneManager.SaveScene(loaded);
            }
            EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");AssetDatabase.SaveAssets();File.WriteAllLines(Path.Combine(Output,"ajustes-sprites.txt"),notes);
        }
        static void RepairSwamp(bool water)
        {
            var all=UnityEngine.Object.FindObjectsByType<Transform>();
            foreach(var t in all)
            {
                if(t==null)continue;
                var rock=t.GetComponent<SpriteRenderer>();
                if(rock!=null&&(t.name=="Roca V2"||t.name=="Plataforma V2"||t.name=="Tronco transitable"))
                {
                    var bounds=rock.bounds;var full=CampaignArt.Current.terrain[water?5:4];
                    rock.sprite=full;var parent=t.parent==null?Vector3.one:t.parent.lossyScale;
                    t.localScale=new Vector3(bounds.size.x/full.bounds.size.x/Mathf.Abs(parent.x),bounds.size.y/full.bounds.size.y/Mathf.Abs(parent.y),1);
                    t.position=bounds.center;rock.color=water?new Color(.62f,.8f,.72f):Color.white;
                }
                // Source ruins use polygon colliders: derive them from the visible rock instead of the old oversized outline.
                var polygon=t.GetComponent<PolygonCollider2D>();
                if(polygon!=null&&!polygon.isTrigger)
                {
                    var visual=t.GetComponentsInChildren<SpriteRenderer>().FirstOrDefault(r=>r.name=="Roca V2");
                    if(visual!=null)
                    {
                        UnityEngine.Object.DestroyImmediate(polygon);var box=t.gameObject.AddComponent<BoxCollider2D>();
                        var bounds=visual.bounds;box.offset=t.InverseTransformPoint(bounds.center);box.size=new Vector2(bounds.size.x/Mathf.Abs(t.lossyScale.x),bounds.size.y/Mathf.Abs(t.lossyScale.y));
                    }
                }
                if(!water&&(t.name=="Tronco inicial"||t.name=="Raíz elevada"||t.name.StartsWith("Plataforma de combate")||t.name=="Muelle"))
                {
                    var c=t.GetComponent<Collider2D>();if(c!=null){c.usedByEffector=true;var e=t.GetComponent<PlatformEffector2D>()??t.gameObject.AddComponent<PlatformEffector2D>();e.useOneWay=true;e.useOneWayGrouping=true;e.surfaceArc=165;}
                }
                if(t.name=="Textura"||t.name=="Musgo")
                {var sr=t.GetComponent<SpriteRenderer>();if(sr!=null)sr.enabled=false;}
                if(water&&t.name=="Corriente del canal")t.gameObject.SetActive(false);
            }
            foreach(var r in UnityEngine.Object.FindObjectsByType<SpriteRenderer>())
            {
                if(r.sprite==null)continue;var imp=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(r.sprite.texture)) as TextureImporter;
                if(imp!=null&&imp.filterMode!=FilterMode.Bilinear){imp.filterMode=FilterMode.Bilinear;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.maxTextureSize=8192;imp.SaveAndReimport();}
            }
            if(!water&&GameObject.Find("Estratos de la orilla")==null)
            {
                var root=new GameObject("Estratos de la orilla");var rock=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Campaign/Sprites/SierraRockFill.asset");
                foreach(var c in UnityEngine.Object.FindObjectsByType<BoxCollider2D>().Where(c=>c.name=="Fondo transitable"))
                {
                    var b=c.bounds;
                    for(float x=b.min.x;x<b.max.x;x+=3.5f)for(int row=0;row<3;row++)
                    {
                        float w=Mathf.Min(3.5f,b.max.x-x);var go=new GameObject("Estrato húmedo");go.transform.SetParent(root.transform);go.transform.position=new Vector3(x+w*.5f,b.max.y-1.1f-row*1.5f,0);
                        var sr=go.AddComponent<SpriteRenderer>();sr.sprite=rock;sr.sortingOrder=-1;sr.color=new Color(.42f,.53f,.37f);go.transform.localScale=new Vector3((w+.03f)/rock.bounds.size.x,1.7f/rock.bounds.size.y,1);
                    }
                }
            }
        }
        public static void PolishAndBuild()
        {
            foreach(string scene in new[]{"AmazoniaTerrestre","AmazoniaAcuatica"})
            {var loaded=EditorSceneManager.OpenScene("Assets/Scenes/"+scene+".unity");RepairSwamp(scene=="AmazoniaAcuatica");EditorSceneManager.SaveScene(loaded);}
            var sierra=EditorSceneManager.OpenScene("Assets/Scenes/LV1.unity");
            foreach(var r in UnityEngine.Object.FindObjectsByType<SpriteRenderer>())if(r.name=="Borde de ichu"){r.transform.position+=Vector3.up*.34f;r.name="Borde de ichu · alineado";}
            EditorSceneManager.SaveScene(sierra);
            EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");AssetDatabase.SaveAssets();Build();
        }
        public static void Build()
        {
            Directory.CreateDirectory(Output);bool release=Environment.GetCommandLineArgs().Contains("-juanRelease");
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName=Path.Combine(Output,release?"Juego/Windows/JuanCapi.exe":"Pruebas/Windows/JuanCapi.exe"),target=BuildTarget.StandaloneWindows64,options=release?BuildOptions.None:BuildOptions.Development});
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Compilación fallida");
        }
        public static void FinalizeAndBuild()
        {
            var feet=AssetDatabase.LoadAssetAtPath<SpriteFootprints>("Assets/Resources/Campaign/Footprints.asset");
            for(int i=0;i<feet.entries.Length;i++)
            {
                var e=feet.entries[i];var s=e.sprite;if(s==null||!s.name.StartsWith("Enemies_"))continue;
                var data=s.texture.GetPixels32();var rect=s.rect;
                for(int y=(int)rect.yMin;y<(int)rect.yMax;y++)
                {int count=0;for(int x=(int)rect.xMin;x<(int)rect.xMax;x++){var p=data[y*s.texture.width+x];if(p.a>180&&Mathf.Min(p.r,Mathf.Min(p.g,p.b))<220)count++;}if(count>=5){e.bottom=(y-rect.y-s.pivot.y)/s.pixelsPerUnit;break;}}
                feet.entries[i]=e;
            }
            EditorUtility.SetDirty(feet);AssetDatabase.SaveAssets();
            foreach(string scene in new[]{"LV1","AmazoniaTerrestre","AmazoniaAcuatica","MinerTown"})
            {
                var loaded=EditorSceneManager.OpenScene("Assets/Scenes/"+scene+".unity");
                foreach(var a in UnityEngine.Object.FindObjectsByType<AnimatedActor>(FindObjectsInactive.Include))if(a.groundFeet&&a.sprite.sprite!=null)a.sprite.transform.localPosition=new Vector3(0,a.groundOffset-feet.Bottom(a.sprite.sprite),0);
                EditorSceneManager.SaveScene(loaded);
            }
            EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");AssetDatabase.SaveAssets();Build();
        }
        public static void RepairAndBuild(){Repair();Build();}
    }
}
