using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Events;

public class ResultDiceGamePanel : MonoBehaviour
{
    [SerializeField] List<GameObject> finalResults = new List<GameObject>();

    [SerializeField] Button _exitButton;
    [SerializeField] Button _restartButton;

    public void InicializedFinalPanel(List<DiceGameController.Player> _playersInGame)
    {
        foreach (var result in finalResults)
        {
            result.SetActive(false);
        }

        for (int i = 0; i < _playersInGame.Count; i++)
        {
            var text1 = finalResults[i].transform.GetChild(0);
            var text2 = finalResults[i].transform.GetChild(1);
            text1.GetComponent<Text>().text = $"Player {i}";
            text2.GetComponent<Text>().text = $"{_playersInGame[i].FinalPoints}";

            finalResults[i].SetActive(true);
        }

        this.gameObject.SetActive(true);
    }

    public void InitControlls(UnityAction action, UnityAction action2)
    {
        _exitButton.onClick.AddListener(action);
        _restartButton.onClick.AddListener(action2);
        this.gameObject.SetActive(false);
    }
}
