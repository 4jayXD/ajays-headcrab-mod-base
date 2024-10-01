using System.Diagnostics.Eventing.Reader;
using UnityEngine;
using UnityEngine.Events;


namespace HeadcrabMod
{
    public abstract class HeadcrabBehaviourBase : AliveBehaviour
    {
        public virtual float GetAttachmentDelay() => 1f;
        public virtual Vector3 GetAttachmentAnchor() => Vector3.zero;
        public virtual bool CanAttachToSpecies(string identity) 
        {
            if (identity == "Human") return true;

            return false;
        }

        public UnityAction a_detach;

        public virtual bool Spawned 
        {
            get 
            {
                return (bool)bodyBehaviour; 
            }
        }

        public bool Dead = false;
        protected float Timer;
        [SkipSerialisation]
        protected float angleOffset = 0;
        public HeadcrabStates State = HeadcrabStates.Unattached;

        public BodyBehaviour bodyBehaviour;       
        public AudioSource MainAudioSource;
        public ZombieBehaviourBase zombieBehaviour;
        public Sprite currentSprite;
        
        public GameObject
            Headcrab, 
            Host;

        public abstract void GetAssets();
        public void SetSprite(Sprite sprite) 
        {
            if ((bool)Headcrab) 
            {
                var renderer = Headcrab.GetComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                Headcrab.GetComponent<BoxCollider2D>().size = sprite.bounds.size;
                Headcrab.GetComponent<PhysicalBehaviour>().RefreshOutline();
                currentSprite = sprite;
            }
        }
        public void PlaySound(AudioClip sound, bool stopCurrentSound = false, float pitch = 1, float volume = 1)
        {
            if (stopCurrentSound) MainAudioSource.Stop();
            else if (MainAudioSource.isPlaying) return;

            if (Global.main.Paused) return;

            MainAudioSource.enabled = true;
            MainAudioSource.clip = sound;
            if (Global.main.SlowMotion) MainAudioSource.pitch = pitch * Global.main.SlowmotionTimescale;
            else MainAudioSource.pitch = pitch;
            MainAudioSource.volume = volume;
            MainAudioSource.Play();
        }

        public override bool IsAlive() 
        {
            return bodyBehaviour.isConsideredAlive;
        }

        public virtual void Start()
        {
            GetAssets();

            Headcrab = gameObject;
            MainAudioSource = Headcrab.GetComponent<PhysicalBehaviour>().MainAudioSource;
            angleOffset = Headcrab.transform.localScale.x;

            a_detach += Detach;

            var physicalBehaviour = Headcrab.GetComponent<PhysicalBehaviour>();
            physicalBehaviour.Properties = ModAPI.FindPhysicalProperties("Human");
            physicalBehaviour.HoldingPositions = null;
            physicalBehaviour.DisplayBloodDecals = false;
            physicalBehaviour.StabCausesWound = false;
            physicalBehaviour.ContextMenuOptions.Buttons.Add(new ContextMenuButton("Detach", "Detach", "Detach Headcrab From Host.", a_detach));

            if (!Spawned)
            {
                bodyBehaviour = Headcrab.AddComponent<BodyBehaviour>();
                bodyBehaviour.circulationBehaviour = gameObject.AddComponent<BodyCirculationBehaviour>();
            }

        }   
        public virtual void Update()
        {
            if (Host == null && State != HeadcrabStates.Unattached) Detach();

            if (IsAlive()) 
            {
                if ((bool)Host && !Host.GetComponent<PersonBehaviour>().IsAlive())               
                    Detach();              

                if (State == HeadcrabStates.Attaching && Timer <= 0) 
                {
                    Timer = 0;
                    Transform();
                    State = HeadcrabStates.Attached;
                }
                else Timer -= 1 * Time.deltaTime;

                if ((bool)Host && Host.GetComponent<PersonBehaviour>().Consciousness < .6f && State == HeadcrabStates.Attaching) 
                {
                    Transform();
                    State = HeadcrabStates.Attached;
                    if (Timer > 0) Timer = 0;
                }
            }  
            else if (!Dead) Kill();
        }

        void OnDestroy() 
        {
            if ((bool)Host && Host.GetComponent<PersonBehaviour>().IsAlive())
            {
                if ((bool)zombieBehaviour) zombieBehaviour.Kill();
                else Detach();
            }
        }
        public virtual void OnCollisionEnter2D(Collision2D collider)
        {       
            if (IsAlive() && State == HeadcrabStates.Unattached && collider.gameObject.name == "Head" && CanAttachToSpecies(collider.gameObject.GetComponent<LimbBehaviour>().SpeciesIdentity))
            {
                Attach(collider.gameObject.transform.parent.gameObject);
            }
        }

