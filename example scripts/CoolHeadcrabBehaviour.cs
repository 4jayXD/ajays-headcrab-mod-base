using UnityEngine;
using HeadcrabMod; // make sure this is here too

namespace Example 
{
  public class CoolHeadcrabBehaviour : HeadcrabBehaviourBase //Make sure name of class matches file. & extend off of the HeadcrabBehaviourBase.
  {
    [SkipSerialisation]
    public Sprite 
      attached_sprite, 
      detached_sprite;

    public override void GetAssets() // set up assets in this function. 
    {
      
    } 
    
    public override void Start() 
    {
      base.Start(); //make sure to have this here.
      SetSprite(detached_sprite); // sets the sprite to the specified sprite & resets the hitbox & outline :).

      // PlaySound(Audioclip) // plays audioclip specified. But if there's a sound playing, it won't play.

      // PlaySound(Audioclip, true) // plays the audioclip specified. & cancels out current clip playing.
    }

    public override void Update() 
    {
      base.Update();//make sure to have this here
      if (!Dead) 
      {
        switch (State) 
        {
          case HeadcrabStates.Unattached: // Updates every frame when the headcrab is not attached to a person.
          
            break;

          case HeadcrabStates.Attaching; // Updates every frame when the headcrab is attaching to a person.
          
            break;

          case HeadcrabStates.Attached: // Updates every frame when the headcrab is attached to a person.
          
            break;
        }
      }
    }

    public override void Attach(GameObject host) // host = person being fucked up. Also Attaches the crab to the person.
    {
      base.Attach(host); // make sure to leave this here. Otherwise it will break.
      SetSprite(attached_sprite)
    }

    public override void Detach() 
    {
      base.Detach(); // make sure to leave this here. Otherwise it will break.
      SetSprite(detached_sprite)
    }

    public override void Transform() // function that runs when attachment is complete. Have fun adding your own logic that determines what zombie behaviour to add lol
    {
      zombieBehaviour = Host.AddComponent<CoolZombieBehaviour>();
    }

    public override void OnDeath() // when crab dies, this is run.
    {
      SetSprite(detached_sprite)
    }
  }
}
