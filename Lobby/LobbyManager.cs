using System;
using System.Collections;
using Mirror;
using TMPro;
using Unity.Services.Relay;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Utp;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager instance;

    RelayNetworkManager relayNetworkmanager;

    [SerializeField] GameObject startButton;
    [SerializeField] TMP_Text joinCodeText;

    [SerializeField] TMP_Text countdownText;

    [SerializeField] int countdownTime = 15;

    [SerializeField] VotingArea[] votingAreas;

    [SerializeField] Transform spawnPoint;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(this);
        } else
        {
            instance = this;
        }

        startButton.SetActive(NetworkManager.singleton.mode == NetworkManagerMode.Host);

        joinCodeText.text = "Join Code\n" + NetworkManager.singleton.GetComponent<RelayNetworkManager>().relayJoinCode;
    }

    [Server]
    private void Start()
    {
        foreach (PlayerController player in GameDataHolder.instance.players.Values)
        {
            player.usernameEnabled = true;
            player.SpawnPlayer(spawnPoint.position, false);
        }
    }

    [ServerCallback]
    private void StartGame()
    {
        VotingArea votedMap = null;
        int mostVotes = 0;

        for (int i = 0; i < votingAreas.Length; i++)
        {
            if (votingAreas[i].votes > mostVotes)
            {
                votedMap = votingAreas[i];
                mostVotes = votingAreas[i].votes;
            }
        }

        if (!GameDataHolder.instance.inGame)
        {
            GameDataHolder.instance.StartNewGame();
        }

        if (votedMap != null) 
        {
            NetworkServer.SetAllClientsNotReady();
            NetworkManager.singleton.ServerChangeScene(votedMap.level);
        }
    }

    public void OnStartPressed()
    {
        StartCountdown();
    }

    [Command(requiresAuthority = false)]
    public void StartCountdown()
    {
        StartCountdownRpc();

        StartCoroutine(CountdownRoutine(true));
    }

    [Command(requiresAuthority = false)]
    public void AddPlayer(PlayerController player, string username)
    {
        for (int i = 0; i <= 3; i++) 
        {
            PlayerController existingPlayer =null;

            GameDataHolder.instance.players.TryGetValue(i, out existingPlayer);

            if (existingPlayer == null)
            {
                GameDataHolder.instance.players.Add(i, player);

                player.playerData = new PlayerData(username, i);

                player.SpawnPlayer(spawnPoint.position, false);


                /*Debug.Log("Subscribed");
                player.GetComponent<CameraController>().transitionOver += OnTransitionOver;*/

                break;
            }
        }
    }

    [Command(requiresAuthority = false)]
    public void OnTransitionOver(int playerId)
    {
        Debug.Log("TRansitioned dued");

        if (isServer)
        {
            GameDataHolder.instance.players[playerId].movementEnabled = true;
        }
    }

    [ClientRpc]
    private void StartCountdownRpc() 
    {
        StartCoroutine(CountdownRoutine(false));
    }

    IEnumerator CountdownRoutine(bool runOnServer)
    {
        countdownText.gameObject.SetActive(true);

        for (int i = countdownTime; i > 0; i--) 
        {
            countdownText.text = "Voting Ends In: " + i.ToString() + "s";
            yield return new WaitForSeconds(1);
        }

        countdownText.gameObject.SetActive(false);

        if (runOnServer) 
        {
            Debug.Log("Server");
            StartGame();
        }
    }
}