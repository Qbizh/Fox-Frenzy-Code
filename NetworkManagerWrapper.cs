using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Utp;

public class NetworkManagerWrapper : RelayNetworkManager
{
    /*public override void OnClientConnect()
    {
        base.OnClientConnect();

        //StartCoroutine(WaitForLobbyManager());
    }*/

    public override void OnServerSceneChanged(string newSceneName)
    {
        base.OnServerSceneChanged(newSceneName);

        Debug.Log(newSceneName);

        if (newSceneName != "Menu")
        {
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                NetworkServer.SetClientReady(connection);
            }
        }
    }
}
