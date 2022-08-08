using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeoDefence
{
    public class MainMenuController : MonoBehaviour
    {
        public void StartNewGame()
        {
            if (TransitionManager.Instance.GetInTransition())
            {
                return;
            }

            StateManager.Instance.ChangeGameState(GameState.Game, 2f, SceneName.BattleScene);
        }

        public void GoToStore()
        {
            if (TransitionManager.Instance.GetInTransition())
            {
                return;
            }

            StateManager.Instance.ChangeGameState(GameState.DemoStore, 1f, SceneName.StoreExample_2021);
        }

        public void ShowHighScore()
        {
            if (TransitionManager.Instance.GetInTransition())
            {
                return;
            }
        }
    }
}

