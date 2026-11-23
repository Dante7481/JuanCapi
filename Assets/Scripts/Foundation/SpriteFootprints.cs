using System;
using System.Collections.Generic;
using UnityEngine;
namespace JuanCapi
{
    public sealed class SpriteFootprints:ScriptableObject
    {
        [Serializable] public struct Entry { public Sprite sprite; public float bottom; }
        public Entry[] entries;
        private Dictionary<Sprite,float> lookup;
        public float Bottom(Sprite sprite)
        {
            if(lookup==null){lookup=new Dictionary<Sprite,float>();foreach(var e in entries)if(e.sprite!=null)lookup[e.sprite]=e.bottom;}
            return sprite!=null&&lookup.TryGetValue(sprite,out float y)?y:sprite==null?0:sprite.bounds.min.y;
        }
    }
}
