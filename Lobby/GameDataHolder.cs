using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class GameDataHolder : NetworkBehaviour
{
    public static GameDataHolder instance;

    [SyncVar] public bool inGame = false;

    [SyncVar] public int gameWinnerId = -1;

    [SyncVar(hook = nameof(OnWinsChanged))] public int currentWinner = -1;

    public readonly SyncDictionary<int, PlayerController> players = new SyncDictionary<int, PlayerController>();

    public readonly SyncList<int> wins = new SyncList<int> { 0, 0, 0, 0 };
    //[Scene] public string[] levels;

    //public int index = 0;


    void OnWinsChanged(int old, int neW)
    {
        Debug.Log("Wins Changed");
    }

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(this);
        } else
        {
            instance = this;
        }

        DontDestroyOnLoad(gameObject);
    }

    [ServerCallback]
    public void StartNewGame()
    {
        for (int i = 0; i < wins.Count; i++) 
        {
            wins[i] = 0;
        }

        gameWinnerId = -1;

        inGame = true;
    }

    [ServerCallback]
    public void SetWinner(int winnerId)
    {
        wins[winnerId]++;

        currentWinner = winnerId;

        if (wins[winnerId] >= 3) 
        {
            gameWinnerId = winnerId;
            inGame = false;
        }
    }

    /*public void SetLevelSequence(LevelSequence data)
    {
        //int[] winners = { -1, -1, -1, -1 };
        winners = new int[data.levels.Length];

        for (int i = 0; i < winners.Length; i++) 
        {
            winners[i] = -1;
        }

        //levels = data.levels;
        index = 0;
    }

    public string GetNextLevel()
    {
        /*index++;

        if (index - 1 > levels.Length - 1)
        {
            return null;
        }

        return levels[index - 1];
        return "";
    }

    public void SetWinner(int winnerId) {
        winners[index - 1] = winnerId;
    }*/
}
