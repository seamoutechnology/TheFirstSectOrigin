using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using GameClient.Core;

namespace GameClient.Managers
{
    public enum InputBindingType
    {
        Tap,
        Hold,
        Combo,
        Sequence
    }

    public class InputBindingData
    {
        public string Id;
        public InputBindingType Type;
        public Key PrimaryKey;
        public Key ModifierKey;
        public Key[] SequenceKeys;
        
        public float HoldDuration;
        public float CurrentHoldTime;
        public Action<float> OnHoldProgress;
        
        public float SequenceTimeout;
        public int CurrentSequenceIndex;
        public float LastSequenceTapTime;

        public Action OnPerformed;
    }

    public class InputManager : Singleton<InputManager>
    {
        private Dictionary<string, InputBindingData> _bindings = new Dictionary<string, InputBindingData>();
        
        
        #region Registration API

        public void RegisterTap(string id, Key key, Action onPerformed)
        {
            _bindings[id] = new InputBindingData
            {
                Id = id,
                Type = InputBindingType.Tap,
                PrimaryKey = key,
                OnPerformed = onPerformed
            };
        }

        public void RegisterCombo(string id, Key modifier, Key primary, Action onPerformed)
        {
            _bindings[id] = new InputBindingData
            {
                Id = id,
                Type = InputBindingType.Combo,
                ModifierKey = modifier,
                PrimaryKey = primary,
                OnPerformed = onPerformed
            };
        }

        public void RegisterHold(string id, Key key, float holdDuration, Action<float> onProgress, Action onCompleted)
        {
            _bindings[id] = new InputBindingData
            {
                Id = id,
                Type = InputBindingType.Hold,
                PrimaryKey = key,
                HoldDuration = holdDuration,
                CurrentHoldTime = 0f,
                OnHoldProgress = onProgress,
                OnPerformed = onCompleted
            };
        }

        public void RegisterSequence(string id, Key[] sequence, float timeout, Action onPerformed)
        {
            _bindings[id] = new InputBindingData
            {
                Id = id,
                Type = InputBindingType.Sequence,
                SequenceKeys = sequence,
                SequenceTimeout = timeout,
                CurrentSequenceIndex = 0,
                LastSequenceTapTime = 0f,
                OnPerformed = onPerformed
            };
        }

        public void Unregister(string id)
        {
            if (_bindings.ContainsKey(id))
            {
                _bindings.Remove(id);
            }
        }

        #endregion

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            var activeBindings = new List<InputBindingData>(_bindings.Values);

            foreach (var binding in activeBindings)
            {
                if (!_bindings.ContainsKey(binding.Id)) continue;

                switch (binding.Type)
                {
                    case InputBindingType.Tap:
                        if (keyboard[binding.PrimaryKey].wasPressedThisFrame)
                        {
                            binding.OnPerformed?.Invoke();
                        }
                        break;

                    case InputBindingType.Combo:
                        if (keyboard[binding.ModifierKey].isPressed && keyboard[binding.PrimaryKey].wasPressedThisFrame)
                        {
                            binding.OnPerformed?.Invoke();
                        }
                        break;

                    case InputBindingType.Hold:
                        if (keyboard[binding.PrimaryKey].isPressed)
                        {
                            binding.CurrentHoldTime += Time.deltaTime;
                            float progress = Mathf.Clamp01(binding.CurrentHoldTime / binding.HoldDuration);
                            binding.OnHoldProgress?.Invoke(progress);

                            if (binding.CurrentHoldTime >= binding.HoldDuration)
                            {
                                binding.CurrentHoldTime = 0f;
                                binding.OnHoldProgress?.Invoke(0f); // Reset thanh cast
                                binding.OnPerformed?.Invoke();
                            }
                        }
                        else
                        {
                            if (binding.CurrentHoldTime > 0)
                            {
                                binding.CurrentHoldTime = 0f;
                                binding.OnHoldProgress?.Invoke(0f);
                            }
                        }
                        break;

                    case InputBindingType.Sequence:
                        HandleSequence(binding, keyboard);
                        break;
                }
            }
        }

        private void HandleSequence(InputBindingData binding, Keyboard keyboard)
        {
            if (binding.SequenceKeys == null || binding.SequenceKeys.Length == 0) return;

            if (binding.CurrentSequenceIndex > 0 && Time.time - binding.LastSequenceTapTime > binding.SequenceTimeout)
            {
                binding.CurrentSequenceIndex = 0;
            }

            Key expectedKey = binding.SequenceKeys[binding.CurrentSequenceIndex];
            
            bool anyKeyPressed = keyboard.anyKey.wasPressedThisFrame;
            
            if (anyKeyPressed)
            {
                if (keyboard[expectedKey].wasPressedThisFrame)
                {
                    binding.CurrentSequenceIndex++;
                    binding.LastSequenceTapTime = Time.time;

                    if (binding.CurrentSequenceIndex >= binding.SequenceKeys.Length)
                    {
                        binding.CurrentSequenceIndex = 0;
                        binding.OnPerformed?.Invoke();
                    }
                }
                else
                {
                    binding.CurrentSequenceIndex = 0;
                }
            }
        }



