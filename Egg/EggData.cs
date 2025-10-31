using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

[CreateAssetMenu(fileName = "Egg Data", menuName = "ScriptableObjects/EggData", order = 1)]
public class EggData : ScriptableObject
{
    public Sprite sprite;
    public EggType type;
}