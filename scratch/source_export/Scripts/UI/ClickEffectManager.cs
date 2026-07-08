using UnityEngine;
using System.Collections.Generic;

namespace GameClient.UI
{
    public class ClickEffectManager : Singleton<ClickEffectManager>
    {
        [Header("Cấu hình")]
        public GameObject flowerPrefab; // Prefab bông hoa có gắn Animator
        public int poolSize = 10;

        private List<GameObject> _pool = new List<GameObject>();
        private int _currentIndex = 0;

        protected override void Awake()
        {
            base.Awake();
            InitPool();
        }

        private void InitPool()
        {
            for (int i = 0; i < poolSize; i++)
            {
                GameObject obj = Instantiate(flowerPrefab, transform);
                obj.SetActive(false);
                _pool.Add(obj);
            }
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                SpawnEffect(Input.mousePosition);
            }
        }

        private void SpawnEffect(Vector2 screenPos)
        {
            GameObject obj = _pool[_currentIndex];
            
            obj.transform.position = screenPos; 
            
            obj.SetActive(false); // Reset
            obj.SetActive(true);  // Kích hoạt lại để chạy Animation từ đầu

            _currentIndex = (_currentIndex + 1) % poolSize;
        }
    }
}
