-- Bảng lưu trữ thông báo hệ thống
CREATE TABLE IF NOT EXISTS announcements (
    id SERIAL PRIMARY KEY,
    type VARCHAR(50) NOT NULL, -- MAINTENANCE, RULES, ACTIVITY, NEWS
    title TEXT NOT NULL,
    content TEXT NOT NULL,
    start_at TIMESTAMP NOT NULL,
    end_at TIMESTAMP NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Index để truy vấn nhanh các thông báo đang hoạt động
CREATE INDEX idx_announcements_active ON announcements(is_active, start_at, end_at);
