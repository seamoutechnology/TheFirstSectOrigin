using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GameClient.UI;
using GameClient.UI.Combat;
using GameClient.Managers;
using GameClient.Gameplay.World;
using DG.Tweening;
using UnityEngine.AddressableAssets;

namespace GameClient.Gameplay.Combat
{
    public class FakeCombatSimulator : MonoBehaviour
    {
        [Header("Spawn Positions")]
        [SerializeField] private Transform[] playerPositions; // Size 5
        [SerializeField] private Transform enemyPosition;    // 1 position for the Boss bot

        [Header("Default Assets")]
        [SerializeField] private Sprite fallbackHeroSprite;
        [SerializeField] private Sprite enemyBotSprite;

        [Header("Simulation Settings")]
        [SerializeField] private bool forceVictory = true;
        [SerializeField] private float actionDelay = 1.2f;

        private List<SpriteRenderer> _playerRenderers = new List<SpriteRenderer>();
        private SpriteRenderer _enemyRenderer;

        private int _bossMaxHP = 3000;
        private int _bossCurrentHP;

        private int[] _playerMaxHPs = new int[5];
        private int[] _playerCurrentHPs = new int[5];

        private async void Start()
        {
            _bossCurrentHP = _bossMaxHP;

            // 1. Spawning Player Team (5 Heroes)
            List<long> selectedHeroIds = CombatStartData.SelectedHeroIds;
            if (selectedHeroIds == null || selectedHeroIds.Count == 0)
            {
                // Fallback dummy selection if loaded directly in Scene
                selectedHeroIds = new List<long> { 1, 2, 3, 4, 5 };
            }

            for (int i = 0; i < playerPositions.Length; i++)
            {
                if (i >= selectedHeroIds.Count) break;

                // Create SpriteRenderer GameObject
                GameObject go = new GameObject($"FakeHero_{i}");
                go.transform.position = playerPositions[i].position;
                go.transform.SetParent(transform);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = fallbackHeroSprite;
                _playerRenderers.Add(sr);

                _playerMaxHPs[i] = 1000;
                _playerCurrentHPs[i] = 1000;

                // Load real hero icon if config exists
                long heroId = selectedHeroIds[i];
                var heroInstance = GameManager.Instance.PlayerHeroes?.Find(h => h.Id == heroId);
                if (heroInstance != null)
                {
                    var config = HeroDataManager.Instance.GetHeroConfigByName(heroInstance.Name);
                    if (config == null) config = HeroDataManager.Instance.GetHeroConfig(heroInstance.Id);

                    if (config != null && !string.IsNullOrEmpty(config.iconAddress))
                    {
                        try
                        {
                            var sprite = await Addressables.LoadAssetAsync<Sprite>(config.iconAddress).Task;
                            if (sprite != null) sr.sprite = sprite;
                        }
                        catch (System.Exception)
                        {
                            // fallback
                        }
                    }
                }
            }

            // 2. Spawning Boss Bot
            GameObject bossGo = new GameObject("FakeBoss");
            bossGo.transform.position = enemyPosition.position;
            bossGo.transform.SetParent(transform);

            _enemyRenderer = bossGo.AddComponent<SpriteRenderer>();
            _enemyRenderer.sprite = enemyBotSprite;
            // Flip x to face the players
            _enemyRenderer.flipX = true;

            // 3. Open Combat HUD (shows skill buttons, logs, etc.)
            UIManager.Instance.OpenPanel("CombatHUD");

            // 4. Start Simulation Loop
            StartCoroutine(Co_CombatSimulationLoop());
        }

