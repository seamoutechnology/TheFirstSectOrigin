using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using Debug = UnityEngine.Debug;

namespace GameClient.Editor.GMDashboard
{
    public class GMServerTab
    {
        private EditorWindow window;
        private class ServerProcessInfo
        {
            public string Name;
            public int Port;
            public Process Process;
            public bool IsRunning;
            
            public void UpdateStatus()
            {
                if (Process != null && !Process.HasExited) 
                {
                    IsRunning = true;
                    return;
                }
                
                try
                {
                    using (var client = new System.Net.Sockets.TcpClient())
                    {
                        var result = client.BeginConnect("127.0.0.1", Port, null, null);
                        var success = result.AsyncWaitHandle.WaitOne(System.TimeSpan.FromMilliseconds(50));
                        if (success)
                        {
                            client.EndConnect(result);
                            IsRunning = true;
                            return;
                        }
                    }
                }
                catch { }
                IsRunning = false;
            }
        }

        private List<ServerProcessInfo> _servers = new List<ServerProcessInfo>();
        private Queue<string> _logs = new Queue<string>();
        private const int MaxLogs = 100;
        private Vector2 _scrollPosition;
        
        private string _serverRootDir;
        private string _shareRootDir;
        private double _lastCheckTime;

        public GMServerTab(EditorWindow window)
        {
            this.window = window;
        }

        public void OnEnable()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../../"));
            _serverRootDir = Path.Combine(projectRoot, "server");
            _shareRootDir = Path.Combine(projectRoot, "share");

            if (_servers.Count == 0)
            {
                _servers.Add(new ServerProcessInfo { Name = "admin", Port = 8080 });
                _servers.Add(new ServerProcessInfo { Name = "login-server", Port = 50051 });
                _servers.Add(new ServerProcessInfo { Name = "gateway", Port = 50052 });
                _servers.Add(new ServerProcessInfo { Name = "world-server", Port = 50053 });
                _servers.Add(new ServerProcessInfo { Name = "combat-server", Port = 50054 });
            }

            EditorApplication.update += OnEditorUpdate;
        }

        // be called from window's OnDisable if we wanted to automatically close servers,
        // but typically tabs don't automatically close servers unless user does it
        public void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            StopAllServers();
        }

        private void OnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup - _lastCheckTime > 2.0f)
            {
                _lastCheckTime = EditorApplication.timeSinceStartup;
                foreach (var server in _servers)
                {
                    server.UpdateStatus();
                }
                window.Repaint();
            }
        }

        public void OnGUI()
        {
            GUILayout.Label("Microservices Control Panel", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start All Servers", GUILayout.Height(30))) StartAllServers();
            if (GUILayout.Button("Stop All Servers", GUILayout.Height(30))) StopAllServers();
            if (GUILayout.Button("Compile Protobuf", GUILayout.Height(30))) CompileProtobuf();
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();
            
            GUILayout.Label("Server Status", EditorStyles.boldLabel);
            foreach (var server in _servers)
            {
                GUILayout.BeginHorizontal("box");
                
                GUI.color = server.IsRunning ? Color.green : Color.red;
                GUILayout.Label("●", GUILayout.Width(20));
                GUI.color = Color.white;

                GUILayout.Label(server.Name, GUILayout.Width(150));

                GUI.enabled = !server.IsRunning;
                if (GUILayout.Button("Start", GUILayout.Width(80))) StartServer(server);

                GUI.enabled = server.IsRunning;
                if (GUILayout.Button("Stop", GUILayout.Width(80))) StopServer(server);
                GUI.enabled = true;

                GUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            GUILayout.Label("Internal Logs", EditorStyles.boldLabel);
            if (GUILayout.Button("Clear Logs", GUILayout.Width(100)))
            {
                _logs.Clear();
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, "box", GUILayout.ExpandHeight(true));
            GUIStyle logStyle = new GUIStyle(EditorStyles.label);
            logStyle.wordWrap = true;
            logStyle.richText = true;

            foreach (var log in _logs)
            {
                EditorGUILayout.LabelField(log, logStyle);
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void StartServer(ServerProcessInfo server)
        {
            if (server.IsRunning) return;

            string targetCmd = $"run cmd/{server.Name}/main.go";
            AddLog($"<color=yellow>[System] Khởi động {server.Name}...</color>");

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "go",
                    Arguments = targetCmd,
                    WorkingDirectory = _serverRootDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                server.Process = new Process { StartInfo = startInfo };
                
                server.Process.OutputDataReceived += (sender, args) => {
                    if (!string.IsNullOrEmpty(args.Data)) AddLog($"<color=cyan>[{server.Name}]</color> {args.Data}");
                };
                server.Process.ErrorDataReceived += (sender, args) => {
                    if (!string.IsNullOrEmpty(args.Data)) AddLog($"<color=red>[{server.Name} ERROR]</color> {args.Data}");
                };

                server.Process.Start();
                server.Process.BeginOutputReadLine();
                server.Process.BeginErrorReadLine();
            }
            catch (System.Exception ex)
            {
                AddLog($"<color=red>[System] Lỗi khởi động {server.Name}: {ex.Message}</color>");
            }
        }

        private void StopServer(ServerProcessInfo server)
        {
            if (!server.IsRunning) return;

            try
            {
                server.Process.Kill();
                server.Process.Dispose();
                server.Process = null;
                AddLog($"<color=yellow>[System] Đã dừng {server.Name}.</color>");
            }
            catch (System.Exception ex)
            {
                AddLog($"<color=red>[System] Lỗi dừng {server.Name}: {ex.Message}</color>");
            }
        }

        private void StartAllServers()
        {
            foreach (var server in _servers) StartServer(server);
        }

        private void StopAllServers()
        {
            foreach (var server in _servers) StopServer(server);
        }

        private void CompileProtobuf()
        {
            AddLog($"<color=yellow>[System] Đang chạy Compile Protobuf...</color>");
            string scriptPath = Path.Combine(_shareRootDir, "compile_proto.bat");
            
            if (!File.Exists(scriptPath))
            {
                AddLog($"<color=red>[System] Không tìm thấy file {scriptPath}</color>");
                return;
            }

            try
            {
                Process p = new Process();
                p.StartInfo.FileName = scriptPath;
                p.StartInfo.WorkingDirectory = _shareRootDir;
                p.StartInfo.UseShellExecute = true;
                p.Start();
            }
            catch (System.Exception ex)
            {
                 AddLog($"<color=red>[System] Lỗi khi chạy compile_proto: {ex.Message}</color>");
            }
        }

        private void AddLog(string message)
        {
            EditorApplication.delayCall += () =>
            {
                if (_logs.Count >= MaxLogs) _logs.Dequeue();
                _logs.Enqueue(message);
                window.Repaint();
                _scrollPosition.y = float.MaxValue;
            };
        }
    }
}
