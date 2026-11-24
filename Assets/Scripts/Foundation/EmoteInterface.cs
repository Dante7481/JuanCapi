using UnityEngine;
namespace JuanCapi
{
    public sealed partial class GameInterface
    {
        public void Emotes()
        {
            if(!Game.CanPlay||Game.Player==null||Game.Player.isDead)return;
            Begin("emotes","EMOTES","T  ·  1 / 2 / 3",Play);
            var names=new[]{"Wazaaaa","Uiia","Six Seven"};
            for(int i=0;i<3;i++)
            {
                int index=i;Icon(page,CampaignArt.Current.playerPortrait,245+i*300,230,180,180);
                var button=Button(page,(i+1)+"  "+names[i],190+i*300,445,280,66,()=>StartEmote(index));if(i==0)Focus(button);
            }
        }
        void StartEmote(int index){Play();Game.Player.GetComponent<PlayerEmotes>().Play(index);}
    }
}
