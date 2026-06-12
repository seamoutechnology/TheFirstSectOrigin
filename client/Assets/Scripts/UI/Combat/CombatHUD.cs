using UnityEngine;
using UnityEngine.UI;
using GameClient.Gameplay.Combat;
using GameClient.Gameplay.Combat.Skills;
using GameClient.UI;
using GameClient.UI.Core;
using System.Collections.Generic;
using System.Linq;

namespace GameClient.UI.Combat
{
    public class CombatHUD : BaseUIPanel
    {
        [Header("Text references")]
        public TMPro.TMP_Text txtTurnInfo;

        [Header("Containers")]
        public Transform playerStatusContainer;
        public Transform enemyStatusContainer;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject characterItemPrefab;

        [Header("Skills Panel")]
        public GameObject skillPanel;

        private CombatManager _combatManager;
        private List<SkillData> _availableSkills;
        private Dictionary<CombatEntity, CombatHUDCharacterItem> _characterItems = new();

        public override void Setup(object data = null)
        {
            base.Setup(data);
            Initialize(CombatManager.Instance);
        }

        public void Initialize(CombatManager combatManager)
        {
            if (combatManager == null) return;
            
            _combatManager = combatManager;
            _combatManager.OnTurnStarted -= HandleTurnStarted;
            _combatManager.OnTurnStarted += HandleTurnStarted;
            _combatManager.OnCombatEnded -= HandleCombatEnded;
            _combatManager.OnCombatEnded += HandleCombatEnded;

            _availableSkills = new List<SkillData>();

            var basicAttack = ScriptableObject.CreateInstance<SkillData>();
            basicAttack.SkillID = "basic_attack";
            basicAttack.SkillName = "Đánh Thường";
            basicAttack.PrimaryEffect = SkillEffectType.Damage;
            basicAttack.PrimaryMultiplier = 1.0f;
            _availableSkills.Add(basicAttack);

            var luyenKhi = ScriptableObject.CreateInstance<SkillData>();
            luyenKhi.SkillID = "luyen_khi";
            luyenKhi.SkillName = "Luyện Khí";
            luyenKhi.IsSupport = true;
            luyenKhi.PrimaryEffect = SkillEffectType.RestoreMP;
            luyenKhi.PrimaryFlatBonus = 50;
            luyenKhi.SecondaryEffect = SkillEffectType.BuffAttack;
            luyenKhi.SecondaryFlatBonus = 20;
            _availableSkills.Add(luyenKhi);

            var vanKiemQuyet = ScriptableObject.CreateInstance<SkillData>();
            vanKiemQuyet.SkillID = "van_kiem_quyet";
            vanKiemQuyet.SkillName = "Vạn Kiếm Quyết";
            vanKiemQuyet.IsAoE = true;
            vanKiemQuyet.PrimaryEffect = SkillEffectType.Damage;
            vanKiemQuyet.PrimaryMultiplier = 0.8f;
            _availableSkills.Add(vanKiemQuyet);

            var hoiHuyet = ScriptableObject.CreateInstance<SkillData>();
            hoiHuyet.SkillID = "hoi_huyet";
            hoiHuyet.SkillName = "Hồi Huyết Thuật";
            hoiHuyet.IsSupport = true;
            hoiHuyet.PrimaryEffect = SkillEffectType.Heal;
            hoiHuyet.PrimaryMultiplier = 1.2f;
            _availableSkills.Add(hoiHuyet);

            var buffSkill = ScriptableObject.CreateInstance<SkillData>();
            buffSkill.SkillID = "buff_giap";
            buffSkill.SkillName = "Kim Chung Trạo";
            buffSkill.IsSupport = true;
            buffSkill.PrimaryEffect = SkillEffectType.BuffDefense;
            buffSkill.PrimaryFlatBonus = 30;
            _availableSkills.Add(buffSkill);

            BuildCharacterStatusUI();
            skillPanel.SetActive(true);
        }

        protected override void OnCleanup()
        {
            base.OnCleanup();
            if (_combatManager != null)
            {
                _combatManager.OnTurnStarted -= HandleTurnStarted;
                _combatManager.OnCombatEnded -= HandleCombatEnded;
            }
            _characterItems.Clear();
        }

        private void BuildCharacterStatusUI()
        {
            // Clear existing status items
            foreach (Transform child in playerStatusContainer) Destroy(child.gameObject);
            foreach (Transform child in enemyStatusContainer) Destroy(child.gameObject);
            _characterItems.Clear();

            if (_combatManager == null || characterItemPrefab == null) return;

            // Instantiating Player character items
            foreach (var hero in _combatManager.Players)
            {
                var go = Instantiate(characterItemPrefab, playerStatusContainer);
                var item = go.GetComponent<CombatHUDCharacterItem>();
                if (item != null)
                {
                    item.Bind(hero);
                    _characterItems[hero] = item;
                }
            }

            // Instantiating Enemy character items
            foreach (var enemy in _combatManager.Enemies)
            {
                var go = Instantiate(characterItemPrefab, enemyStatusContainer);
                var item = go.GetComponent<CombatHUDCharacterItem>();
                if (item != null)
                {
                    item.Bind(enemy);
                    _characterItems[enemy] = item;
                }
            }
        }

        private void HandleTurnStarted(CombatEntity entity)
        {
            if (txtTurnInfo != null)
            {
                txtTurnInfo.text = $"Lượt của: {entity.entityName}";
            }

            // Turn Highlight Indicator
            foreach (var kvp in _characterItems)
            {
                kvp.Value.SetHighlight(kvp.Key == entity);
            }

            // Skill panel stays active so the player can cast Sect Master skills anytime
            skillPanel.SetActive(true);
        }

        private void HandleCombatEnded()
        {
            if (txtTurnInfo != null)
            {
                txtTurnInfo.text = "Trận chiến kết thúc!";
            }
            skillPanel.SetActive(false);
            
            // Turn off highlight indicator
            foreach (var kvp in _characterItems)
            {
                kvp.Value.SetHighlight(false);
            }
        }

        public void OnSkillSelected(int skillIndex)
        {
            if (skillIndex >= 0 && skillIndex < _availableSkills.Count)
            {
                var skill = _availableSkills[skillIndex];
                
                CombatEntity target = null;
                if (!skill.IsSupport)
                {
                    target = _combatManager.Enemies.FirstOrDefault(e => !e.IsDead);
                }
                else
                {
                    target = _combatManager.Players.FirstOrDefault(p => !p.IsDead);
                }

                if (target != null || skill.IsAoE || skill.IsSupport)
                {
                    _combatManager.CastPlayerSkillInstantly(skill, target);
                }
            }
        }
    }
}
