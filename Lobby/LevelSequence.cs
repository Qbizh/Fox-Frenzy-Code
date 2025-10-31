using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[CreateAssetMenu(fileName = "Level Sequence", menuName = "ScriptableObjects/LevelSequence", order = 1)]
public class LevelSequence : ScriptableObject
{
    [Scene] public string[] levels;
}