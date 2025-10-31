using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;

public class BaseEgg : NetworkBehaviour
{

    [SyncVar(hook = nameof(UpdateBehavior))] [SerializeField] public EggState state;

    public EggType type;

    [SerializeField] TrailRenderer trailEffect;

    [SerializeField] float despawnTime;

    [SyncVar (hook = nameof(UpdateMystery))] public bool isMystery;

    [SerializeField] int interactableLayer;

    [SerializeField] LayerMask groundMask;

    [SyncVar] public PlayerController owner;

    [SerializeField] Sprite[] eggSprites;

    [SerializeField] FMODUnity.EventReference BombSFX;

    [SerializeField] FMODUnity.EventReference CrackSFX;

    [SerializeField] FMODUnity.EventReference AirstrikeSFX;

    [SerializeField] FMODUnity.EventReference yolkSFX;

    public enum EggState
    {
        Fired,
        Idle,
        Carried,
        Thrown,
    }
    [ServerCallback]
    public void ServerInitialize(EggType newType, bool defaultType)
    {
        int random = UnityEngine.Random.Range(0, 8);
        if (random == 7)
        {
            isMystery = true;
        }

        Initialize(newType, defaultType);
        ClientInit(newType, defaultType);
    }


    private void Update()
    {
        if (isServer && owner != null && state == EggState.Carried)
        {
            transform.position = owner.transform.position;
        }
    }

/*    IEnumerator DespawnRoutine()
    {
        yield return new WaitForSeconds(despawnTime);

        if (state != EggState.Carried && state != EggState.Thrown)
        {
            if (isServer)
            {
                NetworkServer.UnSpawn(gameObject);
                PrefabPool.instance.Return(gameObject);
            } else
            {
                Destroy(gameObject);
            }
        }
    }*/

    private void Initialize(EggType newType, Boolean defaultType)
    {

        /*StartCoroutine(DespawnRoutine());*/

        owner = null;

        type = newType;
        //Get sprite renderer and set sprite
        if (TryGetComponent(out EggBehavior eggBehavior)) 
        {
            Destroy(eggBehavior);
        }
        //gameObject.GetComponent<SpriteRenderer>().sprite = eggDataTypes[(int)newData.type].sprite;
        if (isMystery)
        {
            GetComponent<Animator>().SetInteger("Type", 5);
        } else
        {
            GetComponent<Animator>().SetInteger("Type", (int)type);
        }
        
        if (defaultType)
        {
            UpdateState(EggState.Fired);

        }
        
        switch (type)
        {

            case EggType.Bomb:
                this.gameObject.AddComponent<BombBehavior>();
                break;
            case EggType.Yolk:
                this.gameObject.AddComponent<YolkBehavior>();
                break;
            case EggType.BearTrap:
                this.gameObject.AddComponent<BearTrapBehavior>();
                break;
            case EggType.Airstrike:
                this.gameObject.AddComponent<AirstrikeBehavior>();
                break;
            case EggType.Landmine:
                this.gameObject.AddComponent<LandmineBehavior>();
                break;
        }
    }

    [ClientRpc]
    private void ClientInit(EggType newType, bool defaultType) {
        Initialize(newType, defaultType);
    }

    void UpdateBehavior(EggState oldState, EggState newState)
    {
        trailEffect.Clear();

        //Debug.Log("Updated behavior to " + newState);
        switch (newState)
        {
            case EggState.Fired:
                if (NewChicken.instance.GetOwner() != null)
                {
                    Physics2D.IgnoreCollision(GetComponent<Collider2D>(), NewChicken.instance.GetOwner().GetComponent<Collider2D>(), true);
                }

                trailEffect.enabled = true;

                EnableCollision(); break;
            case EggState.Idle:

                trailEffect.enabled = false;

                if (oldState == EggState.Fired)
                {
                    if (NewChicken.instance.GetOwner() != null)
                    {
                        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), NewChicken.instance.GetOwner().GetComponent<Collider2D>(), false);
                    }
                }

                DisableCollision(); break;
            case EggState.Carried:
                trailEffect.enabled = false;

                //Debug.Log("Collision off");
                DisableCollision(); break;
            case EggState.Thrown:
                trailEffect.enabled = true;

                if (owner != null) {
                    Physics2D.IgnoreCollision(GetComponent<Collider2D>(), owner.GetComponent<Collider2D>(), true);
                }
                
