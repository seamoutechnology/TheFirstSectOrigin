using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using DG.Tweening;
using GameClient.Cutscenes.Core;
using GameClient.Managers;
using GameClient.UI;

namespace GameClient.Cutscenes
{
    public class JsonCutscene : BaseCutscene
    {
        private CutsceneGraphData _graphData;
        private Dictionary<string, Transform> _entities = new Dictionary<string, Transform>();
        private Dictionary<string, int> _pendingDependencies = new Dictionary<string, int>();
        private GameObject _cachedBubblePrefab;

        public void Initialize(string jsonContent)
        {
            _graphData = JsonUtility.FromJson<CutsceneGraphData>(jsonContent);
            if (_graphData == null)
            {
                Debug.LogError("[JsonCutscene] Lỗi parse JSON!");
            }
        }

        protected override async void StartExecution()
        {
            if (_graphData == null || _graphData.nodes.Count == 0)
            {
                Stop();
                return;
            }

            foreach (var e in _graphData.entities)
            {
                var existingInScene = GameObject.Find(e.id);
                if (existingInScene != null)
                {
                    existingInScene.SetActive(false);
                }
            }

            try
            {
                _cachedBubblePrefab = await GameClient.Core.ResourceManager.Instance.LoadAssetAsync<GameObject>("UI_SpeechBubble");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonCutscene] Không thể pre-cache UI_SpeechBubble: {ex.Message}");
            }

            foreach (var e in _graphData.entities)
            {
                if (string.IsNullOrEmpty(e.prefabPath))
                {
                    var go = new GameObject(e.id);
                    go.SetActive(false);
                    _entities[e.id] = go.transform;
                }
                else if (e.prefabPath.StartsWith("Primitive:"))
                {
                    string primType = e.prefabPath.Replace("Primitive:", "");
                    GameObject primGo;
                    if (primType == "Sphere") primGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    else if (primType == "Capsule") primGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    else primGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    
                    primGo.SetActive(false);
                    _entities[e.id] = primGo.transform;
                }
                else
                {
                    GameObject instance = null;
                    
                    try 
                    {
                        var prefab = await GameClient.Core.ResourceManager.Instance.LoadAssetAsync<GameObject>(e.prefabPath);
                        if (prefab != null)
                        {
                            bool originalState = prefab.activeSelf;
                            prefab.SetActive(false);
                            instance = Instantiate(prefab, e.startPos, Quaternion.identity);
                            prefab.SetActive(originalState);
                        }
                    }
                    catch (System.Exception) { }

                    if (instance == null)
                    {
                        var prefab = GameClient.Core.ResourceManager.Instance.LoadFromResources<GameObject>(e.prefabPath);
                        if (prefab != null)
                        {
                            bool originalState = prefab.activeSelf;
                            prefab.SetActive(false);
                            instance = Instantiate(prefab, e.startPos, Quaternion.identity);
                            prefab.SetActive(originalState);
                        }
                    }

                    if (instance != null)
                    {
                        _entities[e.id] = instance.transform;
                    }
                    else
                    {
                        Debug.LogError($"[JsonCutscene] Không tìm thấy Prefab tại đường dẫn: {e.prefabPath}. Fallback về GameObject rỗng.");
                        var go = new GameObject(e.id);
                        go.SetActive(false);
                        _entities[e.id] = go.transform;
                    }
                }

                _entities[e.id].name = e.id;
                _entities[e.id].position = e.startPos;
            }

            foreach (var ent in _entities.Values)
            {
                if (ent != null)
                {
                    ent.gameObject.SetActive(true);
                }
            }

            _pendingDependencies.Clear();
            foreach (var node in _graphData.nodes)
            {
                int incomingCount = _graphData.nodeLinks.Count(l => l.targetNodeGuid == node.guid);
                _pendingDependencies[node.guid] = incomingCount;
            }

            var entryNode = _graphData.nodes.FirstOrDefault(n => n.type == CutsceneNodeType.Entry);
            if (entryNode == null)
            {
                Debug.LogError("[JsonCutscene] Không tìm thấy Entry Node!");
                Stop();
                return;
            }

            ExecuteNode(entryNode.guid);
        }

