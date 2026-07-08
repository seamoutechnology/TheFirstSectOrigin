using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using GameClient.Managers;
using DG.Tweening;

namespace GameClient.UI.Effects
{
    public class TouchEffectManager : Singleton<TouchEffectManager>
    {
        [Header("Settings")]
        public GameObject effectPrefab; 
        public int poolSize = 10;
        public Canvas targetCanvas; // Canvas hiển thị hiệu ứng

        private List<GameObject> _pool = new List<GameObject>();
        private List<Sprite> _sharedSprites = new List<Sprite>();

        protected override void Awake()
        {
            base.Awake();
            
            if (targetCanvas == null || targetCanvas.gameObject.scene.name != "DontDestroyOnLoad")
            {
                GameObject canvasObj = new GameObject("TouchEffectCanvas_Auto");
                canvasObj.transform.SetParent(this.transform); // Làm con của Manager để được DontDestroyOnLoad
                targetCanvas = canvasObj.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                targetCanvas.sortingOrder = 9999; // Đè lên tất cả UI khác
                
                var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
            }

            var raycaster = targetCanvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster != null) raycaster.enabled = false;
            
            var group = targetCanvas.GetComponent<CanvasGroup>();
            if (group == null) group = targetCanvas.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            PrepareSprites();
            CreatePool();
        }

        private void PrepareSprites()
        {
            if (effectPrefab == null) return;
            
            var player = effectPrefab.GetComponent<SpriteSequencePlayer>();
            if (player == null) return;

            if (player.spriteAtlas != null)
            {
                Sprite[] atlasSprites = new Sprite[player.spriteAtlas.spriteCount];
                player.spriteAtlas.GetSprites(atlasSprites);
                _sharedSprites.AddRange(atlasSprites);
                
                _sharedSprites.Sort((a, b) => {
                    var matchesA = System.Text.RegularExpressions.Regex.Matches(a.name, @"\d+");
                    var matchesB = System.Text.RegularExpressions.Regex.Matches(b.name, @"\d+");
                    
                    int count = Mathf.Min(matchesA.Count, matchesB.Count);
                    for (int i = 0; i < count; i++)
                    {
                        int i1 = int.Parse(matchesA[i].Value);
                        int i2 = int.Parse(matchesB[i].Value);
                        if (i1 != i2) return i1.CompareTo(i2);
                    }
                    return matchesA.Count.CompareTo(matchesB.Count);
                });
            }
            else if (player.sprites != null && player.sprites.Count > 0)
            {
                _sharedSprites.AddRange(player.sprites);
            }
        }

        private void CreatePool()
        {
            if (effectPrefab == null) return;

            for (int i = 0; i < poolSize; i++)
            {
                GameObject go = Instantiate(effectPrefab, targetCanvas.transform);
                var player = go.GetComponent<SpriteSequencePlayer>();
                
                if (_sharedSprites.Count > 0) player.SetSprites(_sharedSprites);
                
                go.SetActive(false);
                _pool.Add(go);
            }
        }

        private void Update()
        {
            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            {
                Vector2 position = Pointer.current.position.ReadValue();
                SpawnEffect(position);
            }
        }

        private void SpawnEffect(Vector2 screenPos)
        {
            _pool.RemoveAll(item => item == null);
            GameObject effect = _pool.Find(go => !go.activeSelf);
            
            if (effect == null)
            {
                effect = Instantiate(effectPrefab, targetCanvas.transform);
                _pool.Add(effect);
                
                var player = effect.GetComponent<SpriteSequencePlayer>();
                if (_sharedSprites.Count > 0) player.SetSprites(_sharedSprites);
            }

            if (effect != null)
            {
                effect.transform.position = screenPos;
                
                effect.transform.rotation = Quaternion.identity;
                effect.transform.localScale = Vector3.one;

                effect.SetActive(true);
                
                var player = effect.GetComponent<SpriteSequencePlayer>();
                player.Play();

                float duration = player.sprites.Count > 0 ? (player.sprites.Count / player.fps) : 1f;
                DG.Tweening.DOVirtual.DelayedCall(duration, () => 
                {
                    if (effect != null) effect.SetActive(false);
                }).SetId(effect).SetUpdate(true);
            }
        }
    }
}
