using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeoDefence
{
    public class StateManager : MonoBehaviour
    {
        public static StateManager Instance;

        //Property cant be shown in editor?
        //[SerializeField] public GameState CurrentGameState { get; private set; }
        public GameState CurrentGameState;
        [SerializeField] private GameState _nextState;
        [SerializeField] private SceneName _nextSceneName;
        [SerializeField] private SceneName _currentSceneName;

        private void Awake()
        {
            if (Instance == null)
            {
                transform.parent = null;
                DontDestroyOnLoad(gameObject);
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (CurrentGameState != _nextState)
            {
                if (TransitionManager.Instance.GetInTransition() && TransitionManager.Instance.GetInMiddlePoint())
                {
                    if (_nextSceneName != _currentSceneName)
                    {
                        _currentSceneName = _nextSceneName;
                        CurrentGameState = _nextState;
                        SceneManager.LoadScene(_nextSceneName.ToString());
                    }
                    else
                    {
                        CurrentGameState = _nextState;
                    }
                }
            }
        }

        public void ChangeGameState(GameState nextGameState, float transitionTimeInSeconds, SceneName nextScene = SceneName.None)
        {
            //We want to change scene
            if (nextScene != SceneName.None)
            {
                if (transitionTimeInSeconds < 0.001f)
                {
                    SceneManager.LoadScene(nextScene.ToString());
                }
                else
                {
                    TransitionManager.Instance.OrderTransition(transitionTimeInSeconds);
                    _nextState = nextGameState;
                    _nextSceneName = nextScene;
                }
            }
            //We DON'T want to change scene
            else
            {
                if (transitionTimeInSeconds < 0.001f)
                {
                    CurrentGameState = nextGameState;
                }
                else
                {
                    TransitionManager.Instance.OrderTransition(transitionTimeInSeconds);
                    _nextState = nextGameState;
                }
            }
        }
    }
}

/*

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace gbpickup
{
    public class StateManager : MonoBehaviour
    {
        public static StateManager Instance;

        [SerializeField] private GameState _currentGameState;
        [SerializeField] private GameState _nextGameState;
        [SerializeField] private SubState _subState;
        [SerializeField] private SceneName _nextSceneName;
        [SerializeField] private bool _inTransition;

        public bool DifficultySelected;


        private void Awake()
        {
            if (Instance == null)
            {
                transform.parent = null;
                DontDestroyOnLoad(gameObject);
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            switch (_currentGameState)
            {
                case GameState.Loading:
                    LoadingState();
                    break;
                case GameState.DifficultySelection:
                    DifficultySelectionState();
                    break;
                case GameState.ModelShowDown:
                    ModelShowDownState();
                    break;
                case GameState.PickUp:
                    PickUpState();
                    break;
                case GameState.RoundScore:
                    RoundScoreState();
                    break;
            }

            if (_currentGameState != _nextGameState)
            {
                if (TransitionManager.Instance.GetInTransition() && TransitionManager.Instance.GetInMiddlePoint())
                {
                    ExecuteStateTransition();
                }
                else if (!_inTransition)
                {
                    ExecuteStateTransition();
                }
            }
        }

        public void ChangeGameState(GameState nextGameState, float transitionTimeInSeconds, SceneName nextScene = SceneName.None, bool instant = true,
            TransitionMessage message = TransitionMessage.None, bool particles = false)
        {
            TransitionManager.Instance.OrderTransition(transitionTimeInSeconds, message, particles);
            _nextGameState = nextGameState;
            _nextSceneName = nextScene;
            _inTransition = !instant;
        }

        private void ExecuteStateTransition()
        {
            _currentGameState = _nextGameState;
            _subState = SubState.BeginState;
            if (_nextSceneName != SceneName.None)
            {
                SceneManager.LoadScene(_nextSceneName.ToString());
                _nextSceneName = SceneName.None;
                _inTransition = false;
            }
        }

        private void LoadingState()
        {
            ChangeGameState(GameState.DifficultySelection, 0f, SceneName.DifficultySelection);
        }

        private void DifficultySelectionState()
        {
            switch (_subState)
            {
                case SubState.BeginState:
                    GlobalUIManager.Instance.ShowTimeBar = false;
                    CoreLoopManager.Instance.DisableScoreView();
                    _subState = SubState.RunState;
                    break;
                case SubState.RunState:
                    if (DifficultySelected)
                    {
                        _subState = SubState.ExitState;
                    }
                    break;
                case SubState.ExitState:
                    if (DifficultySelected)
                    {
                        CoreLoopManager.Instance.CreateGame(true);
                        ChangeGameState(GameState.ModelShowDown, 5f, SceneName.Coreloop, false, TransitionMessage.GetReady, true);
                        DifficultySelected = false;
                    }
                    break;

            }
        }

        private void ModelShowDownState()
        {
            switch (_subState)
            {
                case SubState.BeginState:
                    CoreLoopManager.Instance.CreateGame(false);
                    CoreLoopManager.Instance.StartObjectShowDown();
                    GlobalUIManager.Instance.ShowTimeBar = true;
                    _subState = SubState.RunState;
                    break;
                case SubState.RunState:
                    if (Input.GetMouseButtonDown(0) && !TransitionManager.Instance.GetInTransition())
                    {
                        CoreLoopManager.Instance.GoToNextObject();
                    }

                    if (!TransitionManager.Instance.GetInTransition() && CoreLoopManager.Instance.TimeOut)
                    {
                        ChangeGameState(GameState.RoundScore, 2f, SceneName.None, false, TransitionMessage.TimeOut);
                        CoreLoopManager.Instance.ResetShowDown();
                    }

                    if (CoreLoopManager.Instance.ShowDownCompleted)
                    {
                        _subState = SubState.ExitState;
                    }
                    break;
                case SubState.ExitState:
                    CoreLoopManager.Instance.ResetShowDown();
                    ChangeGameState(GameState.PickUp, 2f);
                    break;
            }
        }

        private void PickUpState()
        {
            switch (_subState)
            {
                case SubState.BeginState:
                    if (TransitionManager.Instance.GetInMiddlePoint())
                    {
                        CoreLoopManager.Instance.CreateModel(ModelCreationAlgorithm.ScalingRectangle);
                        CoreLoopManager.Instance.CreateVictoryLine();
                        CoreLoopManager.Instance.UpdateSlots();
                        _subState = SubState.RunState;
                    }
                    break;
                case SubState.RunState:
                    if (!TransitionManager.Instance.GetInTransition() && CoreLoopManager.Instance.TimeOut)
                    {
                        CoreLoopManager.Instance.TestLine();
                        ChangeGameState(GameState.RoundScore, 2f, SceneName.None, false, TransitionMessage.TimeOut);
                        CoreLoopManager.Instance.CleanPickUp();
                    }

                    if (CoreLoopManager.Instance.LineComplete)
                    {
                        CoreLoopManager.Instance.TestLine();
                        _subState = SubState.ExitState;
                    }
                    break;
                case SubState.ExitState:
                    CoreLoopManager.Instance.CleanPickUp();
                    StateManager.Instance.ChangeGameState(GameState.ModelShowDown, 1f);
                    break;
            }
        }

        private void RoundScoreState()
        {
            switch (_subState)
            {
                case SubState.BeginState:
                    GlobalUIManager.Instance.ShowTimeBar = false;
                    CoreLoopManager.Instance.ShowEndScore();
                    _subState = SubState.RunState;
                    break;
                case SubState.RunState:
                    if (CoreLoopManager.Instance.StartNewGame)
                    {
                        _subState = SubState.ExitState;
                    }
                    break;
                case SubState.ExitState:
                    CoreLoopManager.Instance.ExitScoreView();
                    ChangeGameState(GameState.DifficultySelection, 2f, SceneName.DifficultySelection, false, TransitionMessage.None);
                    break;
            }
        }

        public GameState GetGameState()
        {
            return _currentGameState;
        }
    }
}

*/

