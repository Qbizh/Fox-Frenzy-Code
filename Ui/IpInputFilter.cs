using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IpInputFilter : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<TMP_InputField>().onValidateInput += delegate (string input, int charIndex, char addedChar) {
            return (char.IsNumber(addedChar) || addedChar == '.') ? char.ToUpper(addedChar) : '\0';
        };
    }
}
