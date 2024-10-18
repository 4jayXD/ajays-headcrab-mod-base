using UnityEngine;
using HeadcrabMod;

namespace Example
{
    public class CoolZombieBehaviour : ZombieBehaviourBase 
    {
        public override void GetAssets() //set up asset variables here.
        {
        
        }

        public override void Start()  // calls when added to scene.
        {
            base.Start();
        }

        public override void Update() // calls every frame.
        {
            base.Update();
        }

        public override void OnDeath() //calls when zombie dies.
        {
        
        }
    }
}
