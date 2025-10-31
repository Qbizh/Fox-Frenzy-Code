using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NameInputFilter : MonoBehaviour
{
    [SerializeField] int maxLength;

    private void Awake()
    {
        GetComponent<TMP_InputField>().onValidateInput += delegate (string input, int charIndex, char addedChar) {
            return (input.Length <= maxLength) ? char.ToUpper(addedChar) : '\0';
        };
    }
}
