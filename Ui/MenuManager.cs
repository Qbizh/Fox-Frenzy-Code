using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Utp;
using Unity.Services.Relay;

public class MenuManager : MonoBehaviour
{
    [SerializeField] RelayNetworkManager relayNetworkManager;

    [SerializeField] TMP_InputField nameInput;
    [SerializeField] TMP_InputField IPInput;

    [SerializeField] Animator menuAnimator;

    bool hostPressed = false;

    [SerializeField] GameObject blackScreen;

    private void Start()
    {
        nameInput.text = PlayerPrefs.GetString(Constants.usernameKey);
    }

    private async void Awake()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public void OnNameChanged()
    {
        PlayerPrefs.SetString(Constants.usernameKey, nameInput.text);
    }

    public void TransitionFinished()
    {
        blackScreen.SetActive(true);

        menuAnimator.SetTrigger("Transition");
        Debug.Log("Triggered");

        if (hostPressed)
        {
            PlayerPrefs.Save();
            relayNetworkManager.StartRelayHost(NetworkManager.singleton.maxConnections);
        } else
        {
            PlayerPrefs.Save();
            relayNetworkManager.relayJoinCode = IPInput.text;
            relayNetworkManager.JoinRelayServer();
        }
    }

    public void HostPressed()
    {
        if (nameInput.text != "")
        {
            hostPressed = true;
            menuAnimator.SetTrigger("Transition");
        }
        
    }

    public void JoinPressed()
    {
        if (IPInput.text != "" && nameInput.text != "")
        {
            hostPressed = false;
            menuAnimator.SetTrigger("Transition");
        }
    }

    public void QuitPressed()
    {
        Application.Quit();
    }
}
