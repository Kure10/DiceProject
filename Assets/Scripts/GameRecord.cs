using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameRecord : MonoBehaviour
{
    [SerializeField] Text _disciplineNameText;
    [SerializeField] Button _scoreButton;

    [Space]
    [SerializeField] GameObject _doneIndicator;
    [SerializeField] GameObject _posibleOptionIndicator;
    [SerializeField] Text _valueText;

    [SerializeField] List<Text> _playerTextList = new List<Text>();

    private DiceGameController.Discipline _discipline = null;
    private int _value = 0;
    private List<RecordData> _recordDataList = new List<RecordData>();

    public DiceGameController.Discipline CurrentDiscipline { get { return this._discipline; } }

    public void InitRecord(int playersCount, int _mode = 0, int startingThrows = 3, DiceGameController.Discipline discipline = null, UnityAction action = null)
    {
        _discipline = discipline;

        if (_mode == 0)
        {
            _disciplineNameText.text = discipline.GetName;
            _scoreButton.gameObject.SetActive(true);
            _scoreButton.onClick.AddListener(OnRegisterRecord);
            _scoreButton.onClick.AddListener(action);

            _recordDataList.Clear();
            
            for (int i = 0; i < playersCount; i++)
            {
                var tex = _playerTextList[i];
                tex.text = "-";
                tex.gameObject.SetActive(true);

                RecordData rd = new RecordData(i);
                _recordDataList.Add(rd);
            }
        }
        else if (_mode == 1)
        {
            _disciplineNameText.text = "Players";
            _scoreButton.gameObject.SetActive(false);

            for (int i = 0; i < playersCount; i++)
            {
                var tex = _playerTextList[i];
                tex.text = (i + 1).ToString();
                tex.gameObject.SetActive(true);
            }
        }
        else if (_mode == 2)
        {
            _disciplineNameText.text = "Stored";
            _scoreButton.gameObject.SetActive(false);
            
            for (int i = 0; i < playersCount; i++)
            {
                var tex = _playerTextList[i];
                tex.text = startingThrows.ToString();
                tex.gameObject.SetActive(true);
            }
        }
        else if (_mode == 3)
        {
            _disciplineNameText.text = "Points";
            _scoreButton.gameObject.SetActive(false);

            for (int i = 0; i < playersCount; i++)
            {
                var tex = _playerTextList[i];
                tex.text = "0";
                tex.gameObject.SetActive(true);
            }
        }
    }

    public bool IsRecordFulfilled(int playerOnturnNumber)
    {
        if (_recordDataList.Count >= playerOnturnNumber)
        {
            RecordData record = _recordDataList[playerOnturnNumber];
            return record.IsFullfilled;
        }
        else
        {
            Debug.LogWarning("Error there is no such record in RecordDataList");
        }

        return false;
    }

    public void TurnOnPosibleCompletedOptionIndicator(int valueOfCurrentDiscipline)
    {
        _posibleOptionIndicator.SetActive(true);
        _doneIndicator.SetActive(false);
        _value = valueOfCurrentDiscipline;
        _valueText.text = valueOfCurrentDiscipline.ToString();
        _valueText.enabled = true;
    }

    public void TurnOnConCompliantOptionIndicator(int valueOfCurrentDiscipline)
    {
        // Todo Maybe later..

        //_posibleOptionIndicator.SetActive(true);
        //_doneIndicator.SetActive(false);
        //_value = valueOfCurrentDiscipline;
        //_valueText.text = valueOfCurrentDiscipline.ToString();
        //_valueText.enabled = true;
    }

    public void TurnOnDoneIndicator()
    {
        _posibleOptionIndicator.SetActive(false);
        _doneIndicator.SetActive(true);
    }

    public void ResetButtonLook()
    {
        _value = 0;
        _posibleOptionIndicator.SetActive(false);
        _doneIndicator.SetActive(false);
        _valueText.text = "-";
        _valueText.enabled = false;
    }

    public void UpdatePlayerScore(int finalValue, int playerOnTurn)
    {
        _playerTextList[playerOnTurn].text = finalValue.ToString();
    }

    public void UpdatePlayerThrows(int remainingThrows, int playerOnTurn)
    {
        _playerTextList[playerOnTurn].text = remainingThrows.ToString();
    }

    private void OnRegisterRecord()
    {
        if(DiceGameController.IsAtLeastOneMovePlayed && DiceGameController.CanInteractUI)
        {
            TurnOnDoneIndicator();
            _scoreButton.onClick.RemoveAllListeners();

            int playerOnTurn = DiceGameController.PlayerOnTurnNumber;

            RecordData record = _recordDataList[playerOnTurn];
            record.RegisterRecord(_value);
            _valueText.text = _value.ToString();
            _valueText.enabled = true;
        }
    }

    public void AiRegisterRecord(int playerOnTurnNumber, int value)
    {
        RecordData record = _recordDataList[playerOnTurnNumber];
        record.RegisterRecord(value);

        _playerTextList[playerOnTurnNumber -1].text = value.ToString();
        _playerTextList[playerOnTurnNumber -1].gameObject.SetActive(true);
    }

    public int GetPlayerDisciplineValue (int playerSerialNumber)
    {
        return _recordDataList[playerSerialNumber].GetDisValue;
    }

    public class RecordData
    {
        private int _playerSerialNumber = -1;
        private bool _isFulfilled = false;
        private int _disciplineValue = 0;

        public bool IsFullfilled { get { return _isFulfilled; } }
        public int GetDisValue { get { return _disciplineValue; } }
        public RecordData(int playerSerialNum)
        {
            _playerSerialNumber = playerSerialNum;
            _isFulfilled = false;
            _disciplineValue = 0;
        }

        public void RegisterRecord (int disciplineValue)
        {
            _isFulfilled = true;
            _disciplineValue = disciplineValue;
        }
    }
}
