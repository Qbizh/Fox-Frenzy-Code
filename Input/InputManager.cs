using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager instance;

    public PlayerControls playerControls;

    public InputAction move;
    public InputAction jump;
    public InputAction dive;
    public InputAction interact;
    public InputAction fastFall;
    public InputAction select;

    private void Awake()
    {
        
        if (instance != null)
        {
            Destroy(gameObject);
        } else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        playerControls = new PlayerControls();
        playerControls.Player.Enable();
        
        move = playerControls.Player.Move;
        jump = playerControls.Player.Jump;
        dive = playerControls.Player.Dive;
        interact = playerControls.Player.Interact;
        fastFall = playerControls.Player.FastFall;
        select = playerControls.Player.Select;

        /*playerControls.Player.Jump.performed += OnJump;*/

        var rebinds = PlayerPrefs.GetString("rebinds");
        playerControls.LoadBindingOverridesFromJson(rebinds);
        for (int i = 0; i < move.bindings.Count; i++)
        {
            Debug.Log(move.bindings[i]);
        }
    }

    public void reloadInputs()
    {
        move = playerControls.Player.Move;
        jump = playerControls.Player.Jump;
        dive = playerControls.Player.Dive;
        interact = playerControls.Player.Interact;
        fastFall = playerControls.Player.FastFall;
        select = playerControls.Player.Select;
    }


}