using UnityEngine;
using DG.Tweening;

namespace GameClient.BaseBuilding.AI
{
    public class DiscipleAI : MonoBehaviour
    {
        private BaseState _currentState;
        private Animator _animator;

        public Vector2 TargetPosition { get; set; }
        public float MoveSpeed = 3f;

        [Header("Tuổi Thọ & Độ Trung Thành")]
        public int MaxLifespan = 100;
        public int CurrentLifespan = 100;
        public bool IsDead => CurrentLifespan <= 0;
        
        public int CurrentLoyalty = 100;

        private void Start()
        {
            _animator = GetComponentInChildren<Animator>();
            if (_animator == null)
            {
                Debug.LogWarning("[DiscipleAI] Không tìm thấy Animator component trên Disciple hoặc các child object.");
            }

            ChangeState(new IdleState(this));
        }

        private void Update()
        {
            _currentState?.Update();
        }

        public void ChangeState(BaseState newState)
        {
            _currentState?.Exit();
            _currentState = newState;
            _currentState?.Enter();
        }

        public void ReceiveQuest(Vector2 destination)
        {
            Debug.Log($"[DiscipleAI] Nhận lệnh di chuyển tới {destination}");
            TargetPosition = destination;
            ChangeState(new MoveState(this));
        }

        
        public void StartCombat()
        {
            ChangeState(new CombatState(this));
        }

        public void CastSkill(int skillIndex)
        {
            ChangeState(new AttackState(this, skillIndex));
        }

        public void TakeDamage()
        {
            if (_currentState is DeadState) return;
            ChangeState(new HitState(this));
        }

        public void Die()
        {
            if (_currentState is DeadState) return;
            CurrentLifespan = 0;
            ChangeState(new DeadState(this));
        }

        public void Revive(float percentLifespan = 1.0f)
        {
            CurrentLifespan = (int)(MaxLifespan * percentLifespan);
            if (CurrentLifespan > MaxLifespan) CurrentLifespan = MaxLifespan;
            if (CurrentLifespan <= 0) CurrentLifespan = 1;

            ChangeState(new ReviveState(this));
        }

        public void ConsumeLifespan(int years)
        {
            if (IsDead) return;

            CurrentLifespan -= years;
            if (CurrentLifespan <= 0)
            {
                CurrentLifespan = 0;
                Debug.Log("[DiscipleAI] Hết thọ nguyên! Đệ tử đã tọa hóa (chết).");
                Die();
            }
        }

        public void ChangeLoyalty(int amount)
        {
            if (IsDead) return;
            
            CurrentLoyalty += amount;
            CurrentLoyalty = Mathf.Clamp(CurrentLoyalty, 0, 100);

            if (CurrentLoyalty <= 0)
            {
                Betray();
            }
        }

        public void Betray()
        {
            if (_currentState is DeadState || _currentState is BetrayedState) return;
            Debug.Log("[DiscipleAI] Độ trung thành giảm xuống 0! Đệ tử đã phản bội.");
            ChangeState(new BetrayedState(this));
        }

        public void Tame(int amount)
        {
            if (_currentState is BetrayedState)
            {
                CurrentLoyalty += amount;
                Debug.Log($"[DiscipleAI] Đã thuần phục lại đệ tử, độ trung thành tăng lên {CurrentLoyalty}");
                ChangeState(new IdleState(this));
            }
        }

        public void StartCrafting()
        {
            ChangeState(new CraftingState(this));
        }

