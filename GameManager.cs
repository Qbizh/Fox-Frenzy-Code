using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using FMODUnity;

public class GameManager : NetworkBehaviour
{
    [SerializeField]
    [Scene] string lobbyScene;
    bool gameRunning = true;

    //Player Timer Bars
    public static GameManager instance;

    [SerializeField] float timeToWin = 45f;

    //[SyncVar(hook = nameof(OnUpdateScore))]public SyncList<float> scores = new SyncList<float>();
    float[] scores = new float[4];

    [SerializeField] GameObject confettiEffect;

    [SerializeField] EventReference musicRef;
    private FMOD.Studio.EventInstance musicInstance;

    [SerializeField] EventReference conffetiSFX;

    [SerializeField] EventReference stickerSFX;

    [SerializeField] EventReference winSFX;

    [SerializeField] EventReference hatDescendSFX;

    [SerializeField] AnimationCurve musicSpeedCurve;

    float musicSpeed = 0;

    [Header("Game Ui")]

    [SerializeField] GameObject gameUi;

    [SerializeField] Slider[] playerSliders;

    [SerializeField] Image[] hatImages;
    [SerializeField] Sprite[] hatSprites;

    [SerializeField] Transform chickenMarker;

    [SerializeField] TMP_Text countdown;

    [SerializeField] Animator blackScreenAnimator;

    [Header("Win Ui")]
    [SerializeField] GameObject winScreen;

    [SerializeField] Image[] winHats;

    [SerializeField] GameObject[] playerWinCounters;

    [SerializeField] GameObject[] winCounters;

    [SerializeField] GameObject chickenDinnerText;

    //Name Tags
    [SerializeField] TextMeshProUGUI[] playerTags;

    bool[] readyPlayers;

    [SerializeField] Transform[] spawnPoints;

    GameDataHolder gameDataHolder;

    // Start is called before the first frame update
    void Start()
    {
        if (instance != null) { Destroy(gameObject); } else { instance = this; }

        gameDataHolder = GameDataHolder.instance;

        musicInstance = FMODUnity.RuntimeManager.CreateInstance(musicRef);
        musicInstance.start();

        for (int i = 0; i < playerSliders.Length; i++)
        {
            playerSliders[i].maxValue = timeToWin;
        }

        /*GameObject[] playerObjs = GameObject.FindGameObjectsWithTag("Player");

        players = new PlayerController[playerObjs.Length];*/

        foreach (int playerId in gameDataHolder.players.Keys) {
            //int playerId = player.GetComponent<PlayerController>().playerData.playerIndex;
            //players[playerId] = player.GetComponent<PlayerController>();
            // Debug.Log(player.GetComponent<PlayerController>().equippedHat != null);
            playerSliders[playerId].gameObject.SetActive(true);
            playerWinCounters[playerId].gameObject.SetActive(true);

            winHats[playerId].sprite = hatSprites[(int)gameDataHolder.players[playerId].equippedHat];
            winHats[playerId].transform.parent.gameObject.SetActive(true);

            hatImages[playerId].sprite = hatSprites[(int)gameDataHolder.players[playerId].equippedHat];

            for (int j = playerId * 3; j < playerId * 3 + gameDataHolder.wins[playerId]; j++)
            {
                Debug.Log(j + " Tried to set acitve");
                winCounters[j].SetActive(true);
            }

            //playerTags[playerId].text = players[playerId].playerData.userName;

            if (isServer)
            {
                //scores.Add(0);

                gameDataHolder.players[playerId].SpawnPlayer(spawnPoints[playerId].position, true);

                //player.GetComponent<CameraController>().transitionOver += OnClientTransitionOver;
            }
         }

         readyPlayers = new bool[gameDataHolder.players.Count];
    }

    private void OnDisable()
    {
        if (isServer)
        {
            foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
            {
                player.GetComponent<CameraController>().transitionOver -= OnClientTransitionOver;
            }
        }

        musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    }



    private void Update()
    {
        UpdateChickenMarker();
    }

    IEnumerator StartGame()
    {
        countdown.gameObject.SetActive(true);

        for (int i = 3; i > 0; i--) 
        {
            countdown.text = i.ToString();
            Debug.Log("Countdown: " + i);
            yield return new WaitForSeconds(1);
        }

        countdown.gameObject.SetActive(false);

        if (isServer)
        {
            for (int i = 0; i < gameDataHolder.players.Count; i++)
            {
                gameDataHolder.players[i].movementEnabled = true;
            }
        }
    }

    [Command(requiresAuthority = false)]
    public void UpdateScore(int playerId, float amount)
    {
        if (scores[playerId] + amount >= timeToWin)
        {
            if (gameRunning)
            {
                
                FinishGame(gameDataHolder.players[playerId]);
            }
            
        } else
        {
            scores[playerId] += amount;
            UpdateScoreIndicators(playerId, scores[playerId]);
        }
        
    }

    [ClientRpc]
    void UpdateScoreIndicators(int playerIndex, float newScore)
    {
        playerSliders[playerIndex].value = newScore;

        if (newScore/timeToWin > musicSpeed)
        {
            musicSpeed = newScore / timeToWin;

            musicInstance.setParameterByName("Pitch", musicSpeedCurve.Evaluate(musicSpeed));
        }
    }


