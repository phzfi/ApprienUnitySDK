using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeoDefence
{
    [CreateAssetMenu]
    public class StaticDataStore : ScriptableObject
    {
        public GameObject EnemyPrefab;

        [Space]
        public float StaticSpawnTime;
        public float RandomSpawnTime;
        public float Escalation;
    }
}

