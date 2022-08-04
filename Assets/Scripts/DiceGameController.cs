using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class DiceGameController : MonoBehaviour
{
    [Header("Menu Fields")]
    [SerializeField] GameObject _menuPanel;
    [SerializeField] Text _informationText;
    [SerializeField] Button _buttonConfirm;
    [SerializeField] InputField _inputField;
    [SerializeField] ResultDiceGamePanel _endResultPanel;

    [Header("Game Fields")]
    [SerializeField] float _endTurnDiceMoveSpeed = 6;
    [Tooltip("Rotation Power of Dices")] [SerializeField] Vector2Int _diceMinMaxTorquePower = new Vector2Int(100, 500);
    [Tooltip("Throw Power of Dices")] [SerializeField] int _diceForcePower = 500;
    [SerializeField] Button _rollButton;
    [SerializeField] GameObject _gamePanel;
    [SerializeField] Transform _gridContent;
    [SerializeField] Transform _throwPoint;
    [SerializeField] List<Dice> _dices = new List<Dice>();
    [SerializeField] List<Transform> dicesEndTurnPositions = new List<Transform>();

    [Header("Prefabs")]
    [SerializeField] GameObject _gameRecord;

    [Header("Game Settings")]
    [SerializeField] int _startingSavedThrows = 3;
    [SerializeField] int _bonusRoundThrows = 2;
    [SerializeField] int _maxStoredThrows = 150;
    [Range(0, 6)] [SerializeField] int _easyDisciplineCount = 3;
    [Range(0, 10)] [SerializeField] int _hardDisciplineCount = 3;


    private static int _playerOnTurnCounter = 0;
    private static bool _isAtLeastOneMovePlayed = false;
    private static bool _canRegisterNewRecord = false;

    private List<Discipline> _disciplineList = new List<Discipline>();
    private List<Discipline> _chosenDisciplinesInGame = new List<Discipline>();

    private bool _gameOn = true;
    private int _turnCounter = 0;
    private int _playersCount = 0;
    private bool _isBlockedRollButton = false;
    private bool _newThrowStart = true;
    private bool _isAiOnTurn = false;
    private bool _isFirstRound = true;
    private bool _rollWasPresed = false;
    private bool _stopMoving = true;
    private List<int> _results = new List<int>();
    private List<GameRecord> _gameRecords = new List<GameRecord>();
    private List<Player> _playersInGame = new List<Player>();
    private GameRecord _playersPointsRecord = null;
    private GameRecord _playersThrowsRecord = null;

    public bool IsPlayerOnTurn
    {
        get
        {
            return _playerOnTurnCounter == 0;
        }
    }
    public static int PlayerOnTurnNumber { get { return _playerOnTurnCounter; } }
    public static bool IsAtLeastOneMovePlayed { get { return _isAtLeastOneMovePlayed; } }
    public static bool CanInteractUI { get { return _canRegisterNewRecord; } }

    // Start is called before the first frame update
    void Start()
    {
        _menuPanel.SetActive(true);
        _gamePanel.SetActive(false);
        _rollButton.gameObject.SetActive(false);
        _buttonConfirm.onClick.AddListener(OnConfirmPress);
        _rollButton.onClick.AddListener(OnRollPress);
        _inputField.placeholder.GetComponent<Text>().text = "Put Number here!";
        _endResultPanel.InitControlls(()=> RestartGame() , ()=> ExitGame());

        foreach (Dice dice in _dices)
        {
            dice.InitDice(OnSelectDice);
        }

        // create discipline
        var disciplineMemberCount = Enum.GetNames(typeof(DisciplineCondition)).Length;
        for (int i = 0; i < disciplineMemberCount; i++)
        {
            Discipline dis = new Discipline(i);
            _disciplineList.Add(dis);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_gameOn)
        {
            if (!_isAiOnTurn)
            {
                RaycastCheck();
                DicesEvaluationAfterRoll();
                ActivateRollButtonAgain();
                MoveDicesToMiddleSlowly();
            }
            else
            {
                VirtualAiRoll();
                CalculateSumPlayerPoints();
                UpdatePlayerOrder();
                CheckIfAiEndsTurn();
            }
        }

    }

    public void OnRollPress()
    {
        if (_isBlockedRollButton || _playersInGame[_playerOnTurnCounter].GetStoredThrows <= 0)
            return;

        foreach (GameRecord record in _gameRecords)
        {
            if (!record.IsRecordFulfilled(_playerOnTurnCounter))
            {
                record.ResetButtonLook();
            }
        }

        List<Dice> dicesReadyToThrow = new List<Dice>();

        foreach (var dice in _dices)
        {
            if (!dice.IsSelected)
            {
                dice.UnFreezeRigidBody();
                dicesReadyToThrow.Add(dice);
            }
            else
            {
                dice.TurnOffPhysics();
            }
        }

        int i = 0;
        foreach (var diceToThrow in dicesReadyToThrow)
        {
            Vector3 offSet = new Vector3(0, 0, (i * -0.2f));
            diceToThrow.transform.position = _throwPoint.position + offSet;
            diceToThrow.RandomThrow(_diceMinMaxTorquePower, _diceForcePower);
            i++;
        }

        _playersInGame[_playerOnTurnCounter].UseThrow();
        _playersThrowsRecord.UpdatePlayerThrows(_playersInGame[_playerOnTurnCounter].GetStoredThrows, _playerOnTurnCounter);

        Invoke("AfterRoll", 1f);
        _isBlockedRollButton = true;
        _canRegisterNewRecord = false;

        Debug.Log("OnRollPress");
    }

    private void VirtualAiRoll()
    {
        // virtual roll
        List<int> aiThrowsResults = new List<int>();

        for (int i = 0; i < 6; i++)
        {
            int randomNumber = Random.Range(1, 7);
            aiThrowsResults.Add(randomNumber);
        }

        // now i should how list of numbers
        // some AI logic which will decide what discipline to register..
        bool tmpAiStop = true;
        foreach (GameRecord gRecord in _gameRecords)
        {
            if (gRecord.IsRecordFulfilled(_playerOnTurnCounter))
                continue;

            int valueOfCurrentDiscipline = gRecord.CurrentDiscipline.DisciplineEvaluation(aiThrowsResults);

            if (valueOfCurrentDiscipline > 0 && tmpAiStop)
            {
                tmpAiStop = false;
                gRecord.AiRegisterRecord(_playerOnTurnCounter, valueOfCurrentDiscipline);
            }
        }
    }

    private void CheckIfIsEndGame()
    {
        if (_turnCounter >= _chosenDisciplinesInGame.Count)
        {
            _turnCounter = 0;

            _endResultPanel.InicializedFinalPanel(_playersInGame);

            _gameOn = false;
        }
    }


    private void CheckIfAiEndsTurn()
    {
        if (IsPlayerOnTurn)
        {
            _isAiOnTurn = false;
            _isBlockedRollButton = false;
            CheckIfIsEndGame();
        }
    }

    private void ActivateRollButtonAgain()
    {
        if (_isBlockedRollButton && !_newThrowStart)
        {
            _isBlockedRollButton = false;
            _newThrowStart = true;
            _canRegisterNewRecord = true;
        }
    }

    // Invoked 
    public void AfterRoll()
    {
        //_newThrowStart = true;
        _rollWasPresed = true;
    }

    public void OnConfirmPress()
    {
        _gameOn = true;
        bool validInput = false;
        string playerCountString = _inputField.text.ToString();

        if (!string.IsNullOrEmpty(playerCountString))
        {
            _playersCount = Int32.Parse(playerCountString);
        }

        if (_playersCount <= 0 || _playersCount > 5)
        {
            _informationText.text = "Mezi 1 - 5";
            validInput = false;
        }
        else
        {
            _informationText.text = "Good Job";
            validInput = true;
        }

        if (validInput)
        {
            _playersInGame.Clear();
            for (int i = 0; i < _playersCount; i++)
            {
                Player player = new Player(_startingSavedThrows);
                _playersInGame.Add(player);
            }

            // clean previous Game
            foreach (Transform child in _gridContent.transform)
            {
                Destroy(child.gameObject);
            }

            InicializedGamePanel();
            SetupGame();
        }
    }

    public void RestartGame()
    {
        // Todo Implementation
        _menuPanel.SetActive(true);
        _endResultPanel.gameObject.SetActive(false);
    }

    public void ExitGame()
    {
        // Todo Implementation
    }

    private void InicializedGamePanel()
    {
        // first row
        var go = Instantiate(_gameRecord, _gridContent);
        GameRecord gRecord = go.GetComponent<GameRecord>();
        go.name = "PlayersCount";
        gRecord.InitRecord(_playersCount, 1);
        // _playersThrowsRecord , second row
        go = Instantiate(_gameRecord, _gridContent);
        go.name = "StoredThrows";
        _playersThrowsRecord = go.GetComponent<GameRecord>();
        _playersThrowsRecord.InitRecord(_playersCount, 2, startingThrows: _startingSavedThrows);

        // others disciplpine rows
        _chosenDisciplinesInGame.Clear();
        _chosenDisciplinesInGame = ChoiseRandomDiscipline();
        _gameRecords.Clear();

        foreach (Discipline discipline in _chosenDisciplinesInGame)
        {
            go = Instantiate(_gameRecord, _gridContent);
            gRecord = go.GetComponent<GameRecord>();
            gRecord.InitRecord(_playersCount, discipline: discipline, action: OnActionRecordPress);
            _gameRecords.Add(gRecord);
        }

        // _playersPointsRecord , last row 
        go = Instantiate(_gameRecord, _gridContent);
        go.name = "Points";
        _playersPointsRecord = go.GetComponent<GameRecord>();
        _playersPointsRecord.InitRecord(_playersCount, 3);
    }

    private void SetupGame()
    {
        _playerOnTurnCounter = 0;
        _menuPanel.SetActive(false);
        _gamePanel.SetActive(true);
        _rollButton.gameObject.SetActive(true);
    }

    public List<Discipline> ChoiseRandomDiscipline ()
    {
        List<Discipline> chosenDisciplines = new List<Discipline>();

        int i = 0;
        while (i < _easyDisciplineCount)
        {
            int randomNumber = UnityEngine.Random.Range(0, 6);
            Discipline dis = _disciplineList[randomNumber];

            if (!chosenDisciplines.Contains(dis))
            {
                chosenDisciplines.Add(dis);
                i++;
            }
        }

        var disciplineMemberCount = Enum.GetNames(typeof(DisciplineCondition)).Length;
        i = 0;
        while (i < _hardDisciplineCount)
        {
            int randomNumber = UnityEngine.Random.Range(6, disciplineMemberCount);
            Discipline dis = _disciplineList[randomNumber];

            if (!chosenDisciplines.Contains(dis))
            {
                chosenDisciplines.Add(dis);
                i++;
            }
        }

        chosenDisciplines = chosenDisciplines.OrderBy(x => (int)(x.GetDiscipline)).ToList();

        return chosenDisciplines;
    }

    public void OnSelectDice(Dice dice)
    {
        dice.SelectDice();

        Debug.Log("OnSelectDice");
    }

    private void UpdatePlayerOrder ()
    {
        _playerOnTurnCounter += 1;

        if (_playerOnTurnCounter > _playersCount -1)
        {
            _playerOnTurnCounter = 0;
            _isFirstRound = false;
        }

        if (!_isFirstRound)
        {
            _playersInGame[_playerOnTurnCounter].AddNewRoundThrows(_bonusRoundThrows);
        }

        _playersThrowsRecord.UpdatePlayerThrows(_playersInGame[_playerOnTurnCounter].GetStoredThrows, _playerOnTurnCounter);
    }

    public void OnActionRecordPress()
    {
        if (IsAtLeastOneMovePlayed && CanInteractUI)
        {
            CalculateSumPlayerPoints();
            UpdatePlayerOrder();
            DeSeleceDices();
            
            _isAiOnTurn = true;
            _isAtLeastOneMovePlayed = false;
            _turnCounter += 1;
            Debug.Log("Action !!");
        }
    }

    private void CalculateSumPlayerPoints()
    {
        int finalPoints = 0;
        foreach (GameRecord gRecord in _gameRecords)
        {
            if (gRecord.IsRecordFulfilled(_playerOnTurnCounter))
            {
                finalPoints += gRecord.GetPlayerDisciplineValue(_playerOnTurnCounter);
            }
            else
            {
                if(IsPlayerOnTurn)
                {
                    gRecord.ResetButtonLook();
                }
            }
        }

        // Refresh final Player Points , save final points
        _playersPointsRecord.UpdatePlayerScore(finalPoints, _playerOnTurnCounter);
        _playersInGame[_playerOnTurnCounter].FinalPoints = finalPoints;
    }

    private bool CheckDicesVelocity ()
    {
        foreach (Dice dice in _dices)
        {
            var hasVelocity = dice.HasVelocity;
            if (hasVelocity)
                return false;
        }

        return true;
    }

    private void RaycastCheck()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                var trans = hit.transform;
                Dice dice = trans.gameObject.GetComponent<Dice>();

                if (dice != null && CanInteractUI && IsAtLeastOneMovePlayed)
                {
                    dice.OnSelectAction();
                }
            }
        }
    }

    private void DicesEvaluationAfterRoll()
    {
        if (_rollWasPresed && CheckDicesVelocity())
        {
            _results.Clear();
            foreach (Dice dice in _dices)
            {
                int result = dice.EvaluateThrow();
                _results.Add(result);
                dice.FreezeRotation();
            }

            foreach (GameRecord gRecord in _gameRecords)
            {
                if (gRecord.IsRecordFulfilled(_playerOnTurnCounter))
                    continue;

                int valueOfCurrentDiscipline = gRecord.CurrentDiscipline.DisciplineEvaluation(_results);
                if (valueOfCurrentDiscipline > 0)
                {
                    // Todo Later maybe some anim for those which has some value ..
                }
                gRecord.TurnOnPosibleCompletedOptionIndicator(valueOfCurrentDiscipline);
            }

            _isAtLeastOneMovePlayed = true;
            _newThrowStart = false;
            _rollWasPresed = false;
            _stopMoving = false;
        }
    }

    private void DeSeleceDices ()
    {
        foreach (Dice dice in _dices)
        {
            if (dice.IsSelected)
            {
                dice.DeSelect();
            }
        }
    }

    void MoveDicesToMiddleSlowly()
    {
        if(!_stopMoving)
        {
            int i = 0, j = 0;
            int countPositions = dicesEndTurnPositions.Count;
            float step = _endTurnDiceMoveSpeed * Time.deltaTime;
            foreach (Dice dice in _dices)
            {
                if (i < countPositions)
                    dice.transform.position = Vector3.MoveTowards(dice.transform.position, dicesEndTurnPositions[i].position, step);

                if (dice.transform.position == dicesEndTurnPositions[i].position)
                    j++;

                if (j >= 6)
                {
                    _stopMoving = true;
                    // activate colider
                    foreach (var item in _dices)
                    {
                        item.TurnOnPhysics();
                    }
                }

                i++;
            }
        }
    }

    public class Player
    {
        private int _storedThrows = 0;
        private int _finalpoints = 0;
        public int GetStoredThrows { get { return this._storedThrows; } }
        public int FinalPoints
        {
            get { return this._finalpoints; }
            set { _finalpoints = value; }
        }
        public Player(int startingThrowsCount)
        {
            _finalpoints = 0;
            _storedThrows = startingThrowsCount;
        }

        public void UseThrow()
        {
            _storedThrows -= 1;
        }

        public void AddNewRoundThrows(int newThrows)
        {
            _storedThrows += newThrows; 
        }
    }


    public class Discipline
    {
        private string _name;
        private DisciplineCondition _discipline = DisciplineCondition.ones;
        private bool _isDifficult = false;

        private List<int> _results = new List<int>();
        public string GetName { get { return _name; } }
        public DisciplineCondition GetDiscipline { get { return _discipline; } }
        public bool GetDifficulty { get { return _isDifficult; } }

        public Discipline(int disciplineNumber)
        {
            _discipline = (DisciplineCondition)disciplineNumber;
            _name = _discipline.ToString();

            // hard coded number . Thats shit. According something we should decide what is difficult discipline ..
            // for easy expansion , Right now first six are easy.
            if (disciplineNumber > 6)
            {
                _isDifficult = true;
            }
        }

        public int DisciplineEvaluation(List<int> diceResults)
        {
            _results.Clear();
            _results.AddRange(diceResults);

            int finalDisciplineResult = 0;

            switch (_discipline)
            {
                case DisciplineCondition.ones:
                    finalDisciplineResult = SimpleDisciplineCheck(1);
                    break;
                case DisciplineCondition.twos:
                    finalDisciplineResult = SimpleDisciplineCheck(2);
                    break;
                case DisciplineCondition.threesomes:
                    finalDisciplineResult = SimpleDisciplineCheck(3);
                    break;
                case DisciplineCondition.squares:
                    finalDisciplineResult = SimpleDisciplineCheck(4);
                    break;
                case DisciplineCondition.fives:
                    finalDisciplineResult = SimpleDisciplineCheck(5);
                    break;
                case DisciplineCondition.six:
                    finalDisciplineResult = SimpleDisciplineCheck(6);
                    break;
                case DisciplineCondition.pyramidBig:
                    finalDisciplineResult = PyramidCheck(false);
                    break;
                case DisciplineCondition.pyramidSmall:
                    finalDisciplineResult = PyramidCheck(true);
                    break;
                case DisciplineCondition.even:
                    finalDisciplineResult = EvenCheck();
                    break;
                case DisciplineCondition.odd:
                    finalDisciplineResult = OddCheck();
                    break;
                case DisciplineCondition.big:
                    finalDisciplineResult = BigChech();
                    break;
                case DisciplineCondition.small:
                    finalDisciplineResult = SmallChech();
                    break;
                case DisciplineCondition.threePlusThree:
                    finalDisciplineResult = FirstPlusSecondValueCheck(3, 3);
                    break;
                case DisciplineCondition.fourPlusTwo:
                    finalDisciplineResult = FirstPlusSecondValueCheck(4, 2);
                    break;
                case DisciplineCondition.pairs:
                    finalDisciplineResult = PairsCheck();
                    break;
                case DisciplineCondition.straig:
                    finalDisciplineResult = StraigCheck();
                    break;
                default:
                    break;
            }

            return finalDisciplineResult;
        }

        private int SimpleDisciplineCheck(int checkNumber)
        {
            int finalCount = 0;
            foreach (var number in _results)
            {
                if (checkNumber == number)
                {
                    finalCount += number;
                }
            }
            return finalCount;
        }

        private int PyramidCheck(bool isSmall)
        {
            int finalCount = 0;

            int six = _results.Where(m => m == 6).Count();
            int fives = _results.Where(m => m == 5).Count();
            int fours = _results.Where(m => m == 4).Count();
            int threes = _results.Where(m => m == 3).Count();
            int twos = _results.Where(m => m == 2).Count();
            int ones = _results.Where(m => m == 1).Count();

            if (isSmall)
            {
                if (ones == 3 && twos == 2 && threes == 1)
                {
                    finalCount = 10;
                }
                else if (twos == 3 && threes == 2 && fours == 1)
                {
                    finalCount = 16;
                }
                else if (threes == 3 && fours == 2 && fives == 1)
                {
                    finalCount = 22;
                }
                else if (fours == 3 && fives == 2 && six == 1)
                {
                    finalCount = 28;
                }
            }
            else
            {
                if (six == 3 && fives == 2 && fours == 1)
                {
                    finalCount = 32;
                }
                else if (fives == 3 && fours == 2 && threes == 1)
                {
                    finalCount = 26;
                }
                else if (fours == 3 && threes == 2 && twos == 1)
                {
                    finalCount = 20;
                }
                else if (threes == 3 && twos == 2 && ones == 1)
                {
                    finalCount = 14;
                }
            }

            return finalCount;
        }

        public int EvenCheck()
        {
            int finalCount = 0;
            bool allEven = true;

            foreach (int number in _results)
            {
                if (number % 2 == 0)
                    finalCount += number;
                else
                    allEven = false;
            }

            if (allEven)
                return finalCount;

            return 0;
        }

        public int OddCheck()
        {
            int finalCount = 0;
            bool allOdd = true;

            foreach (int number in _results)
            {
                if (!(number % 2 == 0))
                    finalCount += number;
                else
                    allOdd = false;
            }

            if (allOdd)
                return finalCount;

            return 0;
        }

        public int BigChech()
        {
            int finalCount = 0;
            foreach (int number in _results)
            {
                if (number <= 3)
                    return 0;

                finalCount += number;
            }

            return finalCount;
        }

        public int SmallChech()
        {
            int finalCount = 0;

            foreach (int number in _results)
            {
                if (number >= 4)
                    return 0;

                finalCount += number;
            }

            return finalCount;
        }

        public int FirstPlusSecondValueCheck(int checkValueFirst, int checkValueSecond)
        {
            int finalCount = 0;

            int six = _results.Where(m => m == 6).Count();
            int fives = _results.Where(m => m == 5).Count();
            int fours = _results.Where(m => m == 4).Count();
            int threes = _results.Where(m => m == 3).Count();
            int twos = _results.Where(m => m == 2).Count();
            int ones = _results.Where(m => m == 1).Count();

            if (six == checkValueFirst && (fives == checkValueSecond || fours == checkValueSecond || threes == checkValueSecond || twos == checkValueSecond || ones == checkValueSecond))
                finalCount = SumAllNumbersInField();

            else if (fives == checkValueFirst && (six == checkValueSecond || fours == checkValueSecond || threes == checkValueSecond || twos == checkValueSecond || ones == checkValueSecond))
                finalCount = SumAllNumbersInField();

            else if (fours == checkValueFirst && (six == checkValueSecond || fives == checkValueSecond || threes == checkValueSecond || twos == checkValueSecond || ones == checkValueSecond))
                finalCount = SumAllNumbersInField();

            else if (threes == checkValueFirst && (six == checkValueSecond || fives == checkValueSecond || fours == checkValueSecond || twos == checkValueSecond || ones == checkValueSecond))
                finalCount = SumAllNumbersInField();

            else if (twos == checkValueFirst && (six == checkValueSecond || fives == checkValueSecond || fours == checkValueSecond || threes == checkValueSecond || ones == checkValueSecond))
                finalCount = SumAllNumbersInField();

            else if (ones == checkValueFirst && (six == checkValueSecond || fives == checkValueSecond || fours == checkValueSecond || threes == checkValueSecond || twos == checkValueSecond))
                finalCount = SumAllNumbersInField();

            return finalCount;
        }

        public int PairsCheck()
        {
            int finalCount = 0;
            List<int> countList = new List<int>();

            countList.Add(_results.Where(m => m == 6).Count());
            countList.Add(_results.Where(m => m == 5).Count());
            countList.Add(_results.Where(m => m == 4).Count());
            countList.Add(_results.Where(m => m == 3).Count());
            countList.Add(_results.Where(m => m == 2).Count());
            countList.Add(_results.Where(m => m == 1).Count());

            int counter = 0;
            foreach (int count in countList)
            {
                if(count == 2)
                    counter++;
              
                if (counter == 3)
                {
                    finalCount = SumAllNumbersInField();
                    return finalCount;
                }
            }

            return finalCount;
        }

        public int StraigCheck()
        {
            int finalCount = 0;

            int six = _results.Where(m => m == 6).Count();
            int fives = _results.Where(m => m == 5).Count();
            int fours = _results.Where(m => m == 4).Count();
            int threes = _results.Where(m => m == 3).Count();
            int twos = _results.Where(m => m == 2).Count();
            int ones = _results.Where(m => m == 1).Count();

            if(six == 1 && fives == 1 && fours == 1 && threes == 1 && twos == 1 && ones == 1)
            {
                finalCount = SumAllNumbersInField();
            }

            return finalCount;
        }

        private int SumAllNumbersInField()
        {
            int finalCount = 0;
            foreach (int number in _results)
                finalCount += number;

            return finalCount;
        }
    }

    public enum DisciplineCondition
    {
        ones = 0,
        twos = 1,
        threesomes, // ou jejje bejbe
        squares,
        fives,
        six,
        pyramidBig,
        pyramidSmall,
        even, // suda
        odd, // licha
        big, // dices must be 4 - 6
        small, // dices must be 1 - 3
        threePlusThree, // three same with other three same
        fourPlusTwo, // four same with other two same
        pairs, // three pairs of same dices.  For example -> 2x 3d  + 2x 5d + 2x 6d 
        straig // numerical procedure
    }
}
