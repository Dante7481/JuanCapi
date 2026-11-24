using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace JuanCapi.Editor
{
    public static class WorldPresentationRepair
    {
        public static void Pantano()
        {
            foreach(var t in Object.FindObjectsByType<Transform>())
            {
                if(t==null)continue;
                if(new[]{"Camino extendido del pantano","Orilla del lago","Suelo continuo del pantano","Fondo transitable","Tierra continua","Borde de musgo","Estratos de la orilla","Cobertura de raíz","Manglar"}.Contains(t.name))Object.DestroyImmediate(t.gameObject);
            }
            var floor=new GameObject("Suelo continuo del pantano");floor.layer=3;
            floor.transform.position=new Vector3(SwampLevel.ShoreX*.5f,-2,0);floor.AddComponent<BoxCollider2D>().size=new Vector2(SwampLevel.ShoreX,4);
            var panorama=GameObject.Find("Panorama original");var parallax=panorama.GetComponent<ParallaxArt>();if(parallax!=null)Object.DestroyImmediate(parallax);
            var scenes=new Sprite[3];
            for(int i=0;i<3;i++)
            {
                string path="Assets/Art/Campaign/Generated/PantanoCamino0"+(i+1)+".png";
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.maxTextureSize=8192;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=false;importer.SaveAndReimport();
                scenes[i]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            string blendPath="Assets/Art/Campaign/PanoramaBlend.mat";var blend=AssetDatabase.LoadAssetAtPath<Material>(blendPath);
            if(blend==null){blend=new Material(Shader.Find("JuanCapi/PanoramaBlend"));AssetDatabase.CreateAsset(blend,blendPath);}blend.SetFloat("_BlendWidth",2f/45);EditorUtility.SetDirty(blend);
            panorama.GetComponent<SpriteRenderer>().sprite=scenes[0];panorama.GetComponent<SpriteRenderer>().flipX=false;
            panorama.transform.position=new Vector3(22.5f,3.9f,0);
            panorama.transform.localScale=new Vector3(45/scenes[0].bounds.size.x,15/scenes[0].bounds.size.y,1);
            var extension=new GameObject("Camino extendido del pantano");
            for(int i=1;i<3;i++)
            {
                var part=Object.Instantiate(panorama,extension.transform);part.name="Camino fijo "+(i+1);
                part.transform.position=panorama.transform.position+Vector3.right*(43*i);
                var r=part.GetComponent<SpriteRenderer>();r.sprite=scenes[i];r.flipX=false;r.sharedMaterial=blend;r.sortingOrder=panorama.GetComponent<SpriteRenderer>().sortingOrder+i;
                part.transform.localScale=new Vector3(45/scenes[i].bounds.size.x,15/scenes[i].bounds.size.y,1);
            }
            // Three distinct painted sections, with a short soft overlap.
            // The lake exists only at the end, outside the terrestrial collider.
            var shore=new GameObject("Orilla del lago");
            var water=new GameObject("Agua pintada · delante de la inmersión").AddComponent<SpriteRenderer>();water.transform.SetParent(shore.transform);
            var texture=scenes[2].texture;int shorePixel=Mathf.RoundToInt((SwampLevel.ShoreX-86)/45*texture.width);
            var foreground=Sprite.Create(texture,new Rect(shorePixel,0,texture.width-shorePixel,Mathf.RoundToInt(texture.height*.24f)),new Vector2(0,1),100,0,SpriteMeshType.FullRect);foreground.name="PantanoAguaFrontal";
            string foregroundPath="Assets/Art/Campaign/Sprites/PantanoAguaFrontal.asset";var saved=AssetDatabase.LoadAssetAtPath<Sprite>(foregroundPath);
            if(saved==null){AssetDatabase.CreateAsset(foreground,foregroundPath);saved=foreground;}else{EditorUtility.CopySerialized(foreground,saved);Object.DestroyImmediate(foreground);EditorUtility.SetDirty(saved);}
            water.sprite=saved;water.sortingOrder=25;water.transform.position=new Vector3(86+shorePixel*45f/texture.width,0,0);water.transform.localScale=new Vector3(4500f/texture.width,1500f/texture.height,1);
            var dive=Object.FindObjectsByType<SwampProp>().FirstOrDefault(p=>p.kind==SwampPropKind.Dive);if(dive!=null)dive.transform.position=new Vector3(SwampLevel.ShoreX-1,.8f,0);
            // Small raised wooden ledges only; never create holes in the path.
            var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Campaign/Imported/AmazoniaTerrestreOriginal.prefab");
            string[] names={"Tronco inicial","Raíz elevada","Plataforma de combate A","Plataforma de combate B","Muelle"};
            float[] xs={12,37,86,99,121},ys={1.4f,1.2f,1.4f,1.4f,.8f};
            for(int i=0;i<names.Length;i++)
            {
                var old=GameObject.Find(names[i]);if(old!=null)Object.DestroyImmediate(old);
                var go=Object.Instantiate(original.transform.Find(names[i]).gameObject);go.name=names[i];go.transform.position=new Vector3(xs[i],ys[i],0);
                foreach(var c in go.GetComponentsInChildren<Collider2D>())Object.DestroyImmediate(c);
                foreach(var r in go.GetComponentsInChildren<SpriteRenderer>()){r.sortingLayerID=0;r.sortingOrder=5;}
                go.layer=3;var box=go.AddComponent<BoxCollider2D>();box.size=new Vector2(i==4?3:3.5f,.25f);box.usedByEffector=true;
                var e=go.GetComponent<PlatformEffector2D>()??go.AddComponent<PlatformEffector2D>();e.useOneWay=true;e.useOneWayGrouping=true;e.surfaceArc=165;
                var renderers=go.GetComponentsInChildren<SpriteRenderer>();
                foreach(var r in renderers)if(r.name!="Tronco transitable"&&r.name!="Plataforma V2")r.enabled=false;
                var plank=renderers.FirstOrDefault(r=>r.name=="Tronco transitable"||r.name=="Plataforma V2")??renderers.FirstOrDefault();
                if(plank!=null){plank.enabled=true;plank.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Amazonia/Beam.asset");plank.transform.localPosition=Vector3.zero;plank.transform.localScale=new Vector3(box.size.x/plank.sprite.bounds.size.x,.32f/plank.sprite.bounds.size.y,1);box.offset=new Vector2(0,.035f);}
            }
        }
        public static void Mina()
        {
            ExtendLanding(32.6f,31.4f);
            var water=GameObject.Find("Conducto · agua");water.transform.position=new Vector3(59,-3.65f,0);
            var sr=water.GetComponent<SpriteRenderer>();water.transform.localScale=new Vector3(26/sr.sprite.bounds.size.x,6.7f/sr.sprite.bounds.size.y,1);
            GameObject.Find("Superficie del agua").transform.position=new Vector3(59,-.3f,0);
            foreach(var r in Object.FindObjectsByType<SpriteRenderer>())if(r.name=="Pasarela")
            {
                // The top of the opaque metal, rather than the transparent border,
                // is the contact surface. Keep the collider on world y=0.
                r.transform.position=new Vector3(r.transform.position.x,r.transform.position.y+.08f,r.transform.position.z);
                r.name="Pasarela · superficie alineada";
            }
        }
        public static void Apply()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/LV1.unity");ExtendLanding(111.6f,110.4f);AlignFeet();EditorSceneManager.SaveScene(scene);
            scene=EditorSceneManager.OpenScene("Assets/Scenes/AmazoniaTerrestre.unity");Pantano();AlignFeet();EditorSceneManager.SaveScene(scene);
            scene=EditorSceneManager.OpenScene("Assets/Scenes/MinerTown.unity");Mina();AlignFeet();EditorSceneManager.SaveScene(scene);
            EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");AssetDatabase.SaveAssets();
        }
        static void AlignFeet()
        {
            var feet=Resources.Load<SpriteFootprints>("Campaign/Footprints");
            foreach(var a in Object.FindObjectsByType<AnimatedActor>(FindObjectsInactive.Include))
                if(a.groundFeet&&a.sprite.sprite!=null)a.sprite.transform.localPosition=new Vector3(0,a.groundOffset-feet.Bottom(a.sprite.sprite),0);
        }
        static void ExtendLanding(float oldLeft,float newLeft)
        {
            var box=Object.FindObjectsByType<BoxCollider2D>().FirstOrDefault(c=>c.gameObject.layer==3&&!c.isTrigger&&Mathf.Abs(c.bounds.min.x-oldLeft)<.05f&&Mathf.Abs(c.bounds.max.y)<.05f);
            if(box==null)return;
            var b=box.bounds;float width=b.max.x-newLeft;
            var scale=box.transform.localScale;scale.x*=width/b.size.x;box.transform.localScale=scale;
            box.transform.position+=Vector3.left*(oldLeft-newLeft)*.5f;
        }
        public static void ValidateAndBuild()
        {
            PreciseSpriteGeometry.Bake();PreciseSpriteGeometry.Preview();Apply();
            foreach(string path in AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/Resources/Audio/Music"}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var imp=(AudioImporter)AssetImporter.GetAtPath(path);var sample=imp.defaultSampleSettings;sample.loadType=AudioClipLoadType.Streaming;sample.compressionFormat=AudioCompressionFormat.Vorbis;sample.quality=.85f;imp.defaultSampleSettings=sample;imp.SaveAndReimport();
            }
            PreciseSpriteGeometry.Build();
        }
        public static void ApplyAndBuild(){Apply();PreciseSpriteGeometry.Build();}
    }
}
