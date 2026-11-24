using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace JuanCapi.Editor
{
    public static class EnvironmentArtUpgrade
    {
        const string Folder="Assets/Art/Campaign/EnvironmentV2";
        static Sprite[] industrial,machinery,nature,refuges;
        static Material material;
        static Transform decoration;
        static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Entregas/EntornosV2"));
        static bool IsArt(Color32 c)=>c.a>20&&!(Math.Min(c.r,c.b)-c.g>55);
        static Sprite[] Import(string name,int rows)
        {
            string path=Folder+"/"+name+".png";var imp=(TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType=TextureImporterType.Default;imp.isReadable=true;imp.maxTextureSize=8192;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.filterMode=FilterMode.Bilinear;imp.wrapMode=TextureWrapMode.Clamp;imp.mipmapEnabled=false;imp.SaveAndReimport();
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=tex.GetPixels32();var result=new Sprite[rows*3];
            for(int i=0;i<result.Length;i++)
            {
                int x0=(i%3)*tex.width/3,x1=(i%3+1)*tex.width/3,y0=tex.height-(i/3+1)*tex.height/rows,y1=tex.height-(i/3)*tex.height/rows;
                int l=x1,r=x0,b=y1,t=y0;
                for(int y=y0+2;y<y1-2;y++)for(int x=x0+2;x<x1-2;x++)if(IsArt(pixels[y*tex.width+x])){l=Math.Min(l,x);r=Math.Max(r,x+1);b=Math.Min(b,y);t=Math.Max(t,y+1);}
                if(r<=l)throw new Exception("Empty environment cell "+name+i);
                var s=Sprite.Create(tex,new Rect(l,b,r-l,t-b),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);s.name=name+"_"+i;
                string asset=Folder+"/Sprites/"+s.name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Sprite>(asset);
                if(existing==null){AssetDatabase.CreateAsset(s,asset);result[i]=s;}else{EditorUtility.CopySerialized(s,existing);Object.DestroyImmediate(s);EditorUtility.SetDirty(existing);result[i]=existing;}
            }
            return result;
        }
        static SpriteRenderer Draw(string name,Sprite sprite,Vector2 center,Vector2 size,int order=-1,Transform parent=null)
        {
            var go=new GameObject("Entorno V2 · "+name);go.transform.SetParent(parent??decoration,false);go.transform.position=new Vector3(center.x,center.y,0);
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sharedMaterial=material;sr.sortingOrder=order;
            var scale=go.transform.parent.lossyScale;go.transform.localScale=new Vector3(size.x/sprite.bounds.size.x/Mathf.Abs(scale.x),size.y/sprite.bounds.size.y/Mathf.Abs(scale.y),1);return sr;
        }
        static SpriteRenderer Grounded(string name,Sprite sprite,float x,float bottom,float width,int order=-3,Transform parent=null)
        {float h=width*sprite.bounds.size.y/sprite.bounds.size.x;return Draw(name,sprite,new Vector2(x,bottom+h*.5f),new Vector2(width,h),order,parent);}
        static void Replace(SpriteRenderer sr,Sprite sprite)
        {
            var b=sr.bounds;sr.sprite=sprite;sr.sharedMaterial=material;
            var parent=sr.transform.parent==null?Vector3.one:sr.transform.parent.lossyScale;
            sr.transform.localScale=new Vector3(b.size.x/sprite.bounds.size.x/Mathf.Abs(parent.x),b.size.y/sprite.bounds.size.y/Mathf.Abs(parent.y),1);
            sr.transform.position=b.center;
        }
        static Bounds WorldBounds(BoxCollider2D collider)
        {
            Vector2 half=collider.size*.5f;
            var result=new Bounds(collider.transform.TransformPoint(collider.offset-half),Vector3.zero);
            result.Encapsulate(collider.transform.TransformPoint(collider.offset+half));
            result.Encapsulate(collider.transform.TransformPoint(collider.offset+new Vector2(half.x,-half.y)));
            result.Encapsulate(collider.transform.TransformPoint(collider.offset+new Vector2(-half.x,half.y)));
            return result;
        }
        static string CollisionSignature()=>string.Join("\n",Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Include).OrderBy(c=>c.GetEntityId().ToString()).Select(c=>c.GetEntityId().ToString()+":"+EditorJsonUtility.ToJson(c)+":"+c.transform.localToWorldMatrix));
        [MenuItem("Juan Capi/Arte/Integrar colección de entornos V2")]
        public static void Apply()
        {
            Directory.CreateDirectory(Output);Directory.CreateDirectory(Folder+"/Sprites");AssetDatabase.Refresh();
            industrial=Import("Industrial",3);machinery=Import("Machinery",3);nature=Import("Nature",2);refuges=Import("Refuges",2);
            material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/EnvironmentCutout.mat");
            if(material==null){material=new Material(Shader.Find("JuanCapi/EnvironmentCutout"));AssetDatabase.CreateAsset(material,Folder+"/EnvironmentCutout.mat");}
            material.color=Color.white;EditorUtility.SetDirty(material);CreatePrefabs();
            var report=new List<string>();
            foreach(string name in new[]{"LV1","AmazoniaTerrestre","AmazoniaAcuatica","MinerTown"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
                if(GameObject.Find("Colección de entornos V2")!=null)throw new Exception("This scene is already decorated. Start from the saved pre-upgrade scene to preserve manual edits.");
                string before=CollisionSignature();decoration=new GameObject("Colección de entornos V2").transform;
                if(name=="LV1")Sierra();else if(name=="MinerTown")Mine();else Swamp(name=="AmazoniaAcuatica");
                if(before!=CollisionSignature())throw new Exception("A collider changed in "+name);
                int count=Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include).Count(r=>r.sharedMaterial==material);
                report.Add(name+": "+count+" sprites de la colección; colliders y transformaciones físicas idénticos.");
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();File.WriteAllLines(Path.Combine(Output,"Integracion.txt"),report);EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");
        }
        static void CreatePrefabs()
        {
            Directory.CreateDirectory(Folder+"/Prefabs");AssetDatabase.Refresh();
            var sprites=industrial.Concat(machinery).Concat(nature).Concat(refuges).ToArray();
            string[] names={"Suelo de cantera","Suelo de mampostería","Suelo de ladrillo","Pasarela de acero","Pasarela de madera","Pasarela de cobre","Estrato de mineral","Muro de piedra","Muro con tubería","Pilar de acero","Poste de madera","Reja de rescate","Codo de cobre","Tubería con manómetro","Válvula vertical","Cabezal hidráulico","Vagoneta con herramientas","Sacos y bobina","Cornisa de pizarra","Cornisa de ichu","Troncos del pantano","Roca mineral","Ruina sumergida","Travesaño de madera","Refugio de Mara","Refugio de Tico","Refugio de Luma","Piedras y helechos","Bombas de servicio","Banco de trabajo"};
            for(int i=0;i<sprites.Length;i++)
            {
                var go=new GameObject(names[i]);var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprites[i];sr.sharedMaterial=material;
                PrefabUtility.SaveAsPrefabAsset(go,Folder+"/Prefabs/"+names[i]+".prefab");Object.DestroyImmediate(go);
            }
        }
        static void Mine()
        {
            var old=Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);
            foreach(var r in old)
            {
                if(new[]{"Relleno de cantera","Muro de contención","Pasarela","Pasarela · superficie alineada","Conducto · panel del fondo"}.Contains(r.name))r.enabled=false;
                if(r.name=="Conducto · tubería"||r.name=="Descarga · tubería inicial"||r.name=="Sala de válvulas · consola")Replace(r,machinery[3+Mathf.Abs(Mathf.RoundToInt(r.transform.position.x))%3]);
                if(r.name=="Rodillo de cinta")Replace(r,industrial[5]);
                if(r.name=="Cable · abrazadera")r.enabled=false;
            }
            var boxes=Object.FindObjectsByType<BoxCollider2D>(FindObjectsInactive.Include);
            foreach(var c in boxes)
            {
                var b=WorldBounds(c);if(b.size.x<.1f)continue;
                if(c.name=="Suelo de cantera")
                {
                    int n=Mathf.CeilToInt(b.size.x/6.5f);float width=b.size.x/n;
                    for(int j=0;j<n;j++)
                    {
                        float x=b.min.x+width*(j+.5f);int index=(j+Mathf.RoundToInt(b.min.x/7))%3;if(index<0)index+=3;
                        // Rich masonry underneath, with a thin separate walking deck.
                        Draw("Cimentación "+index,industrial[6+index],new Vector2(x,b.max.y-2.65f),new Vector2(width+.025f,5.3f),-3,c.transform);
                        Draw("Borde transitable "+index,industrial[3+index],new Vector2(x,b.max.y-.36f),new Vector2(width+.025f,.78f),2,c.transform);
                    }
                    for(float x=b.min.x+5;x<b.max.x-3;x+=17)Grounded("Vagoneta y mineral",machinery[7],x,b.max.y,1.6f,-4,c.transform);
                }
                else if(c.name=="Pasarela elevada")
                {
                    foreach(var r in c.GetComponentsInChildren<SpriteRenderer>())r.enabled=false;
                    int index=Mathf.Abs(Mathf.RoundToInt(b.center.x))%3;Draw("Pasarela suspendida",industrial[3+index],new Vector2(b.center.x,b.max.y-.32f),new Vector2(b.size.x,.72f),3,c.transform);
                }
                else if(c.name=="Conducto · techo")
                {
                    foreach(var r in c.GetComponentsInChildren<SpriteRenderer>())r.enabled=false;
                    int n=Mathf.CeilToInt(b.size.x/6.5f);float w=b.size.x/n;for(int j=0;j<n;j++){Draw("Bóveda reforzada",industrial[6+j%3],new Vector2(b.min.x+(j+.5f)*w,b.center.y),new Vector2(w+.03f,b.size.y),5,c.transform);}
                }
                else if(c.GetComponentsInChildren<SpriteRenderer>(true).Any(r=>r.name=="Tramo de metal"))
                {
                    foreach(var r in c.GetComponentsInChildren<SpriteRenderer>(true))r.enabled=false;
                    float h=Mathf.Min(6,b.size.y);Draw("Compuerta remachada",machinery[0],new Vector2(b.center.x,b.min.y+h*.5f),new Vector2(Mathf.Max(h*.37f,b.size.x),h),8,c.transform);
                }
            }
            // The flooded channel is a built space, not a row of stretched pillars.
            for(int i=0;i<6;i++)Draw("Revestimiento del conducto",industrial[7+i%2],new Vector2(48.2f+i*4.2f,-4.2f),new Vector2(4.25f,5.8f),-22).color=new Color(.52f,.72f,.7f);
            var press=Object.FindFirstObjectByType<MinePress>();if(press!=null)
            {
                var original=press.head.GetComponent<SpriteRenderer>();original.enabled=false;
                Draw("Cabezal hidráulico",machinery[6],(Vector2)press.head.position+Vector2.up*.65f,new Vector2(2.1f,2.3f),15,press.head);
                var support=GameObject.Find("Soporte de prensa");if(support!=null)Replace(support.GetComponent<SpriteRenderer>(),machinery[4]);
                float x=press.transform.position.x;
                Draw("Bastidor de prensa izquierdo",machinery[0],new Vector2(x-2.1f,3),new Vector2(1.5f,6),-7);
                Draw("Bastidor de prensa derecho",machinery[0],new Vector2(x+2.1f,3),new Vector2(1.5f,6),-7);
                Draw("Viga de prensa",industrial[3],new Vector2(x,5.8f),new Vector2(5.5f,1),-6);
            }
            foreach(var prop in Object.FindObjectsByType<MineProp>())if(prop.kind==MinePropKind.Cable)Draw("Canalización cortable",machinery[5],prop.transform.position,new Vector2(1,2.6f),11,prop.transform);
            var level=Object.FindFirstObjectByType<MineLevel>();Grounded("Refugio de Luma",refuges[2],level.luma.transform.position.x,level.luma.transform.position.y,6.5f,-5);
            foreach(var r in old)if(r.name=="Sala de válvulas · cubierta")r.enabled=false;
            foreach(var p in Object.FindObjectsByType<MineProp>())if(p.kind==MinePropKind.Bench){var r=p.GetComponentInChildren<SpriteRenderer>();if(r!=null)Replace(r,refuges[5]);}
            foreach(var placement in new[]{new Vector2(10,0),new Vector2(76,0),new Vector2(97,0),new Vector2(157,0)})Grounded("Bombas de servicio",refuges[4],placement.x,placement.y,2.4f,-6);
            foreach(float x in new[]{8f,23,90,126})Grounded("Materiales del taller",machinery[8],x,0,1.5f,-4);
        }
        static void Sierra()
        {
            foreach(var r in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
            {
                if(r.name=="Roca continua"||r.name=="Estrato de pizarra"||r.name.StartsWith("Borde de ichu")||r.name.StartsWith("Taller ·"))r.enabled=false;
                if(r.name=="Tranca")Replace(r,machinery[1]);
            }
            foreach(var c in Object.FindObjectsByType<BoxCollider2D>(FindObjectsInactive.Include))
            {
                var b=WorldBounds(c);
                if(c.name=="Suelo de quebrada")
                {
                    int n=Mathf.CeilToInt(b.size.x/5.8f);float width=b.size.x/n;
                    for(int i=0;i<n;i++)
                    {
                        float x=b.min.x+(i+.5f)*width;
                        for(int row=0;row<2;row++)
                        {
                            var rock=nature[3];float h=width*rock.bounds.size.y/rock.bounds.size.x;
                            var sr=Draw("Estratos unidos",rock,new Vector2(x,b.max.y-.35f-h*.5f-row*(h-.3f)),new Vector2(width+.06f,h),-2,c.transform);sr.flipX=(i+row)%2==1;
                        }
                        var top=x>28&&x<41?industrial[0]:nature[1];float height=width*top.bounds.size.y/top.bounds.size.x;
                        Draw("Orilla de ichu",top,new Vector2(x,b.max.y+width*.14f-height*.5f),new Vector2(width+.07f,height),1,c.transform);
                    }
                }
                else if(c.name=="Cornisa")
                {
                    float h=b.size.x*nature[0].bounds.size.y/nature[0].bounds.size.x;
                    Draw("Cornisa de pizarra",nature[0],new Vector2(b.center.x,b.max.y+b.size.x*.12f-h*.5f),new Vector2(b.size.x+.025f,h),3,c.transform);
                }
            }
            var level=Object.FindFirstObjectByType<SierraLevel>();Grounded("Refugio de Mara",refuges[0],level.mara.transform.position.x+1,0,7.2f,-5);
            foreach(var p in Object.FindObjectsByType<SierraProp>())if(p.kind==SierraPropKind.Bench){var r=p.GetComponentInChildren<SpriteRenderer>();if(r!=null)Replace(r,refuges[5]);}
            foreach(var cage in Object.FindObjectsByType<SierraCageArt>()){cage.enabled=false;Grounded("Reja de rescate",machinery[2],cage.transform.position.x,cage.transform.position.y,2.4f,22,cage.transform);}
            foreach(float x in new[]{9f,36,73,120})Grounded("Suministros del camino",machinery[8],x,0,1.25f,-5);
        }
        static void Swamp(bool water)
        {
            int i=0;
            foreach(var r in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
            {
                if(!water&&(r.name=="Tronco transitable"||r.name=="Plataforma V2")&&r.enabled)
                {
                    var collider=r.GetComponentInParent<BoxCollider2D>();if(collider==null)continue;var b=WorldBounds(collider);r.enabled=false;
                    Draw("Travesaño de madera",nature[i++%2==0?2:5],new Vector2(b.center.x,b.max.y-.22f),new Vector2(b.size.x,.5f),5,collider.transform);
                }
                if(water&&r.name=="Roca V2")Replace(r,nature[4]);
                if(water&&r.name=="Banco · ménsula de ruina")Replace(r,nature[4]);
                if(r.name=="Material"&&r.GetComponentInParent<SwampProp>()==null&&r.transform.parent!=null&&r.transform.parent.name.StartsWith("Ruina del guardián"))Replace(r,nature[4]);
            }
            var level=Object.FindFirstObjectByType<SwampLevel>();
            if(water&&level.tico!=null)
            {
                foreach(var r in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))if(r.name=="Campana · aire"||(r.transform.parent!=null&&r.transform.parent.name.StartsWith("Campana ·")))r.enabled=false;
                var p=level.tico.transform.position;Grounded("Refugio de Tico",refuges[1],p.x+.15f,p.y-.7f,6.1f,7);
                // The visible stone sill follows the existing dry floor exactly.
                Draw("Umbral de la campana",nature[4],new Vector2(p.x,p.y-.48f),new Vector2(5,.85f),2);
            }
            foreach(var p in Object.FindObjectsByType<SwampProp>())if(p.kind==SwampPropKind.Bench)
            {
                var r=p.GetComponentInChildren<SpriteRenderer>();if(r!=null)Replace(r,refuges[5]);
                if(!water)Grounded("Taller de la orilla",refuges[0],p.transform.position.x,0,4.4f,-4);
            }
            if(!water)foreach(float x in new[]{18f,62,108})Grounded("Raíces y piedra",refuges[3],x,0,1.5f,-5);
        }
        public static void ApplyAndBuild(){Apply();PreciseSpriteGeometry.Build();}
    }
}