        #region Cross-platform Pointer / Touch / Mouse API

        public bool IsPointerPanDown()
        {
            var touch = UnityEngine.InputSystem.Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
                return true;
            
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null)
                return mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame;
                
            return false;
        }

        public bool IsPointerPanPressed()
        {
            var touch = UnityEngine.InputSystem.Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed)
            {
                // Đếm số ngón tay đang chạm thực sự (không phải số slot)
                int activeTouchCount = 0;
                foreach (var t in touch.touches)
                {
                    if (t.press.isPressed) activeTouchCount++;
                }
                // Chỉ pan khi 1 ngón tay (2 ngón = pinch zoom)
                return activeTouchCount == 1;
            }

            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null)
                return mouse.rightButton.isPressed || mouse.middleButton.isPressed;
                
            return false;
        }

        public bool IsPrimaryPointerDown()
        {
            try
            {
                var touch = UnityEngine.InputSystem.Touchscreen.current;
                if (touch != null && touch.touches.Count > 0)
                {
                    if (touch.primaryTouch.press.wasPressedThisFrame) return true;
                }

                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse != null)
                {
                    if (mouse.leftButton.wasPressedThisFrame) return true;
                }
            }
            catch (System.Exception) { }

            try
            {
                if (Input.touchCount > 0)
                {
                    if (Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began) return true;
                }
                if (Input.GetMouseButtonDown(0)) return true;
            }
            catch (System.Exception) { }
                
            return false;
        }

        public bool IsPrimaryPointerPressed()
        {
            try
            {
                var touch = UnityEngine.InputSystem.Touchscreen.current;
                if (touch != null && touch.touches.Count > 0)
                {
                    if (touch.primaryTouch.press.isPressed) return true;
                }

                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse != null)
                {
                    if (mouse.leftButton.isPressed) return true;
                }
            }
            catch (System.Exception) { }

            try
            {
                if (Input.touchCount > 0)
                {
                    var phase = Input.GetTouch(0).phase;
                    if (phase == UnityEngine.TouchPhase.Began || 
                        phase == UnityEngine.TouchPhase.Moved || 
                        phase == UnityEngine.TouchPhase.Stationary) 
                        return true;
                }
                if (Input.GetMouseButton(0)) return true;
            }
            catch (System.Exception) { }
                
            return false;
        }

        public bool IsPrimaryPointerReleased()
        {
            try
            {
                var touch = UnityEngine.InputSystem.Touchscreen.current;
                if (touch != null && touch.touches.Count > 0)
                {
                    if (touch.primaryTouch.press.wasReleasedThisFrame) return true;
                }

                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse != null)
                {
                    if (mouse.leftButton.wasReleasedThisFrame) return true;
                }
            }
            catch (System.Exception) { }

            try
            {
                if (Input.touchCount > 0)
                {
                    var phase = Input.GetTouch(0).phase;
                    if (phase == UnityEngine.TouchPhase.Ended || 
                        phase == UnityEngine.TouchPhase.Canceled) 
                        return true;
                }
                if (Input.GetMouseButtonUp(0)) return true;
            }
            catch (System.Exception) { }
                
            return false;
        }

        public Vector2 GetPointerPosition()
        {
            try
            {
                var touch = UnityEngine.InputSystem.Touchscreen.current;
                if (touch != null && touch.touches.Count > 0)
                {
                    Vector2 pos = touch.primaryTouch.position.ReadValue();
                    if (pos != Vector2.zero) return pos;
                }

                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse != null)
                {
                    Vector2 pos = mouse.position.ReadValue();
                    if (pos != Vector2.zero) return pos;
                }
            }
            catch (System.Exception) { }
                
            try
            {
                if (Input.touchCount > 0)
                {
                    return Input.GetTouch(0).position;
                }
                return Input.mousePosition;
            }
            catch (System.Exception) { }

            return Vector2.zero;
        }

        public float GetZoomDelta()
        {
            var touch = UnityEngine.InputSystem.Touchscreen.current;
            if (touch != null && touch.touches.Count == 2)
            {
                var t0 = touch.touches[0];
                var t1 = touch.touches[1];
                Vector2 t0PrevPos = t0.position.ReadValue() - t0.delta.ReadValue();
                Vector2 t1PrevPos = t1.position.ReadValue() - t1.delta.ReadValue();
                float prevMagnitude = (t0PrevPos - t1PrevPos).magnitude;
                float currentMagnitude = (t0.position.ReadValue() - t1.position.ReadValue()).magnitude;
                return (currentMagnitude - prevMagnitude) * 0.05f; // Tốc độ pinch zoom
            }
            
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null)
                return mouse.scroll.ReadValue().y * 0.01f; // Tốc độ scroll chuột
                
            return 0f;
        }

        #endregion
    }
}
