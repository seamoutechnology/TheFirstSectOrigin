// UI State Management
function showLogin() {
    document.getElementById('login-modal').style.display = 'flex';
}

// Persistence Logic
document.addEventListener('DOMContentLoaded', () => {
    const userId = localStorage.getItem('user_id');
    const username = localStorage.getItem('username');
    
    if (userId && username) {
        document.getElementById('username-display').innerText = username;
        document.getElementById('landing-page').style.display = 'none';
        document.getElementById('dashboard').style.display = 'flex';
        fetchProfile(userId);
    }
});

// Close modal when clicking outside
window.onclick = function(event) {
    let modal = document.getElementById('login-modal');
    if (event.target == modal) {
        modal.style.display = 'none';
    }
}

async function login() {
    const user = document.getElementById('login-username').value;
    const pass = document.getElementById('login-password').value;
    
    if (!user || !pass) {
        alert('Vui lòng nhập tên đăng nhập và mật khẩu');
        return;
    }

    try {
        const resp = await fetch('/api/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username: user, password: pass })
        });

        const result = await resp.json();
        if (result.code === 0) {
            localStorage.setItem('user_id', result.user_id);
            localStorage.setItem('username', result.username);
            
            document.getElementById('username-display').innerText = result.username;
            document.getElementById('landing-page').style.display = 'none';
            document.getElementById('dashboard').style.display = 'flex';
            document.getElementById('login-modal').style.display = 'none';
            
            // Load profile data
            fetchProfile(result.user_id);
        } else {
            alert(result.message || 'Đăng nhập thất bại');
        }
    } catch (err) {
        console.error(err);
        alert('Lỗi kết nối đến server');
    }
}

async function fetchProfile(userId) {
    try {
        const resp = await fetch(`/api/profile?user_id=${userId}`);
        const result = await resp.json();
        if (result.code === 0) {
            const data = result.data;
            document.getElementById('nickname-display').innerText = data.nickname;
            document.getElementById('gold-display').innerText = data.gold.toLocaleString();
            document.getElementById('diamond-display').innerText = data.diamond.toLocaleString();
            document.getElementById('level-display').innerText = `Lv ${data.level}`;

            const avatarDisplay = document.getElementById('avatar-display');
            if (avatarDisplay) {
                avatarDisplay.src = data.avatar || `https://api.dicebear.com/7.x/adventurer/svg?seed=${encodeURIComponent(data.nickname || 'ChuongMon')}`;
            }

            // Hiển thị các ngày đã điểm danh
            if (data.claimed_days) {
                const dailyGrid = document.querySelector('.daily-grid');
                if (dailyGrid) {
                    const days = dailyGrid.querySelectorAll('.daily-day');
                    // Reset trạng thái
                    days.forEach(el => el.classList.remove('claimed'));
                    
                    // Đánh dấu đã nhận
                    data.claimed_days.forEach(day => {
                        if (days[day-1]) {
                            days[day-1].classList.add('claimed');
                            days[day-1].removeAttribute('onclick'); // Vô hiệu hóa click
                        }
                    });
                }
            }
        }
    } catch (err) {
        console.error('Failed to fetch profile', err);
    }
}

function logout() {
    localStorage.clear();
    document.getElementById('landing-page').style.display = 'block';
    document.getElementById('dashboard').style.display = 'none';
}

function showTab(tabName) {
    // Hide all tabs
    document.querySelectorAll('.tab-content').forEach(tab => {
        tab.style.display = 'none';
    });
    
    // Deactivate all nav items
    document.querySelectorAll('.nav-item').forEach(item => {
        item.classList.remove('active');
    });

    // Show selected tab
    document.getElementById(`tab-${tabName}`).style.display = 'block';
    
    // Activate nav item
    const currentBtn = document.getElementById(`btn-tab-${tabName}`);
    if (currentBtn) {
        currentBtn.classList.add('active');
    }
}

