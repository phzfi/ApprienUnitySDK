using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeoDefence
{
    public enum GameState
    {
        LoadState = 0,
        MainMenu = 1,
        DemoStore = 2,
        HighScoreTable = 3,
        Game = 4,
    }

    //Values equals for game states where state happens
    public enum SceneName
    {
        LoadScene = 0,
        MainMenu = 1,
        StoreExample_2021 = 2,
        BattleScene = 4,
    }
    //Lets make score table as UI panel
}