        public void PlayAnimation(string animName)
        {
            // Kill any active tweens on the transform and sprite renderer to avoid conflicts
            transform.DOKill();
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.DOKill();

            if (_animator != null && _animator.runtimeAnimatorController != null)
            {
                _animator.CrossFade(animName, 0.2f);
                Debug.Log($"[DiscipleAI] Đang phát animation qua Animator: {animName}");
                return;
            }

            Debug.Log($"[DiscipleAI] Đang phát Tween-Animation cho Sprite tĩnh: {animName}");

            // Reset default visual properties
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            if (sr != null) sr.color = Color.white;

            switch (animName.ToLower())
            {
                case "idle":
                    // Squash & Stretch nhịp thở nhẹ nhàng
                    transform.DOScaleY(1.05f, 1f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
                    transform.DOScaleX(0.96f, 1f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
                    break;

                case "run":
                    // Chạy nghiêng người nhấp nhô liên tục
                    transform.DOLocalRotate(new Vector3(0, 0, 8f), 0.2f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutQuad);
                    transform.DOScaleY(0.92f, 0.2f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutQuad);
                    break;

                case "fly":
                    // Trạng thái bay lơ lửng hình sóng Sine
                    transform.DOLocalMoveY(transform.localPosition.y + 0.3f, 0.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
                    transform.DOLocalRotate(new Vector3(0, 0, 4f), 0.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
                    break;

                case "meditate":
                    // Thiền: Thu nhỏ nhẹ như đang tập trung tinh thần, nhấp nhô chậm
                    transform.DOScale(0.9f, 2f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutQuad);
                    if (sr != null) sr.DOFade(0.7f, 2f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutQuad);
                    break;

                case "work":
                    // Làm việc: Gập người lên xuống giả lập cuốc/trồng trọt
                    transform.DOLocalRotate(new Vector3(0, 0, 25f), 0.4f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutQuad);
                    break;

                case "crafting":
                    // Luyện đan/Khí: Rung rinh nhẹ liên tục
                    transform.DOShakePosition(10f, strength: new Vector3(0.04f, 0.04f, 0f), vibrato: 5, randomness: 90).SetLoops(-1);
                    break;

                case "combatidle":
                    // Đứng thủ thế: Nhún người nhanh hơn Idle thường
                    transform.DOScaleY(1.08f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
                    transform.DOScaleX(0.92f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
                    break;

                case "attack":
                case "skill1":
                case "skill2":
                    // Tấn công: Lao lên phía trước nhanh, nghiêng chém rồi giật về
                    Vector3 moveDir = transform.localScale.x > 0 ? Vector3.right : Vector3.left;
                    transform.DOPunchRotation(moveDir * 1.2f, 0.4f, vibrato: 2, elasticity: 0.5f);
                    transform.DOPunchRotation(new Vector3(0, 0, -20f), 0.4f);
                    break;

                case "hit":
                    // Bị đánh: Giật đỏ và rung lắc
                    transform.DOShakePosition(0.3f, strength: new Vector3(0.4f, 0, 0), vibrato: 15);
                    if (sr != null)
                    {
                        sr.DOColor(Color.red, 0.1f).OnComplete(() => sr.DOColor(Color.white, 0.15f));
                    }
                    break;

                case "die":
                    // Tử vong: Xoay nằm ngang và mờ dần (Fade out)
                    transform.DOLocalRotate(new Vector3(0, 0, 90f), 0.4f).SetEase(Ease.OutBounce);
                    transform.DOLocalMoveY(transform.localPosition.y - 0.5f, 0.4f);
                    if (sr != null) sr.DOFade(0f, 1f).SetDelay(0.5f);
                    break;

                case "revive":
                    // Hồi sinh: Từ nằm dọc quay lại bình thường, nhấp nháy sáng
                    transform.DOLocalRotate(Vector3.zero, 0.5f);
                    if (sr != null)
                    {
                        sr.color = new Color(0, 1, 0, 0); // Start transparent green
                        sr.DOFade(1f, 0.5f).OnComplete(() => sr.DOColor(Color.white, 0.3f));
                    }
                    break;
            }
        }
    }


    public class IdleState : BaseState
    {
        private float _idleTimer;

        public IdleState(DiscipleAI ai) : base(ai) { }

        public override void Enter()
        {
            _ai.PlayAnimation("Idle");
            _idleTimer = Random.Range(2f, 5f); // Đứng chơi 2-5 giây rồi đi làm việc
        }

        public override void Update()
        {
            _idleTimer -= Time.deltaTime;
            if (_idleTimer <= 0)
            {
                if (Random.value > 0.5f)
                    _ai.ChangeState(new MeditateState(_ai));
                else
                    _ai.ChangeState(new WorkState(_ai));
            }
        }
    }

    public class MoveState : BaseState
    {
        public MoveState(DiscipleAI ai) : base(ai) { }

        public override void Enter()
        {
            float distance = Vector2.Distance(_ai.transform.position, _ai.TargetPosition);
            float time = distance / _ai.MoveSpeed;

            if (distance > 15f)
            {
                _ai.PlayAnimation("Fly");
            }
            else
            {
                _ai.PlayAnimation("Run");
            }

            _ai.transform.DOMove(_ai.TargetPosition, time).SetEase(Ease.Linear).OnComplete(() =>
            {
                _ai.ChangeState(new IdleState(_ai));
            });
        }

        public override void Exit()
        {
            _ai.transform.DOKill();
        }
    }

    public class MeditateState : BaseState
    {
        private float _timer;
        public MeditateState(DiscipleAI ai) : base(ai) { }

        public override void Enter()
        {
            _ai.PlayAnimation("Meditate");
            _timer = 5f; // Thiền 5 giây
            Debug.Log("[DiscipleAI] Đang ngồi thiền hấp thu linh khí...");
        }

        public override void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0)
            {
                _ai.ChangeState(new IdleState(_ai));
            }
        }
    }

    public class WorkState : BaseState
    {
        private float _timer;
        public WorkState(DiscipleAI ai) : base(ai) { }

        public override void Enter()
        {
            _ai.PlayAnimation("Work");
            _timer = 5f; // Làm việc 5 giây
            Debug.Log("[DiscipleAI] Đang trồng linh thảo...");
        }

        public override void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0)
            {
                _ai.ChangeState(new IdleState(_ai));
            }
        }
    }

    public class CraftingState : BaseState
    {
        private float _timer;
        public CraftingState(DiscipleAI ai) : base(ai) { }

        public override void Enter()
        {
            _ai.PlayAnimation("Crafting");
            _timer = 10f;
            Debug.Log("[DiscipleAI] Đang luyện khí / luyện đan...");
        }

        public override void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0)
            {
                _ai.ChangeState(new IdleState(_ai));
            }
        }
    }

