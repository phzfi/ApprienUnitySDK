using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeoDefence
{
    public class GamePlayManager : MonoBehaviour
    {
        [SerializeField] private List<EnemySpawnArea> _spawnAreas;
        private GeneralDataStore _dataStore => GeneralDataStore.Instance;

        private float _timeToNextSpawn;
        private float _spawnTimer;
        private float _escalation;

        private void Awake()
        {
            StartSpawningMechanic();
        }

        private void StartSpawningMechanic()
        {
            _timeToNextSpawn = _dataStore.GetStaticSpawnTime() + RandomSpawnTime();
        }

        /*
        private float SetTimeToNextSpawn()
        {
            _timeToNextSpawn = _dataStore.GetStaticSpawnTime() + RandomSpawnTime() - ;
        }
        */

        private float RandomSpawnTime()
        {
            return Random.Range(0, _dataStore.GetRandomSpawnTime());
        }

        private void Update()
        {
            
        }
    }
}

