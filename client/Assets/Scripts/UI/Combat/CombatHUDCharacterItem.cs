using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameClient.Gameplay.Combat;
using DG.Tweening;

namespace GameClient.UI.Combat
{
    public class CombatHUDCharacterItem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text txtName;
        [SerializeField] private TMP_Text txtHP;
        [SerializeField] private Slider sliderHP;
        [SerializeField] private Slider sliderMP;
        [SerializeField] private Image imgTurnHighlight;

        private CombatEntity _entity;

        public void Bind(CombatEntity entity)
        {
            if (_entity != null)
            {
                Unbind();
            }

            _entity = entity;
            
            if (_entity != null)
            {
                _entity.OnTakeDamage += HandleTakeDamage;
                _entity.OnHealed += HandleHealed;
                _entity.OnMPChanged += HandleMPChanged;
                _entity.OnDie += HandleDie;

                txtName.text = _entity.entityName;
                UpdateUI(instant: true);
            }
        }

        public void SetHighlight(bool isActive)
        {
            if (imgTurnHighlight != null)
            {
                imgTurnHighlight.gameObject.SetActive(isActive);
            }
        }

        private void Unbind()
        {
            if (_entity != null)
            {
                _entity.OnTakeDamage -= HandleTakeDamage;
                _entity.OnHealed -= HandleHealed;
                _entity.OnMPChanged -= HandleMPChanged;
                _entity.OnDie -= HandleDie;
                _entity = null;
            }
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void HandleTakeDamage(int damage, bool isCrit)
        {
            UpdateUI(instant: false);
            
            // Subtle shake effect on hit
            transform.DOComplete();
            transform.DOShakePosition(0.2f, strength: 10f, vibrato: 10);
        }

        private void HandleHealed(int amount)
        {
            UpdateUI(instant: false);
        }

        private void HandleMPChanged(int amount)
        {
            UpdateUI(instant: false);
        }

        private void HandleDie()
        {
            UpdateUI(instant: false);
            // Gray out card or fade alpha
            CanvasGroup group = GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = gameObject.AddComponent<CanvasGroup>();
            }
            group.DOFade(0.5f, 0.5f);
        }

        private void UpdateUI(bool instant)
        {
            if (_entity == null) return;

            // HP text: e.g., "500/1000"
            if (txtHP != null)
            {
                txtHP.text = $"{_entity.currentHP}/{_entity.maxHP}";
            }

            // Sliders
            float targetHPPercent = (float)_entity.currentHP / _entity.maxHP;
            float targetMPPercent = _entity.maxMP > 0 ? (float)_entity.currentMP / _entity.maxMP : 0f;

            if (sliderHP != null)
            {
                if (instant)
                {
                    sliderHP.value = targetHPPercent;
                }
                else
                {
                    sliderHP.DOKill();
                    sliderHP.DOValue(targetHPPercent, 0.25f).SetEase(Ease.OutQuad);
                }
            }

            if (sliderMP != null)
            {
                if (instant)
                {
                    sliderMP.value = targetMPPercent;
                }
                else
                {
                    sliderMP.DOKill();
                    sliderMP.DOValue(targetMPPercent, 0.25f).SetEase(Ease.OutQuad);
                }
            }
        }
    }
}
