using UnityEngine;
using UnityEngine.UI;
using GameClient.Gameplay.Combat;
using GameClient.Gameplay.Combat.Skills;
using GameClient.UI; // Assume UIManager exists
using System.Collections.Generic;

namespace GameClient.UI.Combat
{
    public class CombatHUD : MonoBehaviour
    {
        public Text txtTurnInfo;
        public Transform playerStatusContainer;
        public Transform enemyStatusContainer;
        public GameObject skillPanel;

        private CombatManager _combatManager;
        private List<SkillData> _availableSkills;

        public void Initialize(CombatManager combatManager)
        {
            _combatManager = combatManager;
            _combatManager.OnTurnStarted += HandleTurnStarted;
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

            UpdateStatusUI();
            skillPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_combatManager != null)
            {
                _combatManager.OnTurnStarted -= HandleTurnStarted;
                _combatManager.OnCombatEnded -= HandleCombatEnded;
            }
        }

        private void UpdateStatusUI()
        {
            Debug.Log("[CombatHUD] Cập nhật UI Máu/MP...");
        }

        private void HandleTurnStarted(CombatEntity entity)
        {
            txtTurnInfo.text = $"Lượt của: {entity.entityName}";

            if (entity.isPlayer)
            {
                skillPanel.SetActive(true);
            }
            else
            {
                skillPanel.SetActive(false);
            }

            UpdateStatusUI();
        }

        private void HandleCombatEnded()
        {
            txtTurnInfo.text = "Trận chiến kết thúc!";
            skillPanel.SetActive(false);
            UpdateStatusUI();
        }

        public void OnSkillSelected(int skillIndex)
        {
            if (skillIndex >= 0 && skillIndex < _availableSkills.Count)
            {
                var skill = _availableSkills[skillIndex];
                
                CombatEntity target = null;
                if (!skill.IsSupport)
                {
                    target = _combatManager.Enemies.Find(e => !e.IsDead);
                }
                else
                {
                    target = _combatManager.CurrentActiveEntity;
                }

                if (target != null || skill.IsAoE || skill.IsSupport)
                {
                    skillPanel.SetActive(false);
                    _combatManager.ExecuteAction(skill, target);
                }
            }
        }
    }
}
