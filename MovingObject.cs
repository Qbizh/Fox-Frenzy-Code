using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class MovingObject : NetworkBehaviour
{
    private void Start()
    {
        if (isServer)
        {
            GetComponent<Animator>().enabled = true;
        }
    }
}
