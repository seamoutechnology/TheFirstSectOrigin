-- Hệ thống quản lý phiên bản và Addressables
CREATE TABLE IF NOT EXISTS version_configs (
    platform VARCHAR(20) PRIMARY KEY, -- android, ios, pc
    client_version VARCHAR(20) NOT NULL,
    addressable_version VARCHAR(50) NOT NULL,
    catalog_url TEXT NOT NULL,
    force_update BOOLEAN DEFAULT FALSE,
    update_desc TEXT,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO version_configs (platform, client_version, addressable_version, catalog_url, update_desc)
VALUES ('android', '1.0.0', '2026.05.07.01', 'http://cdn.sectorigin.com/android/catalog.json', 'Bản cập nhật đầu tiên')
ON CONFLICT (platform) DO NOTHING;