        private IEnumerator Co_CombatSimulationLoop()
        {
            yield return new WaitForSeconds(1.5f);

            int round = 1;
            while (_bossCurrentHP > 0 && GetActivePlayerCount() > 0)
            {
                Debug.Log($"--- ROUND {round} START ---");

                // 1. Players attack Bot sequentially
                for (int i = 0; i < _playerRenderers.Count; i++)
                {
                    if (_playerCurrentHPs[i] <= 0) continue;
                    if (_bossCurrentHP <= 0) break;

                    yield return StartCoroutine(Co_PerformAttackAnimation(_playerRenderers[i].transform, enemyPosition.position, _enemyRenderer));
                    
                    // Subtract Boss health
                    int damage = Random.Range(300, 600);
                    _bossCurrentHP = Mathf.Max(0, _bossCurrentHP - damage);
                    Debug.Log($"Player {i} dealt {damage} damage to Boss. Boss HP: {_bossCurrentHP}");

                    yield return new WaitForSeconds(actionDelay);
                }

                if (_bossCurrentHP <= 0) break;

                // 2. Bot attacks 1 random active Player
                int targetIndex = GetRandomActivePlayerIndex();
                if (targetIndex != -1)
                {
                    yield return StartCoroutine(Co_PerformAttackAnimation(_enemyRenderer.transform, playerPositions[targetIndex].position, _playerRenderers[targetIndex]));

                    // Subtract Player health
                    int damage = forceVictory ? Random.Range(100, 250) : Random.Range(350, 600);
                    _playerCurrentHPs[targetIndex] = Mathf.Max(0, _playerCurrentHPs[targetIndex] - damage);
                    Debug.Log($"Boss dealt {damage} damage to Player {targetIndex}. Player HP: {_playerCurrentHPs[targetIndex]}");

                    if (_playerCurrentHPs[targetIndex] <= 0)
                    {
                        // Gray out deceased hero
                        _playerRenderers[targetIndex].DOColor(Color.gray, 0.5f);
                    }

                    yield return new WaitForSeconds(actionDelay);
                }

                round++;
            }

            // 4. Combat Ended -> Trigger Win/Lose UI
            bool isVictory = _bossCurrentHP <= 0;
            Debug.Log(isVictory ? "Victory!" : "Defeat!");

            // Hide Combat HUD
            UIManager.Instance.ClosePanel("CombatHUD");

            // Open CombatResultPanel with local mock results
            UIManager.Instance.OpenPanel("CombatResultPanel", new CombatResultPanel.LocalResultData
            {
                IsVictory = isVictory,
                RewardExp = isVictory ? Random.Range(200, 500) : 0,
                RewardLinhThach = isVictory ? Random.Range(50, 150) : 0
            });
        }

        private IEnumerator Co_PerformAttackAnimation(Transform attacker, Vector3 targetPos, SpriteRenderer defenderSr)
        {
            Vector3 startPos = attacker.position;

            // Step A: Jump towards target
            attacker.DOMove(Vector3.Lerp(startPos, targetPos, 0.7f), 0.35f).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(0.35f);

            // Step B: Impact shake and flash color red
            attacker.DOPunchScale(Vector3.one * 0.2f, 0.15f);
            if (defenderSr != null)
            {
                defenderSr.transform.DOShakePosition(0.2f, 0.15f, 10);
                defenderSr.DOColor(Color.red, 0.1f).OnComplete(() => defenderSr.DOColor(Color.white, 0.15f));
            }
            yield return new WaitForSeconds(0.2f);

            // Step C: Jump back home
            attacker.DOMove(startPos, 0.3f).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(0.3f);
        }

        private int GetActivePlayerCount()
        {
            int count = 0;
            for (int i = 0; i < _playerCurrentHPs.Length; i++)
            {
                if (i < _playerRenderers.Count && _playerCurrentHPs[i] > 0) count++;
            }
            return count;
        }

        private int GetRandomActivePlayerIndex()
        {
            List<int> activeIndices = new List<int>();
            for (int i = 0; i < _playerRenderers.Count; i++)
            {
                if (_playerCurrentHPs[i] > 0) activeIndices.Add(i);
            }

            if (activeIndices.Count == 0) return -1;
            return activeIndices[Random.Range(0, activeIndices.Count)];
        }
    }
}
