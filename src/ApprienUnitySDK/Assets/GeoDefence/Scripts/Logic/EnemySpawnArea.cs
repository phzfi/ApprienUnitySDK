using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeoDefence
{
    public class EnemySpawnArea : MonoBehaviour
    {
        [SerializeField] private Bounds _area;
        [SerializeField] private Collider _collider;

        private void Awake()
        {
            _area = transform.GetComponent<Collider>().bounds;
            _collider = transform.GetComponent<Collider>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                SpawnEnemy();
            }
        }

        public void SpawnEnemy()
        {
            var extentsX = _area.extents.x;
            var extentsZ = _area.extents.z;
            var spawnPos = new Vector3(transform.position.x - extentsX + Random.Range(0, extentsX * 2), transform.position.y, transform.position.z - extentsZ + Random.Range(0, extentsZ * 2));
            Instantiate(GeneralDataStore.Instance.GetEnemyPrefab(), spawnPos, Quaternion.identity);
        }
    }
}

