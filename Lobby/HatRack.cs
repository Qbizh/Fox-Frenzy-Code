using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class HatRack : NetworkBehaviour
{

    int index = 1;

    [Command(requiresAuthority = false)]
    public void ExchangeHat(PlayerController player)
    {
        player.equippedHat = (HatType)index;

        index = index + 1 > Enum.GetNames(typeof(HatType)).Length - 3 ? 0 : index + 1;
    }
}
