using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterWrapper : MonoBehaviour
{
    [SerializeField] PlayerController playerController;

    public void OnInteract()
    {
        playerController.ProcessInteract();
    }
    
    public void OnEatFinished()
    {
        playerController.OnEatFinished();
    }

    public void OnHatBestowFinished()
    {
        playerController.OnHatBestowFinished();
    }
}
