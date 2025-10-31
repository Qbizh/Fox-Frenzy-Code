using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class YolkBehavior : EggBehavior
{
    // Start is called before the first frame update
    [Server]

    public override void Break(PlayerController hitPlayer)
    {

        if (hitPlayer)
        {
            hitPlayer.ScheduleTrape(3, PlayerController.TrappedState.Slowed);
        } else
        {
            GameObject trapInstance = Instantiate(PrefabPool.instance.yolkTrap, transform.position, new Quaternion(0, 0, 0, 0));

            NetworkServer.Spawn(trapInstance);
        }

        NetworkServer.UnSpawn(gameObject);
        PrefabPool.instance.Return(gameObject);
    }



}
