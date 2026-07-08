using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using GameClient.Core;
using DG.Tweening;

namespace GameClient.UI.Effects
{
    public enum FloatingTextType
    {
        Damage,
        Critical,
        Heal,
        Miss,
        Buff,
        Debuff,
        ResourceNormal, // Thu hoạch bình thường (xanh)
        ResourceCrit,   // Thu hoạch bạo kích (vàng)
        ResourceMax,    // Thu hoạch tối đa (đỏ)
        ResourceLoss    // Mất tài nguyên/tiền (trắng)
    }

    public class FloatingTextManager : Singleton<FloatingTextManager>
    {
        [SerializeField] private GameObject floatingTextPrefab;
        [SerializeField] private Transform canvasTransform;

        private Queue<GameObject> _pool = new Queue<GameObject>();

        public void SpawnText(Vector3 worldPosition, string textValue, FloatingTextType type)
        {
            GameObject go = GetFromPool();
            go.transform.SetParent(canvasTransform, false);
            
            go.transform.position = worldPosition + new Vector3(0, 1.5f, 0); 

            TMP_Text text = go.GetComponent<TMP_Text>();
            text.text = textValue;
            
            switch (type)
            {
                case FloatingTextType.Damage:
                    text.color = Color.white;
                    text.fontSize = 35;
                    break;
                case FloatingTextType.Critical:
                    text.color = Color.yellow;
                    text.fontSize = 50;
                    break;
                case FloatingTextType.Heal:
                    text.color = Color.green;
                    text.fontSize = 40;
                    text.text = "+" + textValue;
                    break;
                case FloatingTextType.Miss:
                    text.color = Color.gray;
                    text.fontSize = 30;
                    break;
                case FloatingTextType.Buff:
                    text.color = Color.cyan;
                    text.fontSize = 30;
                    break;
                case FloatingTextType.Debuff:
                    text.color = Color.magenta;
                    text.fontSize = 30;
                    break;
                case FloatingTextType.ResourceNormal:
                    text.color = Color.green;
                    text.fontSize = 35;
                    text.text = "+" + textValue;
                    break;
                case FloatingTextType.ResourceCrit:
                    text.color = Color.yellow;
                    text.fontSize = 45; // To hơn một chút
                    text.text = "+" + textValue + "!";
                    break;
                case FloatingTextType.ResourceMax:
                    text.color = Color.red;
                    text.fontSize = 55; // Rất to
                    text.text = "MAX +" + textValue;
                    break;
                case FloatingTextType.ResourceLoss:
                    text.color = Color.white;
                    text.fontSize = 35;
                    text.text = "-" + textValue;
                    break;
            }

            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 1f;

            float duration = 0.8f;
            Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), 2f, 0); 
            Vector3 targetPos = go.transform.position + randomOffset;

            go.transform.localScale = Vector3.one;

            Sequence seq = DOTween.Sequence();
            
            if (type == FloatingTextType.Critical)
            {
                go.transform.localScale = Vector3.zero;
                seq.Append(go.transform.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack));
                seq.Append(go.transform.DOScale(1f, 0.1f));
            }

            seq.Append(go.transform.DOMove(targetPos, duration).SetEase(Ease.OutQuad));
            seq.Join(cg.DOFade(0f, duration).SetEase(Ease.InQuad));
            seq.OnComplete(() => ReturnToPool(go));
        }

        public void SpawnDamage(Vector3 worldPosition, int amount, bool isCrit = false)
        {
            SpawnText(worldPosition, amount.ToString(), isCrit ? FloatingTextType.Critical : FloatingTextType.Damage);
        }

        public void SpawnHeal(Vector3 worldPosition, int amount)
        {
            SpawnText(worldPosition, amount.ToString(), FloatingTextType.Heal);
        }

        public void SpawnResource(Vector3 worldPosition, string resourceName, int amount, FloatingTextType resourceType = FloatingTextType.ResourceNormal)
        {
            SpawnText(worldPosition, $"{amount} {resourceName}", resourceType);
        }

        private GameObject GetFromPool()
        {
            if (_pool.Count > 0)
            {
                GameObject go = _pool.Dequeue();
                go.SetActive(true);
                return go;
            }
            return Instantiate(floatingTextPrefab);
        }

        private void ReturnToPool(GameObject go)
        {
            go.SetActive(false);
            _pool.Enqueue(go);
        }
    }
}
