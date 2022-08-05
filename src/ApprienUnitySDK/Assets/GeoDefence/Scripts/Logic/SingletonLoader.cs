using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeoDefence
{
    public class SingletonLoader : MonoBehaviour
    {
        private void Start()
        {
            StateManager.Instance.ChangeGameState(GameState.MainMenu, 2f, SceneName.MainMenu);
        }
    }
}

