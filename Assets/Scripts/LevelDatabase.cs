using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "Level/Database", order = 1)]
public class LevelDatabase : ScriptableObject
{
    public List<TextAsset> levelTexts;
}
