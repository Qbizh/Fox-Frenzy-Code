using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Mirror;
using System;
public class BombBehavior : EggBehavior
{
    // Start is called before the first frame update
    public Boolean broken = false;
    [ServerCallback]
    public override void Break(PlayerController hitPlayer)
    {
        if (!broken)
        {
            broken = true;
            Debug.Log("Boom");
            Collider2D[] hitObjects = Physics2D.OverlapCircleAll(gameObject.transform.position, 4f);
            for (int i = 0; i < hitObjects.Length; i++)
            {

                if (hitObjects[i].gameObject.CompareTag("Player"))
                {
                    NetworkIdentity playerIdentity = hitObjects[i].gameObject.GetComponent<NetworkIdentity>();
                    Debug.Log(playerIdentity.ToString());
                    if (playerIdentity != null && playerIdentity.connectionToClient != null)
                    {
                        int explodeDir = (int)Mathf.Sign((hitObjects[i].transform.position - transform.position).x);

                        //TargetSendHit(playerIdentity.connectionToClient);
                        PlayerController player = hitObjects[i].gameObject.GetComponent<PlayerController>();

                        player.state = PlayerController.PlayerState.Hurt;
                        player.HurtLocalPlayer(playerIdentity.connectionToClient, new Vector2(30 * explodeDir, 20));

                    }
                    else
                    {
                        Debug.LogWarning("Player identity or connection is null");
                    }
                }
            }
            GetComponent<Rigidbody2D>().velocity = Vector3.zero;
            GetComponent<Rigidbody2D>().isKinematic = true;
            GetComponent<Animator>().SetTrigger("Explode");
            
        }
        /*GetComponent<BaseEgg>().ResetOwner();
        NetworkServer.UnSpawn(gameObject);
        PrefabPool.instance.Return(gameObject);*/

    }

    public override void ClientDestroy()
    {
        Debug.Log("Ran break code");
        GetComponent<Animator>().SetTrigger("Explode");
    }

}