        private async void ExecuteNode(string guid)
        {
            var node = _graphData.nodes.FirstOrDefault(n => n.guid == guid);
            if (node == null) return;

            _activeNodesCount++;

            switch (node.type)
            {
                case CutsceneNodeType.Entry:
                    OnNodeCompleted(guid);
                    break;

                case CutsceneNodeType.MoveTo:
                    if (_entities.TryGetValue(node.targetEntityId, out Transform target))
                    {
                        target.DOMove(node.targetPos, node.duration)
                              .SetEase(ParseEase(node.easeType))
                              .OnComplete(() => OnNodeCompleted(guid));
                    }
                    else
                    {
                        OnNodeCompleted(guid);
                    }
                    break;
                
                case CutsceneNodeType.Wait:
                    DOVirtual.DelayedCall(node.duration, () => OnNodeCompleted(guid));
                    break;

                case CutsceneNodeType.Unparent:
                    if (_entities.TryGetValue(node.targetEntityId, out Transform targetUnparent))
                    {
                        targetUnparent.SetParent(null);
                    }
                    OnNodeCompleted(guid);
                    break;

                case CutsceneNodeType.Dialogue:
                    string localizedText = LocalizationManager.Instance.GetText(node.dialogueTable, node.dialogueKey);
                    Debug.Log($"[Dialogue]: {localizedText}");
                    
                    await ShowSpeechBubbleForDialogue(node, localizedText, guid);
                    break;

                case CutsceneNodeType.OpenUI:
                    GameClient.UIManager.Instance.OpenPanel(node.panelName, null, node.isLoadByPlatform);
                    OnNodeCompleted(guid);
                    break;
                case CutsceneNodeType.CameraMove:
                    if (Camera.main != null)
                    {
                        Sequence camSeq = DOTween.Sequence();
                        
                        if (node.isCameraMoveToEntity && !string.IsNullOrEmpty(node.targetEntityId) && _entities.TryGetValue(node.targetEntityId, out var targetEntity))
                        {
                            Vector3 startCamPos = Camera.main.transform.position;
                            Vector3 offset = node.targetPos;
                            
                            if (offset.z == 0) 
                            {
                                offset.z = -Vector3.Distance(startCamPos, targetEntity.position);
                            }

                            float t = 0;
                            camSeq.Join(DOTween.To(() => t, x => {
                                t = x;
                                Vector3 currentTarget = targetEntity.position 
                                    + Camera.main.transform.right * offset.x 
                                    + Camera.main.transform.up * offset.y 
                                    + Camera.main.transform.forward * offset.z;

                                Camera.main.transform.position = Vector3.LerpUnclamped(startCamPos, currentTarget, t);
                            }, 1f, node.duration).SetEase(ParseEase(node.easeType)));
                            
                            Debug.Log($"[CameraMove] Dynamically tracking entity {node.targetEntityId} with offset {offset}");
                        }
                        else
                        {
                            camSeq.Join(Camera.main.transform.DOMove(node.targetPos, node.duration).SetEase(ParseEase(node.easeType)));
                            Debug.Log($"[CameraMove] Moving to fixed pos {node.targetPos}");
                        }
                        
                        if (node.cameraZoom > 0)
                        {
                            if (Camera.main.orthographic)
                            {
                                camSeq.Join(Camera.main.DOOrthoSize(node.cameraZoom, node.duration).SetEase(ParseEase(node.easeType)));
                            }
                            else
                            {
                                camSeq.Join(Camera.main.DOFieldOfView(node.cameraZoom, node.duration).SetEase(ParseEase(node.easeType)));
                            }
                        }

                        camSeq.OnComplete(() => OnNodeCompleted(guid));
                    }
                    else OnNodeCompleted(guid);
                    break;
                case CutsceneNodeType.CameraShake:
                    if (Camera.main != null)
                    {
                        Camera.main.transform.DOShakePosition(node.duration, node.shakeStrength, node.shakeVibrato)
                            .OnComplete(() => OnNodeCompleted(guid));
                    }
                    else OnNodeCompleted(guid);
                    break;
                case CutsceneNodeType.PlaySound:
                    if (!string.IsNullOrEmpty(node.audioKey))
                    {
                        if (!string.IsNullOrEmpty(node.audioTable))
                        {
                            AudioManager.Instance.PlayLocalizedSFX(node.audioTable, node.audioKey);
                        }
                        else
                        {
                            AudioManager.Instance.PlaySFX(node.audioKey);
                        }
                    }
                    OnNodeCompleted(guid);
                    break;
                case CutsceneNodeType.ParentTo:
                    if (_entities.TryGetValue(node.targetEntityId, out var childObj) &&
                        _entities.TryGetValue(node.parentEntityId, out var parentObj))
                    {
                        childObj.SetParent(parentObj);
                    }
                    OnNodeCompleted(guid);
                    break;
                
                case CutsceneNodeType.PlayAnimation:
                    if (_entities.TryGetValue(node.targetEntityId, out Transform animTarget))
                    {
                        var animator = animTarget.GetComponentInChildren<Animator>();
                        if (animator != null && !string.IsNullOrEmpty(node.animationName))
                        {
                            animator.Play(node.animationName);
                        }
                    }
                    OnNodeCompleted(guid);
                    break;
                    
                case CutsceneNodeType.FindBuilding:
                    var building = GameClient.Gameplay.BaseBuilder.BaseBuildingManager.Instance.GetFirstBuilding(node.targetEntityId);
                    if (building != null)
                    {
                        _entities[node.targetEntityId] = building.transform;
                        Debug.Log($"[Cutscene] Đã tìm thấy building {node.targetEntityId}");
                        
                        if (GameClient.BaseBuilding.Core.CameraController.Instance != null)
                        {
                            GameClient.BaseBuilding.Core.CameraController.Instance.FocusTo(building.transform.position, 1f);
                        }
                        
                        await System.Threading.Tasks.Task.Delay(1000);
                    }
                    else
                    {
                        Debug.LogWarning($"[Cutscene] Không tìm thấy building {node.targetEntityId} trong map!");
                    }
                    OnNodeCompleted(guid);
                    break;
                    
                case CutsceneNodeType.DestroyEntity:
                    if (!string.IsNullOrEmpty(node.targetEntityId) && _entities.TryGetValue(node.targetEntityId, out Transform entTransform))
                    {
                        if (entTransform != null && entTransform.gameObject != null)
                        {
                            Destroy(entTransform.gameObject);
                        }
                        _entities.Remove(node.targetEntityId);
                        Debug.Log($"[Cutscene] Đã hủy thực thể {node.targetEntityId}");
                    }
                    OnNodeCompleted(guid);
                    break;
            }
        }

