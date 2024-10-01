using UnityEngine;
using UnityEngine.Events;

namespace HeadcrabMod
{
    public abstract class ZombieBehaviourBase : MonoBehaviour
    {
        public bool Crawling, Alerted = false;

        public bool Dead = false;


        public bool Spawned = false;
        public virtual string GetZombieType() => "None"; 
        public virtual string GetBloodID() => HeadcrabBlood.ID;

        public PersonBehaviour hostBehaviour;
        public HeadcrabBehaviourBase headcrabBehaviour;
        public GameObject Host, Headcrab;
        public LimbBehaviourBase[] ZombieLimbs;
        public LimbBehaviour Head;
        public AudioSource MainAudioSource;
        public float EatenBodyParts;

        public float transformationTime {  get; private set; }
        public float maxTransformationTime { get; private set; }

        public bool Transforming 
        {
            get 
            {
                return transformationTime > 0;
            }
        }

        public bool 
            Sleeping;

        public virtual bool CanMutate() => false;
        public virtual bool CanAutoWake() => true;

        public bool isOnFire
        {
            get
            {
                foreach (var limb in hostBehaviour.Limbs)
                    if (limb.PhysicalBehaviour.OnFire) return true;

                return false;
            }
        }

        public virtual void WakeUp(bool Ov = false) 
        {
            if (!Sleeping) return;
            if (CanAutoWake() || Ov) 
            {
                Sleeping = false;
                OnAwake();
            }           
        }

        public void transformHost(float transformationTime, bool putToSleep = true, bool toGonome = false) 
        {
            this.transformationTime = transformationTime;
            maxTransformationTime = transformationTime;
            Sleeping = putToSleep;
        }

        public ZombieStates State = ZombieStates.Idle;

        public void PlaySound(AudioClip clip, bool StopCurrentSound = false, float pitch = 1, float volume = 1) 
        {
            if (!StopCurrentSound && MainAudioSource.isPlaying) return;

            if (Global.main.Paused) return;

            MainAudioSource.Stop();
            MainAudioSource.enabled = true;
            MainAudioSource.clip = clip;
            if (Global.main.SlowMotion) MainAudioSource.pitch = pitch * Global.main.SlowmotionTimescale;
            else MainAudioSource.pitch = pitch;
            MainAudioSource.volume = volume;
            MainAudioSource.Play();
        }

        public abstract void GetAssets();
        public virtual void Start()
        {
            GetAssets();
            hostBehaviour = GetComponent<PersonBehaviour>();
            MainAudioSource = hostBehaviour.transform.Find("Head").GetComponent<PhysicalBehaviour>().MainAudioSource;
            Host = gameObject;

            
            foreach (var l in hostBehaviour.Limbs) 
            {
                if (l.HasBrain) Head = l;

                l.BloodLiquidType = GetBloodID();

                float bloodAmountInBody = l.CirculationBehaviour.GetAmount(Liquid.GetLiquid(Blood.ID));
                l.CirculationBehaviour.RemoveLiquid(Liquid.GetLiquid(Blood.ID), bloodAmountInBody);
                l.CirculationBehaviour.AddLiquid(Liquid.GetLiquid(GetBloodID()), bloodAmountInBody);

                l.SpeciesIdentity = "Zombie";    

                if (l.CirculationBehaviour.GetAmount(Liquid.GetLiquid(NeuralStunner.ID)) > 0)
                {
                    float amount = l.CirculationBehaviour.GetAmount(Liquid.GetLiquid(NeuralStunner.ID));
                    l.CirculationBehaviour.RemoveLiquid(Liquid.GetLiquid(NeuralStunner.ID), amount);
                    l.CirculationBehaviour.AddLiquid(Liquid.GetLiquid(GetBloodID()), amount);
                }
            }       
        }

        public virtual void Update() 
        {
            if (hostBehaviour.IsAlive())
            {
                
                if (Transforming) 
                {
                    transformationTime -= 1 * Time.deltaTime;

                    if (!Sleeping) 
                    {
                        transformationTime = 0;
                    }
                }
                else if (Sleeping) WakeUp();


                if (Sleeping || Transforming)
                {
                    hostBehaviour.Consciousness = 0;
                    foreach (LimbBehaviour limb in hostBehaviour.Limbs)
                    {
                        limb.Numbness = 1;
                        limb.BaseStrength = 0;
                        limb.BloodMuscleStrengthRatio = 0;
                    }

                    if (State != ZombieStates.Idle)
                        State = ZombieStates.Idle;
                }
                else 
                {
                    hostBehaviour.Consciousness = 1;
                    foreach (LimbBehaviour limb in hostBehaviour.Limbs)
                    {
                        limb.Numbness = 0;
                        limb.BaseStrength = 10;
                        limb.BloodMuscleStrengthRatio = 1.5f;
                    }
                }
                
            }
            else if (!Dead) Kill(); 
        }

        public void Kill() 
        {
            foreach(var limb in hostBehaviour.Limbs) 
            {                
                if (limb.CirculationBehaviour.IsPump)                
                    limb.CirculationBehaviour.IsPump = false;

                limb.Numbness = 1;
            }
            if (ZombieLimbs != null) ToggleClaws();
            if ((bool)headcrabBehaviour) headcrabBehaviour.Detach();

            hostBehaviour.Consciousness = 0;

            Dead = true;
            OnDeath();
        }
        public virtual void OnAwake() { }
        public virtual void OnDeath() { }

        public void ToggleClaws() 
        {
            foreach (var limb in hostBehaviour.Limbs) 
            {
                if (limb.name.Contains("LowerArm"))                
                    limb.GetComponent<LimbBehaviourBase>().ToggleClaw();  
            }
        }
        
