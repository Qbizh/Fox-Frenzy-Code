using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
public class CardBoardLogic : NetworkBehaviour
{
    // Start is called before the first frame update
    [SyncVar] Boolean ready = true;
    private void OnTriggerStay2D(Collider2D collision)
    {
        Debug.Log("Collision detected");
        if (isServer && collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Collided with player");
            if (collision.gameObject.GetComponent<PlayerController>().state == PlayerController.PlayerState.Dive)
            {
                if (ready)
                {
                    Debug.Log("Tackledd");
                    GetComponent<NetworkAnimator>().SetTrigger("Cardboard");
                    ready = false;
                }
                
            }
            
        }

    }

    void Ready()
    {
        ready = true;
    }
}
