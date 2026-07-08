#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using GameClient.Cutscenes.Core;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

namespace GameClient.Cutscenes.Editor
{
    public class CutsceneNode : Node
    {
        public string GUID;
        public CutsceneNodeType NodeType;
        public bool EntryPoint = false;
        
        public string TargetEntityId;
        public Vector3 TargetPos;
        public float Duration;
        public string EaseType;
        public string DialogueTable = "Story_Dialogue";
        public string DialogueKey;
        public string PanelName;
        public bool IsLoadByPlatform = true;
        public bool IsCameraMoveToEntity;
        public float CameraZoom;
        public float ShakeStrength = 1f;
        public int ShakeVibrato = 10;
        public string AudioTable;
        public string AudioKey;
        public string ParentEntityId;
        public string AnimationName;
        
        public CutsceneNode() { }

        public void AddCustomFields()
        {
            var customDataContainer = new VisualElement();
            customDataContainer.style.paddingLeft = 8;
            customDataContainer.style.paddingRight = 8;
            customDataContainer.style.paddingTop = 8;
            customDataContainer.style.paddingBottom = 8;
            customDataContainer.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 0.8f));

            switch (NodeType)
            {
                case CutsceneNodeType.MoveTo:
                    var entityField = new TextField("Entity ID") { value = TargetEntityId };
                    entityField.RegisterValueChangedCallback(evt => TargetEntityId = evt.newValue);
                    customDataContainer.Add(entityField);

                    var posField = new Vector3Field("Target Pos") { value = TargetPos };
                    posField.RegisterValueChangedCallback(evt => TargetPos = evt.newValue);
                    customDataContainer.Add(posField);

                    var durField = new FloatField("Duration") { value = Duration };
                    durField.RegisterValueChangedCallback(evt => Duration = evt.newValue);
                    customDataContainer.Add(durField);

                    var easeField = new TextField("Ease Type") { value = EaseType };
                    easeField.RegisterValueChangedCallback(evt => EaseType = evt.newValue);
                    customDataContainer.Add(easeField);
                    break;
                case CutsceneNodeType.Dialogue:
                    var speakerField = new TextField("Speaker Entity ID") { value = TargetEntityId };
                    speakerField.RegisterValueChangedCallback(evt => TargetEntityId = evt.newValue);
                    customDataContainer.Add(speakerField);

                    var tableField = new TextField("Table") { value = DialogueTable };
                    customDataContainer.Add(tableField);

                    var keyField = new TextField("Key") { value = DialogueKey };
                    customDataContainer.Add(keyField);

                    var voiceTableField = new TextField("Voice Table") { value = AudioTable };
                    voiceTableField.RegisterValueChangedCallback(evt => AudioTable = evt.newValue);
                    customDataContainer.Add(voiceTableField);

                    var voiceKeyField = new TextField("Voice Key") { value = AudioKey };
                    voiceKeyField.RegisterValueChangedCallback(evt => AudioKey = evt.newValue);
                    customDataContainer.Add(voiceKeyField);

                    var previewLabel = new Label("Preview: ...") { style = { marginTop = 5, whiteSpace = WhiteSpace.Normal, color = Color.yellow } };
                    customDataContainer.Add(previewLabel);

                    Action updatePreview = () =>
                    {
                        previewLabel.text = "Preview: [Not found in Editor]";
#if UNITY_EDITOR
                        try
                        {
                            var collection = UnityEditor.Localization.LocalizationEditorSettings.GetStringTableCollection(DialogueTable);
                            if (collection != null)
                            {
                                var entry = collection.SharedData.GetEntry(DialogueKey);
                                if (entry != null)
                                {
                                    var locales = UnityEditor.Localization.LocalizationEditorSettings.GetLocales();
                                    if (locales.Count > 0)
                                    {
                                        var table = collection.GetTable(locales[0].Identifier) as UnityEngine.Localization.Tables.StringTable;
                                        if (table != null)
                                        {
                                            var val = table.GetEntry(entry.Id)?.LocalizedValue;
                                            if (!string.IsNullOrEmpty(val)) previewLabel.text = "Preview: " + val;
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
#endif
                    };

                    tableField.RegisterValueChangedCallback(evt => { DialogueTable = evt.newValue; updatePreview(); });
                    keyField.RegisterValueChangedCallback(evt => { DialogueKey = evt.newValue; updatePreview(); });
                    
                    updatePreview();
                    break;
                case CutsceneNodeType.Wait:
                    var waitDurField = new FloatField("Duration") { value = Duration };
                    waitDurField.RegisterValueChangedCallback(evt => Duration = evt.newValue);
                    customDataContainer.Add(waitDurField);
                    break;
                case CutsceneNodeType.OpenUI:
                    var panelField = new TextField("Panel Name") { value = PanelName };
                    panelField.RegisterValueChangedCallback(evt => PanelName = evt.newValue);
                    customDataContainer.Add(panelField);
                    
                    var loadPlatformToggle = new Toggle("Load By Platform") { value = IsLoadByPlatform };
                    loadPlatformToggle.RegisterValueChangedCallback(evt => IsLoadByPlatform = evt.newValue);
                    customDataContainer.Add(loadPlatformToggle);
                    break;
                case CutsceneNodeType.CameraMove:
                    var isToEntityToggle = new Toggle("Move To Entity") { value = IsCameraMoveToEntity };
                    isToEntityToggle.RegisterValueChangedCallback(evt => IsCameraMoveToEntity = evt.newValue);
                    customDataContainer.Add(isToEntityToggle);

                    var entityIdFieldCam = new TextField("Target Entity ID") { value = TargetEntityId };
                    entityIdFieldCam.RegisterValueChangedCallback(evt => TargetEntityId = evt.newValue);
                    customDataContainer.Add(entityIdFieldCam);

                    var camPosField = new Vector3Field("Target Pos") { value = TargetPos };
                    camPosField.RegisterValueChangedCallback(evt => TargetPos = evt.newValue);
                    customDataContainer.Add(camPosField);

                    var zoomField = new FloatField("Camera Zoom") { value = CameraZoom };
                    zoomField.RegisterValueChangedCallback(evt => CameraZoom = evt.newValue);
                    customDataContainer.Add(zoomField);

                    var camDurField = new FloatField("Duration") { value = Duration };
                    camDurField.RegisterValueChangedCallback(evt => Duration = evt.newValue);
                    customDataContainer.Add(camDurField);

                    var camEaseField = new TextField("Ease Type") { value = EaseType };
                    camEaseField.RegisterValueChangedCallback(evt => EaseType = evt.newValue);
                    customDataContainer.Add(camEaseField);
                    break;
                case CutsceneNodeType.CameraShake:
                    var shakeDurField = new FloatField("Duration") { value = Duration };
                    shakeDurField.RegisterValueChangedCallback(evt => Duration = evt.newValue);
                    customDataContainer.Add(shakeDurField);

                    var shakeStrengthField = new FloatField("Strength") { value = ShakeStrength };
                    shakeStrengthField.RegisterValueChangedCallback(evt => ShakeStrength = evt.newValue);
                    customDataContainer.Add(shakeStrengthField);

                    var shakeVibratoField = new IntegerField("Vibrato") { value = ShakeVibrato };
                    shakeVibratoField.RegisterValueChangedCallback(evt => ShakeVibrato = evt.newValue);
                    customDataContainer.Add(shakeVibratoField);
                    break;
                case CutsceneNodeType.PlaySound:
                    var audioTableField = new TextField("Audio Table") { value = AudioTable };
                    audioTableField.RegisterValueChangedCallback(evt => AudioTable = evt.newValue);
                    var audioKeyField = new TextField("Audio Key") { value = AudioKey };
                    audioKeyField.RegisterValueChangedCallback(evt => AudioKey = evt.newValue);
                    customDataContainer.Add(audioTableField);
                    customDataContainer.Add(audioKeyField);
                    break;
                case CutsceneNodeType.ParentTo:
                    var childIdField = new TextField("Child Entity ID") { value = TargetEntityId };
                    childIdField.RegisterValueChangedCallback(evt => TargetEntityId = evt.newValue);
                    customDataContainer.Add(childIdField);

                    var parentIdField = new TextField("Parent Entity ID") { value = ParentEntityId };
                    parentIdField.RegisterValueChangedCallback(evt => ParentEntityId = evt.newValue);
                    customDataContainer.Add(parentIdField);
                    break;
                case CutsceneNodeType.PlayAnimation:
                    var animEntityField = new TextField("Target Entity ID") { value = TargetEntityId };
                    animEntityField.RegisterValueChangedCallback(evt => TargetEntityId = evt.newValue);
                    customDataContainer.Add(animEntityField);

                    var animNameField = new TextField("Animation Name") { value = AnimationName };
                    animNameField.RegisterValueChangedCallback(evt => AnimationName = evt.newValue);
                    customDataContainer.Add(animNameField);
                    break;
                case CutsceneNodeType.FindBuilding:
                    var findBldgField = new TextField("Building ID") { value = TargetEntityId };
                    findBldgField.RegisterValueChangedCallback(evt => TargetEntityId = evt.newValue);
                    customDataContainer.Add(findBldgField);
                    break;
                case CutsceneNodeType.DestroyEntity:
                    var destroyEntityField = new TextField("Target Entity ID") { value = TargetEntityId };
                    destroyEntityField.RegisterValueChangedCallback(evt => TargetEntityId = evt.newValue);
                    customDataContainer.Add(destroyEntityField);
                    break;
            }
            
            extensionContainer.Add(customDataContainer);
            RefreshExpandedState();
        }
    }
}
#endif
