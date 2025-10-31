using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraController : NetworkBehaviour
{
    private Bounds camBounds;

    [Scene] [SerializeField] string lobbyScene;

    [SerializeField] float lerpSpeed = 0.3f;

    public float transitionTime = 3f;
    [SerializeField] int transitionDistance = 3;

    [SerializeField] float gameCamSize = 12f;
    [SerializeField] float lobbyCamSize = 10.39f;
    [SerializeField] float winCamSize = 5;

    Vector3 lobbyTargetPos = new Vector3(0, 5.9f, -10);

    float targetOrthographicSize = -1;

    public float transitionTimer = 0f;
    float transitionInitialY = 0f;

    float camHeight;
    float camWidth;

    float minX;
    float maxX;

    float minY;
    float maxY;

    bool winTransitionFinished = false;

    public enum CameraMode
    {
        Lobby,
        Game,
        Transition,
        Win,
        TransitionOut,
        None
    }

    public delegate void TransitionOver(int playerIndex);
    public TransitionOver transitionOver;

    public CameraMode mode = CameraMode.Game;

    private void Start()
    {
        if (!isLocalPlayer) return;

        //SetUpLobbyCam();

        GetCameraBounds();

        //Set up lobby transition
        mode = CameraMode.Transition;
        transitionTimer = transitionTime;

        targetOrthographicSize = camBounds.extents.y;
        Camera.main.orthographicSize = targetOrthographicSize;
        transitionInitialY = camBounds.center.y + camBounds.size.y;

        SceneManager.activeSceneChanged += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene lastScene, Scene newScene)
    {
        if (!isLocalPlayer) return;
        mode = CameraMode.Transition;
        transitionTimer = transitionTime;

        Camera.main.orthographicSize = gameCamSize;
        GetCameraBounds();
        /*if (newScene.path != lobbyScene)
        {
            mode = CameraMode.Game;
            Camera.main.orthographicSize = gameCamSize;
            GetCameraBounds();
        } else
        {
            Debug.Log("Sent to Lobby");
            SetUpLobbyCam();
        }*/

        

        if (newScene.path == lobbyScene)
        {
            Debug.Log("AAAAAAHHH WE ARE IN TEH FRICKING LOBBY AAAH");

            targetOrthographicSize = camBounds.extents.y;
            Camera.main.orthographicSize = targetOrthographicSize;

            transitionInitialY = camBounds.center.y + camBounds.size.y;
        } else
        {
            targetOrthographicSize = camBounds.extents.x / Camera.main.aspect;
            Camera.main.orthographicSize = targetOrthographicSize;

            transitionInitialY = camBounds.center.y - camBounds.size.y * transitionDistance;
        }
    }

    void TransitionCamera(Vector2 targetPos, GameObject player)
    {
        if (Camera.main != null && player == gameObject)
        {
            lobbyTargetPos = new Vector3(targetPos.x, targetPos.y, Camera.main.transform.position.z);
        }
    }

    private void GetCameraBounds()
    {
        var camBoundObj = GameObject.FindGameObjectsWithTag("CameraBounds")[0];

        camBounds = camBoundObj.GetComponent<SpriteRenderer>().bounds;

        CalculateBounds();
    }

    private void CalculateBounds()
    {
        camHeight = Camera.main.orthographicSize;
        camWidth = camHeight * Camera.main.aspect;

        minX = camBounds.min.x + camWidth;
        maxX = camBounds.max.x - camWidth;

        minY = camBounds.min.y + camHeight;
        maxY = camBounds.max.y - camHeight;
    }

    [TargetRpc]
    public void SetCamMode(NetworkConnectionToClient target, CameraMode newMode)
    {
        mode = newMode;

        if (newMode == CameraMode.Transition || newMode == CameraMode.TransitionOut)
        {
            transitionTimer = transitionTime;
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;
        
        CalculateBounds();

        switch(mode)
        {
            case CameraMode.Lobby:

                Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, lobbyTargetPos, lerpSpeed * Time.deltaTime);
                Camera.main.orthographicSize = lobbyCamSize;

                break;
            case CameraMode.Game:

                Vector3 targetPos = new Vector3(transform.position.x, transform.position.y, Camera.main.transform.position.z);

                targetPos = ClampCamToBounds(targetPos);

                Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, targetPos, lerpSpeed * Time.deltaTime);
                Camera.main.orthographicSize = Mathf.Lerp(Camera.main.orthographicSize, gameCamSize, lerpSpeed * Time.deltaTime);
                break;
            case CameraMode.Transition:
                //Camera.main.orthographicSize = camBounds.extents.x;

                transitionTimer -= Time.deltaTime;

                Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, Mathf.Lerp(transitionInitialY, camBounds.center.y, 1 - transitionTimer / transitionTime), Camera.main.transform.position.z);

                if (transitionTimer <= 0)
                {
                    targetOrthographicSize = -1;

                    mode = CameraMode.Game;

                    if (GameManager.instance != null)
                    {
                        GameManager.instance.OnClientTransitionOver(GetComponent<PlayerController>().playerData.playerIndex);
                    } else
                    {
                        LobbyManager.instance.OnTransitionOver(GetComponent<PlayerController>().playerData.playerIndex);
                    }
                }
                break;
            case CameraMode.TransitionOut:
                transitionTimer -= Time.deltaTime;

                transitionInitialY = camBounds.center.y;

                Camera.main.transform.position = new Vector3(camBounds.center.x, Mathf.Lerp(transitionInitialY, camBounds.center.y - camBounds.size.y * transitionDistance, 1 - transitionTimer / transitionTime), Camera.main.transform.position.z);

                Camera.main.orthographicSize = Mathf.Lerp(Camera.main.orthographicSize, targetOrthographicSize == -1 ? camBounds.extents.x / Camera.main.aspect : targetOrthographicSize, lerpSpeed * Time.deltaTime);


                if (transitionTimer <= 0)
                {
                    GameManager.instance.OnClientTransitionOver(GetComponent<PlayerController>().playerData.playerIndex);
                    //transitionOver(GetComponent<PlayerController>().playerData.playerIndex);

                    targetOrthographicSize = -1;

                    mode = CameraMode.None;
                }

                break;
            case CameraMode.Win:
                if (GameDataHolder.instance.currentWinner == -1) break;

                Vector3 winnerPos = GameDataHolder.instance.players[GameDataHolder.instance.currentWinner].GetComponent<Transform>().position;
                winnerPos = new Vector3(winnerPos.x, winnerPos.y, Camera.main.transform.position.z);


                winnerPos = ClampCamToBounds(winnerPos);

                transitionTimer -= Time.deltaTime;

                if (transitionTimer <= 0)
                {
                    if (!winTransitionFinished)
                    {
                        winTransitionFinished = true;
                        GameDataHolder.instance.players[GameDataHolder.instance.currentWinner].animator.SetTrigger("Eat");
                    }
                    
                }

                Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, winnerPos, 1 - transitionTimer / transitionTime);
                Camera.main.orthographicSize = Mathf.Lerp(Camera.main.orthographicSize, winCamSize, 1 - transitionTimer / transitionTime);

                break;
            case CameraMode.None:
                break;
        }

        if (mode != CameraMode.Win)
        {
            winTransitionFinished = false;
        }
    }

    private Vector3 ClampCamToBounds(Vector3 targetPos)
    {
        return new Vector3(Mathf.Clamp(targetPos.x, minX, maxX), Mathf.Clamp(targetPos.y, minY, maxY), targetPos.z);
    }
}