    public class CombatState : BaseState
    {
        public CombatState(DiscipleAI ai) : base(ai) { }

        public override void Enter()
        {
            _ai.PlayAnimation("CombatIdle");
            Debug.Log("[DiscipleAI] Tiến vào trạng thái chiến đấu!");
        }
    }

    public class AttackState : BaseState
    {
        private int _skillIndex;
        private float _duration;

        public AttackState(DiscipleAI ai, int skillIndex = 0, float duration = 1.0f) : base(ai)
        {
            _skillIndex = skillIndex;
            _duration = duration;
        }

        public override void Enter()
        {
            if (_skillIndex == 0)
            {
                _ai.PlayAnimation("Attack");
                Debug.Log("[DiscipleAI] Đánh thường");
            }
            else
            {
                _ai.PlayAnimation($"Skill{_skillIndex}");
                Debug.Log($"[DiscipleAI] Tung tuyệt chiêu {_skillIndex}");
            }
        }

        public override void Update()
        {
            _duration -= Time.deltaTime;
            if (_duration <= 0)
            {
                _ai.ChangeState(new CombatState(_ai));
            }
        }
    }

    public class HitState : BaseState
    {
        private float _duration;
        public HitState(DiscipleAI ai, float duration = 0.5f) : base(ai)
        {
            _duration = duration;
        }

        public override void Enter()
        {
            _ai.PlayAnimation("Hit");
            Debug.Log("[DiscipleAI] Bị trúng đòn!");
        }

        public override void Update()
        {
            _duration -= Time.deltaTime;
            if (_duration <= 0)
            {
                _ai.ChangeState(new CombatState(_ai));
            }
        }
    }

    public class DeadState : BaseState
    {
        public DeadState(DiscipleAI ai) : base(ai) { }

        public override void Enter()
        {
            _ai.PlayAnimation("Die");
            Debug.Log("[DiscipleAI] Đã tử vong...");
        }
    }

    public class ReviveState : BaseState
    {
        private float _duration;
        public ReviveState(DiscipleAI ai, float duration = 2.0f) : base(ai)
        {
            _duration = duration;
        }

        public override void Enter()
        {
            _ai.PlayAnimation("Revive");
            Debug.Log("[DiscipleAI] Được hồi sinh!");
        }

        public override void Update()
        {
            _duration -= Time.deltaTime;
            if (_duration <= 0)
            {
                _ai.ChangeState(new IdleState(_ai));
            }
        }
    }

    public class BetrayedState : BaseState
    {
        private float _leaveTimer;
        private float _destroyTimer;
        
        public BetrayedState(DiscipleAI ai) : base(ai) { }

        public override void Enter()
        {
            _ai.PlayAnimation("Idle"); // hoặc "AngryIdle"
            _leaveTimer = 180f; // 3 phút đời thực để thu phục
            _destroyTimer = Random.Range(5f, 15f);
            Debug.Log("[DiscipleAI] Đệ tử đã phản bội! Cần thu phục trong thời gian giới hạn nếu không sẽ rời đi.");
        }

        public override void Update()
        {
            _leaveTimer -= Time.deltaTime;
            if (_leaveTimer <= 0)
            {
                Debug.Log("[DiscipleAI] Đệ tử phản bội đã chính thức rời khỏi tông môn.");
                Object.Destroy(_ai.gameObject);
                return;
            }

            _destroyTimer -= Time.deltaTime;
            if (_destroyTimer <= 0)
            {
                Debug.Log("[DiscipleAI] Đệ tử phản bội đang đập phá tông môn!");
                _ai.PlayAnimation("Attack");
                _destroyTimer = Random.Range(10f, 20f);
            }
        }
    }
}
