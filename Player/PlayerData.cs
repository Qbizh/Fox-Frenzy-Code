using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct PlayerData
{
    public string userName;
    public int playerIndex;

    public PlayerData(string name, int index)
    {
        userName = name;
        playerIndex = index;
    }
}
