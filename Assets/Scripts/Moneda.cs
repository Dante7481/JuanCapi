using UnityEngine;
using JuanCapi;

public class Moneda : MonoBehaviour
{
    private string id;
    private void Start()
    {
        id = WorldInteraction.StableId(gameObject);
        if (GameRuntime.Instance?.Data?.collected.Contains(id) == true) gameObject.SetActive(false);
        var art=CampaignArt.Current;
        if(art!=null)
        {
            var old=GetComponent<SpriteRenderer>();if(old!=null)old.enabled=false;
            var animation=GetComponent<Animator>();if(animation!=null)animation.enabled=false;
            var visual=new GameObject("Oro");visual.transform.SetParent(transform,false);
            var sprite=visual.AddComponent<SpriteRenderer>();sprite.sprite=art.Icon("gold");sprite.sortingOrder=16;
            visual.transform.localScale=new Vector3(.48f/Mathf.Abs(transform.lossyScale.x)/sprite.sprite.bounds.size.x,.4f/Mathf.Abs(transform.lossyScale.y)/sprite.sprite.bounds.size.y,1);
            visual.AddComponent<FloatingArtwork>();
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            var game = GameRuntime.Instance;
            if (game?.Data != null && game.Data.Collect(id, "gold", 10))
            {
                game.NotifyChanged(); PlasmaEffects.Burst(transform.position,new Color(1,.83f,.3f),.45f); gameObject.SetActive(false);
            }
        }
    }
}
