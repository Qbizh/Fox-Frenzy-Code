using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class HitBoxWrapper : NetworkBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isLocalPlayer)
            transform.GetComponentInParent<PlayerController>().OnHitBoxEnter(other);
    }
}