function processRecharge() {
    const type = document.getElementById('card-type').value;
    const amount = document.getElementById('card-amount').value;
    alert(`Đang xử lý nạp thẻ ${type} mệnh giá ${amount} VNĐ...`);
    setTimeout(() => {
        alert('Gửi thẻ thành công! Vui lòng chờ hệ thống kiểm tra.');
    }, 1000);
}

// Change Password Function
async function changePassword() {
    const current = document.getElementById('current-password').value;
    const newPwd = document.getElementById('new-password').value;
    const confirm = document.getElementById('confirm-password').value;
    if (!current || !newPwd || !confirm) {
        alert('Vui lòng nhập toàn bộ thông tin');
        return;
    }
    if (newPwd !== confirm) {
        alert('Mật khẩu mới và xác nhận không khớp');
        return;
    }
    const userId = localStorage.getItem('user_id');
    try {
        const resp = await fetch('/api/user/change-password', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ user_id: parseInt(userId), current_password: current, new_password: newPwd })
        });
        const result = await resp.json();
        if (result.code === 0) {
            alert('Đổi mật khẩu thành công');
        } else {
            alert(result.message || 'Đổi mật khẩu thất bại');
        }
    } catch (err) {
        console.error(err);
        alert('Lỗi kết nối đến server');
    }
}

function claimDaily(day) {
    const el = event.currentTarget;
    if (el.classList.contains('claimed')) return;

    const userId = localStorage.getItem('user_id');
    try {
        const resp = await fetch('/api/claim-daily', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ user_id: parseInt(userId), day: day })
        });

        const result = await resp.json();
        if (result.code === 0) {
            el.classList.add('claimed');
            alert(`Nhận quà thành công! +${result.added_gold} Vàng, +${result.added_diamond} Kim Cương`);
            fetchProfile(userId); // Cập nhật lại số dư hiển thị
        } else {
            alert(result.message || 'Lỗi khi nhận quà');
        }
    } catch (err) {
        console.error(err);
        alert('Lỗi kết nối đến server');
    }
}

// Assistant Logic
function toggleAssistant() {
    const bubble = document.getElementById('assistant-bubble');
    if (bubble) {
        if (bubble.style.display === 'block') {
            bubble.style.display = 'none';
        } else {
            bubble.style.display = 'block';
        }
    }
}

// Disciples Logic
const mockDisciples = [
    { name: 'Lâm Phàm', rarity: 'SSR', level: 50, img: 'https://api.dicebear.com/7.x/avataaars/svg?seed=LamPham' },
    { name: 'Tiêu Viêm', rarity: 'SR', level: 45, img: 'https://api.dicebear.com/7.x/avataaars/svg?seed=TieuViem' },
    { name: 'Hàn Lập', rarity: 'SR', level: 42, img: 'https://api.dicebear.com/7.x/avataaars/svg?seed=HanLap' },
    { name: 'Thạch Hạo', rarity: 'SSR', level: 55, img: 'https://api.dicebear.com/7.x/avataaars/svg?seed=ThachHao' }
];

function renderDisciples() {
    const list = document.getElementById('disciples-list');
    if (!list) return;
    
    list.innerHTML = mockDisciples.map(d => `
        <div class="disciple-card">
            <div class="disciple-header">
                <span class="disciple-name">${d.name}</span>
                <span class="disciple-rarity ${d.rarity}">${d.rarity}</span>
            </div>
            <div class="disciple-stat-row">
                <span>Cấp độ</span>
                <span style="font-weight:600; color:var(--text-primary);">Lv ${d.level}</span>
            </div>
            <button class="btn btn-primary" style="margin-top: 15px; border-radius:10px; padding: 8px 12px; font-size:0.85rem; width:100%;">Truyền Công</button>
        </div>
    `).join('');
}

// Update showTab to render disciples
const originalShowTab = showTab;
showTab = function(tabName) {
    originalShowTab(tabName);
    if (tabName === 'disciples') {
        renderDisciples();
    }
}