        public abstract class LimbBehaviourBase : MonoBehaviour
        {
            public bool hasBrain, hasClaws, hasMouth = false;

            private UnityAction a_clawToggle, a_mouthToggle;

            public bool clawsGenerated = false;
            public PersonBehaviour HostBehaviour;
            public LimbBehaviour Limb;
            public ZombieBehaviourBase ZombieBehaviour;
            public string ZombieType;

            void PlaySound(AudioClip clip, bool cancelCurrentSound = false)
            {
                var mainAudioSource = Limb.PhysicalBehaviour.MainAudioSource;

                if (!cancelCurrentSound && mainAudioSource.isPlaying) return;
                mainAudioSource.enabled = true;
                mainAudioSource.clip = clip;
                mainAudioSource.Play();
            }

            public virtual void Start()
            {
                a_clawToggle += ToggleClaw;
                a_mouthToggle += ToggleMouth;

                Limb = gameObject.GetComponent<LimbBehaviour>();
                HostBehaviour = Limb.Person;
                ZombieBehaviour = HostBehaviour.gameObject.GetComponent<ZombieBehaviourBase>();

                if (gameObject.name.Contains("LowerArm"))
                {
                    if (Limb.gameObject.HasComponent<GripBehaviour>())
                    {
                        if (gameObject.GetComponent<GripBehaviour>().isHolding)
                            gameObject.GetComponent<GripBehaviour>().DropObject();
                        Destroy(Limb.gameObject.GetComponent<GripBehaviour>());

                        Limb.PhysicalBehaviour.ContextMenuOptions.Buttons.Add(new ContextMenuButton("Claw Toggle", "Claw Toggle", "Toggles claws.", a_clawToggle));

                    }
                    hasClaws = true;
                    return;
                }
                if (gameObject.name.Contains("MiddleBody"))
                {
                    hasMouth = true;

                    Limb.PhysicalBehaviour.ContextMenuOptions.Buttons.Add(new ContextMenuButton("Mouth Toggle", "Mouth Toggle", "Toggles mouth.", a_mouthToggle));
                }
                if (Limb.HasBrain)
                    hasBrain = true;
            }
            public virtual void Update()
            {
                if (HostBehaviour.IsAlive())
                {
                    if (Limb.Health < .5f)
                    {
                        Limb.CirculationBehaviour.HealBleeding();
                        Limb.CirculationBehaviour.AddLiquid(Liquid.GetLiquid(ZombieBehaviour.GetBloodID()), .025f);
                        ZombieBehaviour.EatenBodyParts -= .15f;
                    }

                    if (Limb.IsDismembered && hasClaws) hasClaws = false;
                }
            }

            public virtual void OnCollisionEnter2D(Collision2D col)
            {
                if (hasMouth && Limb.IsConsideredAlive && col.gameObject.HasComponent<LimbBehaviour>())
                    MouthBehaviour(col.gameObject.GetComponent<LimbBehaviour>());
                if (hasClaws && HostBehaviour.IsAlive() && col.gameObject.HasComponent<LimbBehaviour>())
                    ClawBehaviour(col.gameObject.GetComponent<LimbBehaviour>());

            }
            public virtual void MouthBehaviour(LimbBehaviour limb)
            {
                if (!limb.Person.IsAlive() || limb.IsDismembered)
                {
                    limb.CirculationBehaviour.RemoveLiquid(Liquid.GetLiquid(Blood.ID), .025f);
                    Limb.CirculationBehaviour.AddLiquid(Liquid.GetLiquid(Blood.ID), .025f);
                    limb.Crush();
                    ZombieBehaviour.EatenBodyParts += 1f;
                }
                return;
            }
            public virtual void ClawBehaviour(LimbBehaviour victim)
            {
                if (hasClaws) 
                {
                    ModAPI.CreateParticleEffect("BloodExplosion", victim.transform.position);
                    victim.Damage(15f);

                    if (victim.Health < .3f && !victim.IsDismembered)
                    {
                        if (victim.Broken)
                        {
                            victim.Slice();
                            return;
                        }

                        switch (new System.Random().Next(0, 1))
                        {
                            case 0: victim.Slice(); return;
                            case 1: victim.BreakBone(); return;
                        }
                    }
                }
            }


            public GameObject GenerateClaw(Sprite claw, float offset = -.3f)
            {
                GameObject Claw = new GameObject("Claw");

                if (Limb.Person.AngleOffset != Claw.transform.localScale.x)
                    Claw.transform.localScale = new Vector2(Claw.transform.localScale.x * -1, Claw.transform.localScale.y);

                Claw.transform.parent = transform;
                Claw.transform.localPosition = Vector3.zero + new Vector3(0, offset, 0);
                Claw.transform.rotation = Limb.transform.rotation;
                Claw.AddComponent<SpriteRenderer>();
                Claw.GetComponent<SpriteRenderer>().sprite = claw;
                Claw.GetComponent<SpriteRenderer>().sortingOrder = Limb.GetComponent<SpriteRenderer>().sortingOrder;

                return Claw;
            }

            public void ToggleClaw() 
            {
                if (!ZombieBehaviour.Dead)
                {
                    if (hasClaws) hasClaws = false;
                    else hasClaws = true;
                }
                else hasClaws = false;
            }
            public void ToggleMouth() 
            {
                if (!ZombieBehaviour.Dead)
                {
                    if (hasMouth) hasMouth = false;
                    else hasMouth = true;
                }
                else hasMouth = false;
            }
        }
    }  

    public enum ZombieStates 
    {
        Idle,
        Agrivated
    }
}
