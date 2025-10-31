using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuWrapper : MonoBehaviour
{
    [SerializeField] MenuManager menuManager;

    public void OnTranistionOver()
    {
        menuManager.TransitionFinished();
    }
}
