using UnityEngine;
using UnityEngine.UI;
using GameClient.Gameplay.Combat;
using GameClient.Gameplay.Combat.Skills;
using GameClient.UI;
using GameClient.UI.Core;
using System.Collections.Generic;
using System.Linq;
using GameClient.Gameplay.Combat.States;

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
        [SerializeField] private List<UI_SkillButton> skillSlots = new();

        [Header("Debug/Utility Buttons")]
        public Button btnSurrender;
        public Button btnInstantWin;

        private CombatManager _combatManager;
        private List<SkillData> _availableSkills;
        private Dictionary<CombatEntity, CombatHUDCharacterItem> _characterItems = new();
        private Dictionary<string, int> _skillCooldownTracker = new();

        protected override void OnStart()
        {
            base.OnStart();
            
            // Tự động tìm kiếm nếu chưa gán trong Inspector
            if (btnSurrender == null) btnSurrender = transform.Find("btnSurrender")?.GetComponent<Button>();
            if (btnSurrender == null) btnSurrender = transform.Find("Buttons/btnSurrender")?.GetComponent<Button>();
            if (btnSurrender != null) btnSurrender.onClick.AddListener(OnSurrenderClicked);

            if (btnInstantWin == null) btnInstantWin = transform.Find("btnInstantWin")?.GetComponent<Button>();
            if (btnInstantWin == null) btnInstantWin = transform.Find("Buttons/btnInstantWin")?.GetComponent<Button>();
            if (btnInstantWin != null) btnInstantWin.onClick.AddListener(OnInstantWinClicked);
        }

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
            _skillCooldownTracker.Clear();

            // Skill 1: Hồi Xuân Chi Thuật (Hồi máu toàn đội - Cooldown: 10 lượt)
            var skillHeal = ScriptableObject.CreateInstance<SkillData>();
            skillHeal.SkillID = "master_skill_heal";
            skillHeal.SkillName = "Hồi Xuân Chi Thuật";
            skillHeal.Description = "Hồi phục lượng máu lớn cho toàn bộ đồng đội.";
            skillHeal.IsSupport = true;
            skillHeal.IsAoE = true;
            skillHeal.PrimaryEffect = SkillEffectType.Heal;
            skillHeal.PrimaryMultiplier = 1.8f;
            skillHeal.PrimaryFlatBonus = 150;
            _availableSkills.Add(skillHeal);
            _skillCooldownTracker[skillHeal.SkillID] = 0;

            // Skill 2: Thần Lôi Giáng Thế (Sát thương đơn thể lớn - Cooldown: 3 lượt)
            var skillLightning = ScriptableObject.CreateInstance<SkillData>();
            skillLightning.SkillID = "master_skill_lightning";
            skillLightning.SkillName = "Thần Lôi Giáng Thế";
            skillLightning.Description = "Triệu hồi sấm sét oanh tạc gây lượng sát thương đơn thể cực lớn.";
            skillLightning.PrimaryEffect = SkillEffectType.Damage;
            skillLightning.PrimaryMultiplier = 2.5f;
            _availableSkills.Add(skillLightning);
            _skillCooldownTracker[skillLightning.SkillID] = 0;

            // Skill 3: Vạn Kiếm Quy Tông (Sát thương AoE - Cooldown: 5 lượt)
            var skillSwords = ScriptableObject.CreateInstance<SkillData>();
            skillSwords.SkillID = "master_skill_swords";
            skillSwords.SkillName = "Vạn Kiếm Quy Tông";
            skillSwords.Description = "Phóng vạn thanh kiếm tấn công toàn bộ kẻ địch.";
            skillSwords.IsAoE = true;
            skillSwords.PrimaryEffect = SkillEffectType.Damage;
            skillSwords.PrimaryMultiplier = 1.2f;
            _availableSkills.Add(skillSwords);
            _skillCooldownTracker[skillSwords.SkillID] = 0;

            // Skill 4: Kim Chung Trạo (Tăng thủ toàn đội - Cooldown: 4 lượt)
            var skillShield = ScriptableObject.CreateInstance<SkillData>();
            skillShield.SkillID = "master_skill_buff";
            skillShield.SkillName = "Kim Chung Trạo";
            skillShield.Description = "Bảo hộ toàn đội, tăng mạnh Phòng Thủ.";
            skillShield.IsSupport = true;
            skillShield.IsAoE = true;
            skillShield.PrimaryEffect = SkillEffectType.BuffDefense;
            skillShield.PrimaryFlatBonus = 60;
            _availableSkills.Add(skillShield);
            _skillCooldownTracker[skillShield.SkillID] = 0;

            BuildCharacterStatusUI();
            skillPanel.SetActive(true);
            UpdateSkillButtons();
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
            _skillCooldownTracker.Clear();
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

            // Giảm hồi chiêu của các kỹ năng khi bất kỳ tướng ta nào bắt đầu lượt của họ
            if (entity.isPlayer)
            {
                var keys = new List<string>(_skillCooldownTracker.Keys);
                foreach (var key in keys)
                {
                    if (_skillCooldownTracker[key] > 0)
                    {
                        _skillCooldownTracker[key]--;
                    }
                }
                UpdateSkillButtons();
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

        private void UpdateSkillButtons()
        {
            if (skillPanel == null) return;
            
            // 1. Ưu tiên sử dụng danh sách slot được gán thủ công qua Inspector
            if (skillSlots != null && skillSlots.Count > 0)
            {
                for (int i = 0; i < _availableSkills.Count; i++)
                {
                    if (i >= skillSlots.Count) break;
                    if (skillSlots[i] == null) continue;
                    
                    var skill = _availableSkills[i];
                    int cooldown = 0;
                    _skillCooldownTracker.TryGetValue(skill.SkillID, out cooldown);
                    
                    skillSlots[i].Setup(skill.SkillName, cooldown);
                }
                return;
            }
            
            // 2. Fallback: Tự động tìm kiếm các thành phần UI_SkillButton nếu danh sách trống
            var skillBtns = skillPanel.GetComponentsInChildren<UI_SkillButton>(true);
            if (skillBtns != null && skillBtns.Length > 0)
            {
                for (int i = 0; i < _availableSkills.Count; i++)
                {
                    if (i >= skillBtns.Length) break;
                    var skill = _availableSkills[i];
                    int cooldown = 0;
                    _skillCooldownTracker.TryGetValue(skill.SkillID, out cooldown);
                    
                    skillBtns[i].Setup(skill.SkillName, cooldown);
                }
                return;
            }

            // Fallback: Tìm nút button truyền thống nếu không có UI_SkillButton
            var buttons = skillPanel.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < _availableSkills.Count; i++)
            {
                if (i >= buttons.Length) break;
                var btn = buttons[i];
                var skill = _availableSkills[i];
                int cooldown = 0;
                _skillCooldownTracker.TryGetValue(skill.SkillID, out cooldown);
                
                var txt = btn.GetComponentInChildren<TMPro.TMP_Text>();
                
                if (cooldown > 0)
                {
                    btn.interactable = false;
                    if (txt != null)
                    {
                        txt.text = $"{skill.SkillName} ({cooldown}L)";
                    }
                }
                else
                {
                    btn.interactable = true;
                    if (txt != null)
                    {
                        txt.text = skill.SkillName;
                    }
                }
            }
        }

        public void OnSkillSelected(int skillIndex)
        {
            if (skillIndex >= 0 && skillIndex < _availableSkills.Count)
            {
                var skill = _availableSkills[skillIndex];
                
                int cooldown = 0;
                _skillCooldownTracker.TryGetValue(skill.SkillID, out cooldown);
                if (cooldown > 0)
                {
                    UIManager.Instance.ShowMessage("Kỹ Năng Đang Hồi", $"Kỹ năng {skill.SkillName} đang trong thời gian hồi chiêu! Cần thêm {cooldown} lượt nữa để hồi phục.");
                    return;
                }

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
                    
                    // Thiết lập hồi chiêu sau khi thi triển
                    if (skill.SkillID == "master_skill_heal") _skillCooldownTracker[skill.SkillID] = 10;
                    else if (skill.SkillID == "master_skill_lightning") _skillCooldownTracker[skill.SkillID] = 3;
                    else if (skill.SkillID == "master_skill_swords") _skillCooldownTracker[skill.SkillID] = 5;
                    else if (skill.SkillID == "master_skill_buff") _skillCooldownTracker[skill.SkillID] = 4;

                    UpdateSkillButtons();
                }
            }
        }

        private void OnSurrenderClicked()
        {
            if (_combatManager == null) return;
            Debug.Log("[CombatHUD] Surrendering, dealing lethal damage to players.");
            foreach (var p in _combatManager.Players)
            {
                if (!p.IsDead) p.TakeDamage(999999, false);
            }
            _combatManager.StateMachine.ChangeState(new GameOverState());
        }

        private void OnInstantWinClicked()
        {
            if (_combatManager == null) return;
            Debug.Log("[CombatHUD] Cheat instant win, dealing lethal damage to enemies.");
            foreach (var e in _combatManager.Enemies)
            {
                if (!e.IsDead) e.TakeDamage(999999, false);
            }
            _combatManager.StateMachine.ChangeState(new GameOverState());
        }
    }
}
