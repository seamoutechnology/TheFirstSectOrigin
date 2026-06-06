using System.Collections;
using UnityEngine;
using GameClient.UI.Effects;

namespace GameClient.Battle
{
    public abstract class BattleCommand
    {
        public abstract IEnumerator Execute();
    }

    public class SkillCommand : BattleCommand
    {
        public string attackerId;
        public string skillId;
        public bool isCinematic;

        public override IEnumerator Execute()
        {
            Debug.Log($"[Battle] {attackerId} tung chiêu {skillId}");
            
            if (isCinematic)
            {
                yield return BattleSequenceManager.Instance.PlayCinematic(skillId);
            }
            else
            {
                yield return new WaitForSeconds(1.0f); 
            }
        }
    }

    public class DamageCommand : BattleCommand
    {
        public string targetId;
        public int damage;
        public bool isCrit;

        public override IEnumerator Execute()
        {
            GameObject targetGo = BattleSequenceManager.Instance.GetUnitGameObject(targetId);
            if (targetGo != null)
            {
                FloatingTextManager.Instance.SpawnDamage(targetGo.transform.position, damage, isCrit);
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    public class ComboCommand : BattleCommand
    {
        public string heroAId;
        public string heroBId;
        public string animA;
        public string animB;

        public override IEnumerator Execute()
        {
            Debug.Log($"[Combo] {heroAId} và {heroBId} bắt đầu phối hợp!");

            bool signalReceived = false;
            

            
            while (!signalReceived)
            {
                yield return null;
            }

            Debug.Log("[Combo] Tướng B xuất kích dựa trên tín hiệu từ Tướng A!");
            
            yield return new WaitForSeconds(1.0f);
            Debug.Log("[Combo] Kết thúc phối hợp!");
        }
    }

    public class CameraFocusCommand : BattleCommand
    {
        public string targetUnitId;
        public float zoomSize = 3f;
        public float duration = 0.5f;
        public bool isReset = false;

        public override IEnumerator Execute()
        {
            if (isReset)
            {
                CameraManager.Instance.ResetCamera(duration);
            }
            else
            {
                GameObject target = BattleSequenceManager.Instance.GetUnitGameObject(targetUnitId);
                if (target != null)
                {
                    CameraManager.Instance.FocusOn(target.transform.position, zoomSize, duration);
                }
            }
            yield return new WaitForSeconds(duration);
        }
    }

    public class CameraShakeCommand : BattleCommand
    {
        public float duration = 0.2f;
        public float magnitude = 0.1f;

        public override IEnumerator Execute()
        {
            CameraManager.Instance.Shake(duration, magnitude);
            yield return null;
        }
    }

    public class DialogueCommand : BattleCommand
    {
        public string content;

        public override IEnumerator Execute()
        {
            Debug.Log($"[Story] Nhân vật nói: {content}");
            yield return new WaitForSeconds(2.0f);
        }
    }
}
