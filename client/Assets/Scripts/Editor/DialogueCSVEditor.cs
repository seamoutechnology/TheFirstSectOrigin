using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GameClient.Dialogue;

namespace GameClient.EditorTools
{
    public class DialogueCSVEditor : EditorWindow
    {
        private const string JSON_PATH = "Assets/Resources/Data/DialogueDatabase.json";
        
        [MenuItem("Tools/Dialogue/Import-Export CSV (Excel)")]
        public static void ShowWindow()
        {
            GetWindow<DialogueCSVEditor>("Dialogue Data Tool");
        }

        private void OnGUI()
        {
            GUILayout.Label("CÔNG CỤ QUẢN LÝ HỘI THOẠI (CSV / EXCEL)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            GUILayout.Label("File CSV sẽ có cấu trúc: SequenceID, TextKey, SpeakerKey, AvatarID, Position", EditorStyles.helpBox);
            
            EditorGUILayout.Space();

            if (GUILayout.Button("Xuất Data ra file CSV (Export)", GUILayout.Height(40)))
            {
                ExportToCSV();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Nhập Data từ file CSV (Import)", GUILayout.Height(40)))
            {
                ImportFromCSV();
            }
        }

        private void ExportToCSV()
        {
            DialogueDatabase db = LoadDatabaseFromJSON();
            if (db == null || db.sequences.Count == 0)
            {
                Debug.LogWarning("[DialogueEditor] Database rỗng. Đang tạo file CSV mẫu.");
                db = new DialogueDatabase();
                var seq = new DialogueSequence { sequenceId = "demo_seq" };
                seq.nodes.Add(new DialogueNode { nodeId = "node_01", textKey = "dia_01", speakerNameKey = "npc_1", avatarId = "avatar_1", position = DialoguePosition.Left });
                db.sequences.Add(seq);
            }

            string filePath = EditorUtility.SaveFilePanel("Export Dialogue CSV", "", "DialogueData.csv", "csv");
            if (string.IsNullOrEmpty(filePath)) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("SequenceID,NodeID,TextKey,SpeakerKey,AvatarID,Position");

            foreach (var seq in db.sequences)
            {
                foreach (var node in seq.nodes)
                {
                    sb.AppendLine($"{seq.sequenceId},{node.nodeId},{node.textKey},{node.speakerNameKey},{node.avatarId},{node.position}");
                }
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[DialogueEditor] Đã xuất thành công ra: {filePath}");
        }

        private void ImportFromCSV()
        {
            string filePath = EditorUtility.OpenFilePanel("Import Dialogue CSV", "", "csv");
            if (string.IsNullOrEmpty(filePath)) return;

            string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
            if (lines.Length <= 1)
            {
                Debug.LogError("[DialogueEditor] File CSV rỗng hoặc chỉ có Header!");
                return;
            }

            DialogueDatabase db = new DialogueDatabase();
            
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                string[] columns = lines[i].Split(',');
                if (columns.Length < 6) continue;

                string sequenceId = columns[0];
                string nodeId = columns[1];
                string textKey = columns[2];
                string speakerKey = columns[3];
                string avatarId = columns[4];
                DialoguePosition position = Enum.TryParse<DialoguePosition>(columns[5], out var pos) ? pos : DialoguePosition.Center;

                DialogueSequence seq = db.sequences.Find(s => s.sequenceId == sequenceId);
                if (seq == null)
                {
                    seq = new DialogueSequence { sequenceId = sequenceId };
                    db.sequences.Add(seq);
                }

                seq.nodes.Add(new DialogueNode
                {
                    nodeId = nodeId,
                    textKey = textKey,
                    speakerNameKey = speakerKey,
                    avatarId = avatarId,
                    position = position
                });
            }

            SaveDatabaseToJSON(db);
            Debug.Log($"[DialogueEditor] Đã nhập thành công {db.sequences.Count} chuỗi hội thoại từ {filePath}");
        }

        private DialogueDatabase LoadDatabaseFromJSON()
        {
            if (File.Exists(JSON_PATH))
            {
                string json = File.ReadAllText(JSON_PATH, Encoding.UTF8);
                return JsonUtility.FromJson<DialogueDatabase>(json);
            }
            return null;
        }

        private void SaveDatabaseToJSON(DialogueDatabase db)
        {
            string json = JsonUtility.ToJson(db, true); // true để format JSON đẹp, dễ đọc
            
            string dir = Path.GetDirectoryName(JSON_PATH);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(JSON_PATH, json, Encoding.UTF8);
            AssetDatabase.Refresh(); // Yêu cầu Unity load lại file
        }
    }
}
