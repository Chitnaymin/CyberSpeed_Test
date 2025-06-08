using System.Collections.Generic;

[System.Serializable]
public class GameSaveData
{
    public int score;
    public int turnCount;
    public string level;
    public List<int> cardIdOrder;
    public List<int> matchedCardIndices;
}