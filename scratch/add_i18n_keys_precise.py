import random
import os

keys_data = [
    {"key": "ui_btn_leaderboard", "en": "Leaderboard", "vi": "Xếp Hạng"},
    {"key": "ui_btn_shop", "en": "Shop", "vi": "Cửa Hàng"},
    {"key": "ui_title_leaderboard", "en": "POWER LEADERBOARD", "vi": "BẢNG XẾP HẠNG CHIẾN LỰC"},
    {"key": "ui_col_rank", "en": "Rank", "vi": "Hạng"},
    {"key": "ui_col_name", "en": "Name", "vi": "Tên"},
    {"key": "ui_col_power", "en": "Power", "vi": "Chiến Lực"},
    {"key": "ui_title_shop", "en": "GENERAL SHOP", "vi": "CỬA HÀNG BÁCH HÓA"},
    {"key": "ui_shop_buy", "en": "Buy", "vi": "Mua"},
    {"key": "ui_shop_recruit_ticket", "en": "Recruit Ticket", "vi": "Vé Chiêu Mộ"},
    {"key": "ui_shop_stamina_potion", "en": "Stamina Potion", "vi": "Dược Thể Lực"},
    {"key": "ui_shop_speed_hourglass", "en": "Speed Hourglass", "vi": "Đồng Hồ Cát"},
    {"key": "ui_shop_not_enough_gold", "en": "Not enough Gold!", "vi": "Không đủ Vàng!"},
    {"key": "ui_shop_not_enough_xu", "en": "Not enough Xu!", "vi": "Không đủ Xu!"},
    {"key": "ui_shop_buy_success", "en": "Purchased successfully!", "vi": "Mua thành công!"},
    {"key": "ui_shop_failed", "en": "Failed", "vi": "Thất Bại"},
    {"key": "ui_shop_success", "en": "Success", "vi": "Thành Công"}
]

shared_path = r"c:\Project\TheFirstSectOrigin\client\Assets\Resources\Localization\Table\UI_System Shared Data.asset"
en_path = r"c:\Project\TheFirstSectOrigin\client\Assets\Resources\Localization\Table\UI_System_en-US.asset"
vi_path = r"c:\Project\TheFirstSectOrigin\client\Assets\Resources\Localization\Table\UI_System_vi-VN.asset"

def generate_id():
    return random.randint(10000000000000, 999999999999999)

def main():
    with open(shared_path, "r", encoding="utf-8-sig") as f:
        shared_lines = f.readlines()
    with open(en_path, "r", encoding="utf-8-sig") as f:
        en_lines = f.readlines()
    with open(vi_path, "r", encoding="utf-8-sig") as f:
        vi_lines = f.readlines()

    # Find existing keys in shared
    existing_keys = {}
    for i, line in enumerate(shared_lines):
        if "m_Key:" in line:
            k = line.strip().split("m_Key:")[1].strip()
            for j in range(i - 1, max(0, i - 5), -1):
                if "m_Id:" in shared_lines[j]:
                    id_val = int(shared_lines[j].strip().split("m_Id:")[1].strip())
                    existing_keys[k] = id_val
                    break

    new_shared = []
    new_en = []
    new_vi = []

    for item in keys_data:
        key = item["key"]
        if key in existing_keys:
            print(f"Key '{key}' already exists in localization files.")
            continue
            
        m_id = generate_id()
        existing_keys[key] = m_id
        
        # Append to shared
        new_shared.append(f"  - m_Id: {m_id}\n")
        new_shared.append(f"    m_Key: {key}\n")
        new_shared.append(f"    m_Metadata:\n")
        new_shared.append(f"      m_Items: []\n")
        
        # Append to en-US
        new_en.append(f"  - m_Id: {m_id}\n")
        new_en.append(f"    m_Localized: {item['en']}\n")
        new_en.append(f"    m_Metadata:\n")
        new_en.append(f"      m_Items: []\n")
        
        # Append to vi-VN
        vi_val = item["vi"]
        new_vi.append(f"  - m_Id: {m_id}\n")
        new_vi.append(f"    m_Localized: \"{vi_val}\"\n")
        new_vi.append(f"    m_Metadata:\n")
        new_vi.append(f"      m_Items: []\n")

    if new_shared:
        # Find where to insert in shared (before the line containing '  m_Metadata:')
        insert_idx_shared = -1
        for idx, line in enumerate(shared_lines):
            if line.startswith("  m_Metadata:"):
                insert_idx_shared = idx
                break
        if insert_idx_shared == -1:
            # Fallback
            insert_idx_shared = len(shared_lines) - 5
            
        # Find where to insert in en-US and vi-VN (before the line containing '  references:')
        insert_idx_en = -1
        for idx, line in enumerate(en_lines):
            if line.startswith("  references:"):
                insert_idx_en = idx
                break
        if insert_idx_en == -1:
            insert_idx_en = len(en_lines) - 3

        insert_idx_vi = -1
        for idx, line in enumerate(vi_lines):
            if line.startswith("  references:"):
                insert_idx_vi = idx
                break
        if insert_idx_vi == -1:
            insert_idx_vi = len(vi_lines) - 3

        # Insert them!
        shared_lines[insert_idx_shared:insert_idx_shared] = new_shared
        en_lines[insert_idx_en:insert_idx_en] = new_en
        vi_lines[insert_idx_vi:insert_idx_vi] = new_vi

        with open(shared_path, "w", encoding="utf-8-sig") as f:
            f.writelines(shared_lines)
        print("Updated Shared Data table precisely.")

        with open(en_path, "w", encoding="utf-8-sig") as f:
            f.writelines(en_lines)
        print("Updated en-US table precisely.")

        with open(vi_path, "w", encoding="utf-8-sig") as f:
            f.writelines(vi_lines)
        print("Updated vi-VN table precisely.")
    else:
        print("No new keys to add.")

if __name__ == "__main__":
    main()
