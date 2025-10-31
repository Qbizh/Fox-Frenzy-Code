using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Mirror;
public class BasicBehavior : EggBehavior
{
    // Start is called before the first frame update
    [Server]
    public override void Break(PlayerController hitPlayer)
    {
        Debug.Log("soirhg");
        NetworkServer.UnSpawn(gameObject);
        PrefabPool.instance.Return(gameObject);
    }
}
