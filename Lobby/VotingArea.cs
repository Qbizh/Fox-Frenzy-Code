using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

public class VotingArea : NetworkBehaviour
{
    [SerializeField] TMP_Text votesDisplay;

    [SyncVar(hook = nameof(OnVoteChange))] public int votes;
    [Scene] public string level;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isServer && other.gameObject.CompareTag("Player"))
        {
            votes++;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (isServer && other.gameObject.CompareTag("Player"))
        {
            votes--;
        }
    }

    void OnVoteChange(int oldVotes, int newVotes)
    {
        votesDisplay.text = newVotes.ToString();
    }
}
