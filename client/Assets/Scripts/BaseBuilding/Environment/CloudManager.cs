using UnityEngine;
using System.Collections.Generic;
using GameClient.BaseBuilding.Core;
using GameClient.Gameplay.BaseBuilder;

namespace GameClient.BaseBuilding.Environment
{
    public class CloudManager : MonoBehaviour
    {
        [Header("Cloud Settings")]
        public int MaxClouds = 15;
        public float SpawnInterval = 5f;
        public float CloudSpeedMin = 0.5f;
        public float CloudSpeedMax = 2f;
        public Vector2 MapBoundsMin = new Vector2(-50, -50);
        public Vector2 MapBoundsMax = new Vector2(50, 50);

        [Header("Cloud Visuals")]
        public List<GameObject> CloudPrefabs;

        private float _spawnTimer;
        private List<GameObject> _activeClouds = new List<GameObject>();

        private void Start()
        {
            UpdateBounds();
            int initialClouds = MaxClouds / 2;
            for (int i = 0; i < initialClouds; i++)
            {
                SpawnCloud(true);
            }
            _spawnTimer = SpawnInterval;
        }

        private void UpdateBounds()
        {
            if (CameraController.Instance != null)
            {
                MapBoundsMin = CameraController.Instance.minBounds;
                MapBoundsMax = CameraController.Instance.maxBounds;
            }
            else if (BaseGridManager.Instance != null)
            {
                MapBoundsMin = new Vector2(-2f, -2f);
                MapBoundsMax = new Vector2(BaseGridManager.Instance.Width + 2f, BaseGridManager.Instance.Height + 2f);
            }
        }

        private void Update()
        {
            UpdateBounds();
            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0 && _activeClouds.Count < MaxClouds)
            {
                SpawnCloud(false);
                _spawnTimer = SpawnInterval;
            }

            UpdateClouds();
        }

        private void SpawnCloud(bool distributeFullMap)
        {
            GameObject cloud;
            bool isPrefab = CloudPrefabs != null && CloudPrefabs.Count > 0;

            if (isPrefab)
            {
                GameObject randomPrefab = CloudPrefabs[Random.Range(0, CloudPrefabs.Count)];
                cloud = Instantiate(randomPrefab, this.transform);
                cloud.name = $"Cloud_{_activeClouds.Count}";
            }
            else
            {
                cloud = new GameObject($"Cloud_{_activeClouds.Count}");
                cloud.transform.SetParent(this.transform);
            }

            float startX;
            if (distributeFullMap)
            {
                startX = Random.Range(MapBoundsMin.x, MapBoundsMax.x);
            }
            else
            {
                startX = Random.Range(MapBoundsMin.x - 10f, MapBoundsMin.x - 2f);
            }

            float startY = Random.Range(MapBoundsMin.y, MapBoundsMax.y);
            float startZ = -5f; // Nổi lên trên một chút so với map

            cloud.transform.position = new Vector3(startX, startY, startZ);
            
            cloud.transform.rotation = Quaternion.identity;

            if (!isPrefab)
            {
                var sr = cloud.AddComponent<SpriteRenderer>();
                
                Texture2D tex = new Texture2D(128, 64);
                Color[] colors = new Color[128 * 64];
                for (int i = 0; i < colors.Length; i++)
                {
                    colors[i] = new Color(1f, 1f, 1f, Random.Range(0.2f, 0.4f)); 
                }
                tex.SetPixels(colors);
                tex.Apply();
                
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 128, 64), new Vector2(0.5f, 0.5f), 32f);
                sr.sortingOrder = 1000; // Nổi trên các công trình
            }

            var cloudInfo = cloud.AddComponent<CloudInfo>();
            cloudInfo.Speed = Random.Range(CloudSpeedMin, CloudSpeedMax);
            
            cloudInfo.Direction = new Vector3(1f, 1f, 0).normalized;

            var srs = cloud.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in srs)
            {
                Color c = sr.color;
                c.a = Mathf.Min(c.a, 0.45f); // Giới hạn độ đục tối đa 45%
                sr.color = c;
            }

            _activeClouds.Add(cloud);
        }

        private void UpdateClouds()
        {
            for (int i = _activeClouds.Count - 1; i >= 0; i--)
            {
                var cloud = _activeClouds[i];
                var info = cloud.GetComponent<CloudInfo>();
                
                cloud.transform.position += info.Direction * info.Speed * Time.deltaTime;

                if (cloud.transform.position.x > MapBoundsMax.x || cloud.transform.position.y > MapBoundsMax.y)
                {
                    _activeClouds.RemoveAt(i);
                    Destroy(cloud);
                }
            }
        }
    }

    public class CloudInfo : MonoBehaviour
    {
        public float Speed;
        public Vector3 Direction;
    }
}
