using UnityEngine;
using UnityEngine.UI;

namespace JuanCapi
{
    public sealed class InputHintView : MonoBehaviour
    {
        public string action;
        public Text label;
        public SymbolGraphic mouse;
        private KeyCode previous=(KeyCode)(-1);
        private void Update()
        {
            var key=GameInput.Key(action);if(key==previous)return;previous=key;
            bool pointer=key==KeyCode.Mouse0||key==KeyCode.Mouse1;mouse.gameObject.SetActive(pointer);label.gameObject.SetActive(!pointer);
            if(pointer){mouse.mouseButton=key==KeyCode.Mouse0?0:1;mouse.SetVerticesDirty();}
            else label.text=key==KeyCode.LeftShift?"Shift":key==KeyCode.Space?"Espacio":key==KeyCode.LeftControl?"Ctrl":key.ToString();
        }
    }
}
