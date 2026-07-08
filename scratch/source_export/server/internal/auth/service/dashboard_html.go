package service

const DashboardHTML = `<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Sect Origin - Server Metrics & AutoScale Control</title>
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;500;600;700;800&display=swap" rel="stylesheet">
    <style>
        :root {
            --bg-color: #f8f9fa;
            --sidebar-bg: #ffffff;
            --card-bg: #ffffff;
            --border-color: #e5e7eb;
            --primary: #6366f1;
            --primary-glow: rgba(99, 102, 241, 0.15);
            --success: #10b981;
            --warning: #f59e0b;
            --danger: #ef4444;
            --text-primary: #111827;
            --text-secondary: #4b5563;
            --text-muted: #9ca3af;
            --shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05), 0 2px 4px -1px rgba(0, 0, 0, 0.03);
            --shadow-md: 0 10px 15px -3px rgba(0, 0, 0, 0.05), 0 4px 6px -2px rgba(0, 0, 0, 0.02);
        }

        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
            font-family: 'Outfit', sans-serif;
        }

        body {
            background-color: var(--bg-color);
            color: var(--text-primary);
            min-height: 100vh;
            padding: 2.5rem;
            display: flex;
            flex-direction: column;
            align-items: center;
        }

        header {
            width: 100%;
            max-width: 1200px;
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 2.5rem;
            border-bottom: 1px solid var(--border-color);
            padding-bottom: 1.5rem;
        }

        h1 {
            font-size: 2rem;
            font-weight: 800;
            color: var(--text-primary);
            letter-spacing: -0.02em;
        }

        .badge-live {
            background: rgba(16, 185, 129, 0.1);
            color: var(--success);
            padding: 0.4rem 1rem;
            border-radius: 9999px;
            font-size: 0.85rem;
            font-weight: 700;
            border: 1px solid rgba(16, 185, 129, 0.2);
            display: flex;
            align-items: center;
            gap: 0.5rem;
            animation: pulse 2s infinite;
        }

        @keyframes pulse {
            0% { box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.4); }
            70% { box-shadow: 0 0 0 10px rgba(16, 185, 129, 0); }
            100% { box-shadow: 0 0 0 0 rgba(16, 185, 129, 0); }
        }

        .container {
            width: 100%;
            max-width: 1200px;
            display: grid;
            grid-template-columns: 2fr 1fr;
            gap: 2rem;
        }

        @media (max-width: 950px) {
            .container {
                grid-template-columns: 1fr;
            }
        }

        .section-title {
            font-size: 1.25rem;
            font-weight: 700;
            margin-bottom: 1.25rem;
            color: var(--text-primary);
            letter-spacing: -0.01em;
        }

        .grid-servers {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(290px, 1fr));
            gap: 1.5rem;
        }

        .server-card {
            background: var(--card-bg);
            border: 1px solid var(--border-color);
            border-radius: 20px;
            padding: 1.5rem;
            box-shadow: var(--shadow);
            transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
            position: relative;
            overflow: hidden;
        }

        .server-card::before {
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            width: 100%;
            height: 4px;
            background: var(--primary);
        }

        .server-card.scaling-trigger::before {
            background: var(--danger);
            animation: warning-flash 1s infinite alternate;
        }

        @keyframes warning-flash {
            from { opacity: 0.4; }
            to { opacity: 1; }
        }

        .server-card:hover {
            transform: translateY(-4px);
            border-color: rgba(99, 102, 241, 0.2);
            box-shadow: var(--shadow-md);
        }

        .server-header {
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            margin-bottom: 1.25rem;
        }

        .server-name {
            font-size: 1.15rem;
            font-weight: 700;
            color: var(--text-primary);
        }

        .server-status {
            font-size: 0.75rem;
            text-transform: uppercase;
            padding: 0.25rem 0.6rem;
            border-radius: 6px;
            font-weight: 700;
        }

        .status-normal {
            background: rgba(16, 185, 129, 0.1);
            color: var(--success);
        }

        .status-crowded {
            background: rgba(245, 158, 11, 0.1);
            color: var(--warning);
        }

        .metric-row {
            margin-bottom: 1rem;
        }

        .metric-label {
            display: flex;
            justify-content: space-between;
            font-size: 0.85rem;
            color: var(--text-secondary);
            margin-bottom: 0.4rem;
            font-weight: 500;
        }

        .progress-bar {
            width: 100%;
            height: 8px;
            background: #f3f4f6;
            border-radius: 999px;
            overflow: hidden;
        }

        .progress-fill {
            height: 100%;
            border-radius: 999px;
            transition: width 0.5s ease;
        }

        .fill-primary {
            background: var(--primary);
        }

        .fill-success {
            background: var(--success);
        }

        .fill-danger {
            background: var(--danger);
        }

        .btn-spike {
            margin-top: 1rem;
            width: 100%;
            background: rgba(239, 68, 68, 0.05);
            border: 1px solid rgba(239, 68, 68, 0.15);
            color: var(--danger);
            padding: 0.65rem;
            border-radius: 12px;
            font-weight: 600;
            font-size: 0.85rem;
            cursor: pointer;
            transition: all 0.2s;
        }

        .btn-spike:hover {
            background: var(--danger);
            color: white;
            border-color: var(--danger);
        }

        .side-panel {
            background: var(--card-bg);
            border: 1px solid var(--border-color);
            border-radius: 20px;
            padding: 1.5rem;
            box-shadow: var(--shadow);
            display: flex;
            flex-direction: column;
            height: fit-content;
        }

        .log-box {
            background: #f9fafb;
            border: 1px solid var(--border-color);
            border-radius: 12px;
            padding: 1rem;
            font-family: monospace;
            font-size: 0.8rem;
            color: var(--text-secondary);
            height: 320px;
            overflow-y: auto;
            display: flex;
            flex-direction: column;
            gap: 0.5rem;
        }

        .log-box div {
            line-height: 1.5;
            border-left: 2px solid var(--primary);
            padding-left: 0.5rem;
        }

        .log-box div.scale {
            border-left-color: var(--success);
            color: var(--success);
            background: rgba(16, 185, 129, 0.05);
            padding: 0.25rem 0.5rem;
            border-radius: 6px;
        }
    </style>
</head>
<body>
    <header>
        <div>
            <h1>Hệ Thống Giám Sát Tài Nguyên Server</h1>
            <p style="color: var(--text-secondary); margin-top: 0.3rem; font-size: 0.95rem;">Tự động co giãn theo cụm Thanh Long & Bạch Hổ</p>
        </div>
        <div class="badge-live">
            <span style="width: 8px; height: 8px; border-radius: 50%; background: var(--success);"></span>
            LIVE MONITORING
        </div>
    </header>

    <div class="container">
        <div>
            <div class="section-title">Cụm Máy Chủ Đang Chạy</div>
            <div class="grid-servers" id="servers-container">
                <!-- Servers render dynamically -->
            </div>
        </div>

        <div class="side-panel">
            <div class="section-title">Lịch Sử Hoạt Động (Logs)</div>
            <div class="log-box" id="logs-container">
                <div>Đang khởi chạy hệ thống giám sát...</div>
                <div>Kết nối CSDL thành công.</div>
            </div>
        </div>
    </div>

    <script>
        let previousServerCount = 0;

        async function fetchStatus() {
            try {
                const res = await fetch('/api/status');
                const servers = await res.json();
                
                renderServers(servers);
                checkScaling(servers);
            } catch (err) {
                console.error("Failed to fetch server metrics:", err);
            }
        }

        function renderServers(servers) {
            const container = document.getElementById('servers-container');
            container.innerHTML = '';

            servers.forEach(server => {
                const cpuColor = server.cpu >= 80 ? 'fill-danger' : (server.cpu >= 60 ? 'fill-primary' : 'fill-success');
                const isNearingScale = server.cpu >= 80;
                
                const card = document.createElement('div');
                card.className = "server-card " + (isNearingScale ? "scaling-trigger" : "");
                
                card.innerHTML = '<div class="server-header">' +
                    '<div>' +
                    '<div class="server-name">' + server.name + '</div>' +
                    '<div style="font-size: 0.75rem; color: var(--text-muted); margin-top: 0.25rem;">' + server.gateway + '</div>' +
                    '</div>' +
                    '<span class="server-status ' + (server.cpu >= 80 ? 'status-crowded' : 'status-normal') + '">' +
                    (server.cpu >= 80 ? 'QUÁ TẢI' : 'ONLINE') +
                    '</span>' +
                    '</div>' +
                    '<div class="metric-row">' +
                    '<div class="metric-label">' +
                    '<span>Tải CPU</span>' +
                    '<span>' + server.cpu.toFixed(1) + '%</span>' +
                    '</div>' +
                    '<div class="progress-bar">' +
                    '<div class="progress-fill ' + cpuColor + '" style="width: ' + server.cpu + '%"></div>' +
                    '</div>' +
                    '</div>' +
                    '<div class="metric-row">' +
                    '<div class="metric-label">' +
                    '<span>Tải RAM</span>' +
                    '<span>' + server.ram.toFixed(1) + '%</span>' +
                    '</div>' +
                    '<div class="progress-bar">' +
                    '<div class="progress-fill fill-primary" style="width: ' + server.ram + '%"></div>' +
                    '</div>' +
                    '</div>' +
                    '<div class="metric-row">' +
                    '<div class="metric-label">' +
                    '<span>Người chơi</span>' +
                    '<span>' + server.players + ' / ' + server.max_players + '</span>' +
                    '</div>' +
                    '<div class="progress-bar">' +
                    '<div class="progress-fill fill-success" style="width: ' + ((server.players / server.max_players) * 100) + '%"></div>' +
                    '</div>' +
                    '</div>' +
                    '<button class="btn-spike" onclick="simulateSpike(' + server.id + ')">Giả Lập Tải Nặng (>=80% CPU)</button>';

                container.appendChild(card);
            });
        }

        function checkScaling(servers) {
            const currentCount = servers.length;
            if (previousServerCount > 0 && currentCount > previousServerCount) {
                const newServer = servers[servers.length - 1];
                addLog('[SCALE EFFECT] Phát hiện tải nặng! Tự động tạo phân vùng máy chủ mới thành công: ' + newServer.name + ' (' + newServer.gateway + ')', 'scale');
            }
            previousServerCount = currentCount;
        }

        async function simulateSpike(zoneId) {
            addLog('Gửi lệnh giả lập tải nặng lên Server ID ' + zoneId + '...');
            try {
                await fetch('/api/spike?zone_id=' + zoneId + '&cpu=92', { method: 'POST' });
                addLog('Đang đẩy tải Server ID ' + zoneId + ' lên 92%...');
            } catch (err) {
                console.error(err);
            }
        }

        function addLog(text, type = '') {
            const container = document.getElementById('logs-container');
            const entry = document.createElement('div');
            entry.className = "log-entry " + type;
            const time = new Date().toLocaleTimeString();
            entry.innerText = '[' + time + '] ' + text;
            container.appendChild(entry);
            container.scrollTop = container.scrollHeight;
        }

        // Poll every 1.5 seconds
        setInterval(fetchStatus, 1500);
        fetchStatus();
    </script>
</body>
</html>`
