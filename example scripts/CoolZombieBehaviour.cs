using UnityEngine;
using HeadcrabMod;

namespace Example
{
    public class CoolZombieBehaviour : ZombieBehaviourBase 
    {
        [SkipSerialisation]
        public Sprite claw;
        
        public override void GetAssets() //set up asset variables here.
        {
        
        }

        public override void Start()  // calls when added to scene.
        {
            foreach(var limb in hostBehaviour.Limbs) // do this to add exta functionallity to the zombie.
            {
                limb.gameObject.addComponent<ZombieLimbBehaviour>();
            }
            
            base.Start();
        }

        public override void Update() // calls every frame.
        {
            base.Update();

            foreach(var limb in ZombieLimbs) 
            { 
                limb.GenerateClaw(claw) // generates the claws. make sure to specify the lower arms tho. because it will just add a claw to every limb. Computers be like that.
            }
            
        }

        public override void OnDeath() //calls when zombie dies.
        {
        
        }

        public class ZombieLimbBehaviour : LimbBehaviourBase  // do this to add exta functionallity to the zombie.
        {
            
        }
    }
}
