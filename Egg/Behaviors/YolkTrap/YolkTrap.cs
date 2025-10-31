using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class YolkTrap : NetworkBehaviour
{
    // Start is called before the first frame update
    [SerializeField] LayerMask groundLayer;
    float timeToLive;
    PlayerController[] playersInTrap;

    void Start()
    {
        playersInTrap = new PlayerController[4];
        if (isServer)
        {
            timeToLive = 10f;
        }

        RaycastHit2D hit = Physics2D.Raycast((Vector2)transform.position + Vector2.up * 0.5f, Vector2.down, 2f, groundLayer);

        BoxCollider2D collider = GetComponent<BoxCollider2D>();

        if (hit.collider != null)
        {
            transform.up = hit.normal;
            //transform.position = hit.point + hit.normal * GetComponent<BoxCollider2D>().size.y / 4;

            Bounds hitColliderBounds = hit.collider.bounds;

            if (transform.localScale.x > hit.collider.bounds.size.x)
            {
                transform.localScale = new Vector2(hitColliderBounds.size.x, transform.localScale.y);

                transform.position = (Vector2)hit.transform.position + hit.normal * hitColliderBounds.size.y/2;
            } else
            {
                /*float distanceFromCenter = (hit.point - (Vector2)hitColliderBounds.center + (hit.normal * hitColliderBounds.size.y/2)).x;

                float distanceOver = Mathf.Abs(Mathf.Abs(distanceFromCenter) + transform.localScale.x / 2 - hitColliderBounds.size.x / 2);

                if (distanceOver > 0)
                {
                    Debug.Log(distanceOver);

                    transform.localScale = new Vector2(transform.localScale.x - distanceOver, transform.localScale.y);
                    transform.position += transform.right * distanceOver /2 * -Mathf.Sign(distanceFromCenter);
                }*/

                float distanceFromCenter = (hit.point - ((Vector2)hitColliderBounds.center + (hit.normal * hitColliderBounds.size.y / 2))).x;

                if (Mathf.Abs(distanceFromCenter) + transform.localScale.x/2 > hitColliderBounds.size.x/2)
                {
                    Debug.Log("RAAAH");
                    transform.position = (Vector2)hitColliderBounds.center + (Vector2)transform.right * (hitColliderBounds.size.x / 2 * Mathf.Sign(distanceFromCenter) - (collider.bounds.size.x / 2 * Mathf.Sign(distanceFromCenter))) + (Vector2)transform.up * hitColliderBounds.size.y / 2;
                } else
                {
                    transform.position = hit.point + (Vector2)transform.up * collider.bounds.size.y / 2;
                }
            }
        } else
        {
            NetworkServer.Destroy(gameObject);
            RpcDestroy();
            Destroy(gameObject);
        }
    }

    [ClientRpc]
    void RpcDestroy()
    {
        Destroy(gameObject);
    }
    // Update is called once per frame
    void Update()
    {
        if (isServer)
        {
            timeToLive -= Time.deltaTime;
            if (timeToLive <= 0)
            {
                for (int i = 0; i < playersInTrap.Length; i++)
                {
                    if (playersInTrap[i] != null)
                    {
                        playersInTrap[i].UpdateTrapState(PlayerController.TrappedState.None);
                        playersInTrap[i] = null;
                    }
                }
                NetworkServer.Destroy(gameObject);
                RpcDestroy();
                Destroy(gameObject);
            }
        }
    }
    /* [TargetRpc]
     void TargetNerfVelocity(NetworkConnection conn, PlayerController player)
     {
         player.GetComponent<Rigidbody2D>().velocity /= 4;
     }
 */
    /*private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.layer == 6 && !activatedTrap)
        {
            GetComponent<Rigidbody2D>().isKinematic = true;
            GetComponent<Rigidbody2D>().velocity = Vector3.zero;
            transform.position = new Vector3(transform.position.x, collider.transform.position.y + collider.bounds.size.y * 0.5f + 0.5f * GetComponent<BoxCollider2D>().bounds.size.y);
            activatedTrap = true;



        }
    }*/
    private void OnTriggerStay2D(Collider2D collider)
    {
        
        if (collider.gameObject.CompareTag("Player") && isServer)
        {
            if (playersInTrap[collider.gameObject.GetComponent<PlayerController>().playerData.playerIndex] != collider.gameObject.GetComponent<PlayerController>())
            {
                collider.gameObject.GetComponent<PlayerController>().UpdateTrapState(PlayerController.TrappedState.Slowed);

                //NetworkConnectionToClient playerConn = collider.gameObject.GetComponent<NetworkIdentity>().connectionToClient;

                playersInTrap[collider.gameObject.GetComponent<PlayerController>().playerData.playerIndex] = collider.gameObject.GetComponent<PlayerController>();
            }

            
        }
        


    }
    private void OnTriggerExit2D(Collider2D collider)
    {
        if (collider.gameObject.CompareTag("Player") && isServer)
        {
            collider.gameObject.GetComponent<PlayerController>().UpdateTrapState(PlayerController.TrappedState.None);
            playersInTrap[collider.gameObject.GetComponent<PlayerController>().playerData.playerIndex] = null;
        }
    }
}
