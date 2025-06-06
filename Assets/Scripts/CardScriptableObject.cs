using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardData", menuName = "ScriptableObjects/CardSO", order = 1)]
public class CardScriptableObject : ScriptableObject
{
    public string name;
    public int id;
    public Sprite cardImage;
}