        public virtual void Attach(GameObject host)
        {
            Timer = GetAttachmentDelay();
            if (!(bool)zombieBehaviour)
                State = HeadcrabStates.Attaching;
            else
                State = HeadcrabStates.Attached;

            foreach (var col in host.GetComponentsInChildren<Collider2D>())
                Physics2D.IgnoreCollision(col, Headcrab.GetComponent<Collider2D>());

            GameObject head = host.transform.Find("Head").gameObject;
            Headcrab.GetComponent<Rigidbody2D>().mass = head.GetComponent<Rigidbody2D>().mass;

            FixedJoint2D Attacher = Headcrab.AddComponent<FixedJoint2D>();
            Headcrab.transform.position = head.transform.position + GetAttachmentAnchor();
            Headcrab.transform.rotation = head.transform.rotation;
            Attacher.connectedBody = head.GetComponent <Rigidbody2D>();
            head.GetComponent<LimbBehaviour>().Frozen = true;
            head.GetComponent<SpriteRenderer>().sortingLayerName = "Background";
            Headcrab.transform.parent = head.transform;
            if (host.GetComponent<PersonBehaviour>().AngleOffset != angleOffset)
            {
                Headcrab.transform.localScale = new Vector2(Headcrab.transform.localScale.x * -1, Headcrab.transform.localScale.y);
                angleOffset = (int)host.GetComponent<PersonBehaviour>().AngleOffset;
            }
            
            if (!(bool)Host) Host = host;        
        }
        public virtual void Detach()
        {
            State = HeadcrabStates.Unattached;
            if ((bool)Host) 
            {
                if ((bool)zombieBehaviour) 
                {
                    zombieBehaviour.Kill();
                    zombieBehaviour.hostBehaviour.Consciousness = 0;
                    foreach(var l in zombieBehaviour.hostBehaviour.Limbs) 
                    {
                        l.Numbness = 1;
                        l.BaseStrength = 0;
                        l.Health = 0;
                        l.CirculationBehaviour.IsPump = false;
                        l.HasBrain = false;
                    } 
                }

                Headcrab.transform.parent = null;
                Host.transform.Find("Head").GetComponent<LimbBehaviour>().Frozen = false;
                Host.transform.Find("Head").GetComponent<SpriteRenderer>().sortingLayerName = "Default";
                zombieBehaviour = null;
                Host = null;
                Destroy(Headcrab.GetComponent<FixedJoint2D>());
            }
                    
        }
        public abstract void Transform();

        public void Kill() 
        {          
            if (!Dead) 
            {
                if ((bool)Host)
                {
                    if ((bool)zombieBehaviour) zombieBehaviour.Kill();
                    else Detach();
                }
                Dead = true;
                OnDeath();
            }
        }
        public virtual void OnDeath()
        {

        }
    }
    public class BodyBehaviour : MonoBehaviour, Messages.IShot, Messages.IStabbed
    {
        public float Health = 1f;
        public bool isConsideredAlive 
        {
            get 
            {
                return Health > .01f;
            }          
        }      
        public BodyCirculationBehaviour circulationBehaviour;

        [SkipSerialisation]
        public float 
            PainLevel, 
            ShockLevel;
        [SkipSerialisation]
        public float 
            OxygenLevel, 
            Conciousness = 1f;

        void Start() 
        {
                
        }


        public void Shot(Shot shot)
        {
            Health = 0;

        }
        public void Stabbed(Stabbing stabbing)
        {
            Health = 0;
        }
    }
    public class BodyCirculationBehaviour : BloodContainer, Messages.IShot, Messages.IExitShot, Messages.IStabbed, Messages.IUnstabbed
    {
        public string BloodLiquidID = HeadcrabBlood.ID;
        public HeadcrabBehaviourBase headcrabBehaviour;
        public BodyBehaviour bodyBehaviour;
        public ushort StabWoundCount, GunshotWoundCount = 0;
        public bool isBeingStabbed
        {
            get 
            {
                return gameObject.GetComponent<PhysicalBehaviour>().IsBeingStabbed;
            }
        }
        

        protected void Start() 
        {
            headcrabBehaviour = GetComponent<HeadcrabBehaviourBase>();
            bodyBehaviour = GetComponent<BodyBehaviour>();
            
            AddLiquid(Liquid.GetLiquid(BloodLiquidID), 1f);
        }

        protected override void Update()
        {
            base.Update();

            if ((bool)headcrabBehaviour.Host && headcrabBehaviour.Host.HasComponent<ZombieBehaviourBase>()) 
            {               
                switch (headcrabBehaviour.Host.GetComponent<ZombieBehaviourBase>().State) 
                {
                    case ZombieStates.Idle:

                        

                        break;
                }
            }
        }

        public void Shot(Shot shot)
        {
            GunshotWoundCount += 1;

            
        }
        public void ExitShot(Shot shot)
        {
            GunshotWoundCount += 1;
        }

        public void Stabbed(Stabbing stabbing)
        {

        }
        public void Unstabbed(Stabbing stabbing)
        {
            StabWoundCount += 1;
        }
    }

    public enum HeadcrabStates 
    {
        Unattached,
        Attaching,
        Attached
    }
}
