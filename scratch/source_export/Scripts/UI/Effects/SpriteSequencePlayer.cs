using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using System.Collections.Generic;
using DG.Tweening;

namespace GameClient.UI.Effects
{
    public class SpriteSequencePlayer : MonoBehaviour
    {
        public Image targetImage;
        public float fps = 30f;
        public bool loop = true;
        public bool playOnEnable = true;

        [Header("Sprite Source")]
        public SpriteAtlas spriteAtlas; // Kéo file Sprite Atlas vào đây
        
        [Header("Manual Frames (Optional)")]
        public List<Sprite> sprites = new List<Sprite>();
        
        private int _currentIndex = 0;
        private Tween _playTween;

        public void SetSprites(List<Sprite> spritesList)
        {
            this.sprites = spritesList;
        }

        public void Play()
        {
            if (this.sprites.Count == 0 || targetImage == null) return;
            
            DOTween.Kill(this.gameObject);
            
            _currentIndex = 0;
            targetImage.sprite = this.sprites[0];
            
            float duration = this.sprites.Count / fps;
            
            _playTween = DOVirtual.Float(0, this.sprites.Count - 0.01f, duration, (v) => 
            {
                int index = Mathf.FloorToInt(v);
                if (index != _currentIndex && index >= 0 && index < this.sprites.Count)
                {
                    _currentIndex = index;
                    targetImage.sprite = this.sprites[_currentIndex];
                }
            })
            .SetId(this.gameObject) // Đóng dấu chủ quyền Tween cho con bướm này
            .SetUpdate(true) // Bướm vẫn bay ngay cả khi game tạm dừng (TimeScale = 0)
            .SetEase(Ease.Linear)
            .SetLoops(loop ? -1 : 1, LoopType.Restart)
            .OnComplete(() => 
            {
                if (!loop)
                {
                    gameObject.SetActive(false);
                }
            });
        }
        
        void OnDisable()
        {
            DOTween.Kill(this.gameObject);
        }
    }
}