    [ServerCallback]
    void FinishGame(PlayerController player)
    {
        if (gameRunning)
        {
            gameRunning = false;

            gameDataHolder.SetWinner(player.playerData.playerIndex);

            foreach (PlayerController p in GameDataHolder.instance.players.Values)
            {
                p.movementEnabled = false;
                FinishGameRpc(p.GetComponent<NetworkIdentity>().connectionToClient, p);
            }
        }
    }

    [TargetRpc]
    void FinishGameRpc(NetworkConnectionToClient target, PlayerController player)
    {
        gameUi.SetActive(false);

        CameraController camController = player.GetComponent<CameraController>();

        camController.transitionTimer = 1;

        camController.mode = CameraController.CameraMode.Win;
    }


    public void EnableWinScreen()
    {
        winScreen.SetActive(true);
        winScreen.GetComponent<Animator>().SetTrigger("SlideIn");
    }

    [ServerCallback]
    void LeaveLevel()
    {
        if (!isServer) return;
        Debug.Log("Finishing");
        foreach (GameObject egg in GameObject.FindGameObjectsWithTag("Egg"))
        {
            egg.GetComponent<BaseEgg>().ResetOwner();
            egg.GetComponent<NetworkIdentity>().RemoveClientAuthority();
            NetworkServer.UnSpawn(egg);
            PrefabPool.instance.Return(egg);
        }
        foreach (PlayerController player in gameDataHolder.players.Values)
        {
            if (player != null)
            {
                player.heldObject = null;
                player.UpdateTrapState(PlayerController.TrappedState.None);
                player.SetTriggerRpc("Reset");
            }
        }

        if (GameDataHolder.instance.gameWinnerId != -1)
        {
            PlayerController winner = GameDataHolder.instance.players[GameDataHolder.instance.gameWinnerId];

            if (winner.equippedHat == HatType.Roasted)
            {
                winner.equippedHat = HatType.Skin;
            }
            else
            {
                winner.equippedHat = HatType.Roasted;
            }
        }

        NetworkServer.SetAllClientsNotReady();
        NetworkManager.singleton.ServerChangeScene(lobbyScene);
    }

    void TransitionOutLevel()
    {
        for (int i = 0; i < gameDataHolder.players.Count; i++) 
        {
            if (gameDataHolder.players[i].isLocalPlayer)
            {
                blackScreenAnimator.SetTrigger("FadeIn");

                CameraController camController = gameDataHolder.players[i].GetComponent<CameraController>();
                
                camController.transitionTimer = camController.transitionTime;
                camController.mode = CameraController.CameraMode.TransitionOut;
                break;
            }
        }
    }

    [Command(requiresAuthority = false)]
    public void OnClientTransitionOver(int playerId)
    {
        readyPlayers[playerId] = true;

        bool allPlayersReady = true;

        for (int i = 0; i < readyPlayers.Length; i++) 
        {
            if (!readyPlayers[i])
            {
                allPlayersReady = false;
                break;
            }
        }

        if (allPlayersReady && gameRunning) 
        {
            StartCoroutine(StartGame());
            StartGameClientRpc();
        } else if (allPlayersReady)
        {
            Debug.Log("Lets leave");
            LeaveLevel();
        }
    }

    [ClientRpc]
    void StartGameClientRpc()
    {
        StartCoroutine(StartGame());
    }

    private void UpdateChickenMarker()
    {
        Vector2 targetPos = Camera.main.WorldToScreenPoint(NewChicken.instance.transform.position);

        if (Mathf.Abs(targetPos.x) < Screen.width && Mathf.Abs(targetPos.y) < Screen.height)
        {
            chickenMarker.gameObject.SetActive(false);
        } else
        {
            chickenMarker.gameObject.SetActive(true);
        }

        targetPos = new Vector2(Mathf.Clamp(targetPos.x, -Screen.width, Screen.width), Mathf.Clamp(targetPos.y, -Screen.height, Screen.height));

        chickenMarker.position = Vector2.Lerp(chickenMarker.position, targetPos, 4f * Time.deltaTime);
    }

    public void OnWinSlideIn()
    {
        winCounters[GameDataHolder.instance.currentWinner * 3 + gameDataHolder.wins[GameDataHolder.instance.currentWinner] - 1].SetActive(true);

        FMODUnity.RuntimeManager.PlayOneShot(stickerSFX);

        winScreen.GetComponent<Animator>().SetTrigger("SlideOut");
    }

    public void OnWinSlideOut() 
    {
        if (GameDataHolder.instance.gameWinnerId != -1)
        {

            musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

            PlayerController winner = GameDataHolder.instance.players[GameDataHolder.instance.gameWinnerId];

            winner.fakeHat.sprite = winner.equippedHat == HatType.Roasted ? hatSprites[7] : hatSprites[6];

            winner.animator.SetTrigger("HatOn");

            FMODUnity.RuntimeManager.PlayOneShot(conffetiSFX);
            FMODUnity.RuntimeManager.PlayOneShot(hatDescendSFX);

            confettiEffect.transform.position = (Vector2)winner.transform.position + new Vector2(0, 5);
            confettiEffect.SetActive(true);
        } else
        {
            TransitionOutLevel();
        }
    }

    public void OnWinnerAnimOver()
    {
        FMODUnity.RuntimeManager.PlayOneShot(winSFX);
        chickenDinnerText.SetActive(true);
    }

    public void OnTextAnimOver()
    {
        chickenDinnerText.SetActive(false);
        TransitionOutLevel();
    }
}