                EnableCollision(); break;
        }
    }
    [ServerCallback]
    void UpdateState(EggState newState)
    {
        //Debug.Log("Updated state to " + newState);
        state = newState;
        
    }

    [ServerCallback]
    void UpdateHeldPos()
    {
        if (state == EggState.Carried && owner)
        {
            transform.position = owner.transform.position;
            /*transform.position = owner.HoldTransform.position;
            transform.rotation = owner.HoldTransform.rotation;*/
        }
    }

    [Command(requiresAuthority = false)]
    public void ResetOwner()
    {
        owner = null;
    }

    void DisableCollision()
    {
        //GetComponent<Rigidbody2D>().simulated = true;
        //GetComponent<NetworkRigidbodyReliable2D>().enabled = false;
        gameObject.layer = interactableLayer;
    }
    void EnableCollision()
    {
        //GetComponent<Rigidbody2D>().simulated = true;
        //GetComponent<NetworkRigidbodyReliable2D>().enabled = true;
        gameObject.layer = 0;
    }

    [ClientRpc]
    void HideEgg(bool visible)
    {
        GetComponent<Rigidbody2D>().simulated = visible;
        GetComponent<SpriteRenderer>().enabled = visible;
        GetComponent<Collider2D>().enabled = visible;
    }
    
    void BreakLogic(PlayerController hitPlayer)
    {
        switch (type)
        {
            case EggType.Bomb:
                FMODUnity.RuntimeManager.PlayOneShot(BombSFX, transform.position);
                break;
            case EggType.Airstrike:
                FMODUnity.RuntimeManager.PlayOneShot(AirstrikeSFX, transform.position); 
                break;
            case EggType.Yolk:
                FMODUnity.RuntimeManager.PlayOneShot(yolkSFX, transform.position);
                break;
            default:
                FMODUnity.RuntimeManager.PlayOneShot(CrackSFX, transform.position);
                break;
        }
        if (isServer)
        {
            GetComponent<EggBehavior>().Break(hitPlayer);
        } else
        {
            GetComponent<EggBehavior>().ClientDestroy();
        }
        
    }

    
    public void OnEffectDone()
    {
        if (isServer)
        {
            NetworkServer.UnSpawn(gameObject);
            PrefabPool.instance.Return(gameObject);
        } else
        {
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
            
        }
        
    }
    [ClientRpc]
    public void PlayAirstirke()
    {
        FMODUnity.RuntimeManager.PlayOneShot("event:/AirStrikeAlarm");
    }


    //Terrible hack, rewuite later i think?
    [Command(requiresAuthority = false)]
    public void Throw(Vector2 throwVel)
    {
        Debug.Log("Threw");
        //owner.heldObject = null;
        //owner = null;

        HideEgg(true);

        UpdateState(EggState.Thrown);
        //GetComponent<NetworkIdentity>().RemoveClientAuthority();
        transform.SetParent(NetworkManager.singleton.transform, true);
        GetComponent<Rigidbody2D>().velocity = throwVel; 

    }

    [ServerCallback]
    public void PickUp(PlayerController player)
    {
        Debug.Log("Picked up");
        UpdateState(EggState.Carried);
        //NetworkConnectionToClient playerConnection = );
        //NetworkIdentity playerConn = player.GetComponent<NetworkIdentity>();
        //DisableCollision();

        HideEgg(false);

        owner = player;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        
        switch (state)
        {
            case EggState.Fired:
                if (collision.gameObject.CompareTag("Player"))
                {
                    BreakLogic(collision.gameObject.GetComponent<PlayerController>()); 
                    break;
                } else if ((groundMask.value & 1 << collision.gameObject.layer) > 0)
                {
                    //Debug.Log("Right before idle");
                    UpdateState(EggState.Idle);
                }
                
                break;
            case EggState.Idle:
                //I don't think anything happens here?
                //PickUp();
                break;
            case EggState.Carried:
                //if this happens i think something fucked up

                break;
            case EggState.Thrown:
                /*if (owner != null)
                {
                    Physics2D.IgnoreCollision(GetComponent<Collider2D>(), owner.GetComponent<Collider2D>(), false);
                    //ResetOwner();
                }*/

                BreakLogic(collision.gameObject.GetComponent<PlayerController>());
                break;
        }
    }

    void UpdateMystery(bool oldMystery, bool newMystery)
    {
        if (newMystery)
        {
            GetComponent<Animator>().SetInteger("Type", 5);
        }
    }
}
