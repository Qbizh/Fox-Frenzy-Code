using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Mirror;
public class LandmineBehavior : EggBehavior
{
    // Start is called before the first frame update
    [Server]
    public override void Break(PlayerController hitPlayer)
    {
        GameObject trapInstance = Instantiate(PrefabPool.instance.landMine, transform.position, new Quaternion(0, 0, 0, 0));
        NetworkServer.Spawn(trapInstance);
        NetworkServer.UnSpawn(gameObject);
        PrefabPool.instance.Return(gameObject);
    }
}
