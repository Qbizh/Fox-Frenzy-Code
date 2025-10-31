using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlackScreenWrapper : MonoBehaviour
{
    public void OnFadeOut()
    {
        gameObject.SetActive(false);
    }
}