        private void OnNodeCompleted(string guid)
        {
            var outgoingLinks = _graphData.nodeLinks.Where(l => l.baseNodeGuid == guid).ToList();

            foreach (var link in outgoingLinks)
            {
                if (_pendingDependencies.ContainsKey(link.targetNodeGuid))
                {
                    _pendingDependencies[link.targetNodeGuid]--;
                    if (_pendingDependencies[link.targetNodeGuid] <= 0)
                    {
                        ExecuteNode(link.targetNodeGuid);
                    }
                }
            }

            CompleteNode();
        }

        private async Task ShowSpeechBubbleForDialogue(CutsceneNodeData node, string localizedText, string guid)
        {
            Transform speakingEntity = null;
            if (!string.IsNullOrEmpty(node.targetEntityId))
            {
                _entities.TryGetValue(node.targetEntityId, out speakingEntity);
            }

            GameObject bubblePrefab = _cachedBubblePrefab;
            if (bubblePrefab == null)
            {
                try
                {
                    bubblePrefab = await GameClient.Core.ResourceManager.Instance.LoadAssetAsync<GameObject>("UI_SpeechBubble");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[JsonCutscene] Không thể load UI_SpeechBubble: {ex.Message}");
                }
            }

            if (bubblePrefab != null && speakingEntity != null)
            {
                Transform parentCanvas = null;
                Canvas mainCanvas = GameObject.FindFirstObjectByType<Canvas>();
                if (mainCanvas != null)
                {
                    parentCanvas = mainCanvas.transform;
                }

                bool originalActiveState = bubblePrefab.activeSelf;
                bubblePrefab.SetActive(false);
                GameObject bubbleInstance = Instantiate(bubblePrefab, parentCanvas);
                bubblePrefab.SetActive(originalActiveState);

                var speech = bubbleInstance.GetComponent<SpeechBubble>();
                if (speech != null)
                {
                    AudioClip voiceClip = null;
                    if (!string.IsNullOrEmpty(node.audioKey))
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(node.audioTable))
                            {
                                var handle = UnityEngine.Localization.Settings.LocalizationSettings.AssetDatabase.GetLocalizedAssetAsync<AudioClip>(node.audioTable, node.audioKey);
                                voiceClip = await handle.Task;
                            }
                            else
                            {
                                voiceClip = await GameClient.Core.ResourceManager.Instance.LoadAssetAsync<AudioClip>(node.audioKey);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[JsonCutscene] Không thể load voice clip {node.audioKey}: {ex.Message}");
                        }
                    }

                    float duration = node.duration > 0 ? node.duration : -1f;
                    speech.Setup(speakingEntity, localizedText, voiceClip, duration);

                    float waitTime = duration;
                    if (voiceClip != null)
                    {
                        waitTime = voiceClip.length;
                    }
                    else if (waitTime < 0)
                    {
                        waitTime = Mathf.Clamp(localizedText.Length / 15f, 1.8f, 8.0f);
                    }

                    DOVirtual.DelayedCall(waitTime + 0.1f, () => OnNodeCompleted(guid));
                }
                else
                {
                    DOVirtual.DelayedCall(2.5f, () => OnNodeCompleted(guid));
                }
            }
            else
            {
                DOVirtual.DelayedCall(2.5f, () => OnNodeCompleted(guid));
            }
        }

        private Ease ParseEase(string easeName)
        {
            if (Enum.TryParse(easeName, out Ease ease))
                return ease;
            return Ease.Linear;
        }
    }
}
