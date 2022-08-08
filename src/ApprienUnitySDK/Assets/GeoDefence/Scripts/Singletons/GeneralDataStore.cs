using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeoDefence
{
    public class GeneralDataStore : MonoBehaviour
    {
        public static GeneralDataStore Instance;
        [SerializeField] private StaticDataStore _staticDataStore;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                transform.parent = null;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public GameObject GetEnemyPrefab()
        {
            return _staticDataStore.EnemyPrefab;
        }

        public float GetStaticSpawnTime()
        {
            return _staticDataStore.StaticSpawnTime;
        }

        public float GetRandomSpawnTime()
        {
            return _staticDataStore.RandomSpawnTime;
        }

        public float GetEsclationStep()
        {
            return _staticDataStore.Escalation;
        }
    }
}

