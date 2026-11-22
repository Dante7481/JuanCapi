using UnityEngine;
using UnityEngine.EventSystems;

namespace JuanCapi
{
    public sealed class SelectionAccent : MonoBehaviour,ISelectHandler,IDeselectHandler
    {
        public GameObject indicator;
        public void OnSelect(BaseEventData data){CampaignAudio.Play("select",true);if(indicator!=null)indicator.SetActive(true);}
        public void OnDeselect(BaseEventData data){if(indicator!=null)indicator.SetActive(false);}
    }
}
