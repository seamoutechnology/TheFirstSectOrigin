import random
import os

# New localization keys to insert
keys_data = [
    {
        "key": "ui_btn_leaderboard",
        "en": "Leaderboard",
        "vi": "Xếp Hạng"
    },
    {
        "key": "ui_btn_shop",
        "en": "Shop",
        "vi": "Cửa Hàng"
    },
    {
        "key": "ui_title_leaderboard",
        "en": "POWER LEADERBOARD",
        "vi": "BẢNG XẾP HẠNG CHIẾN LỰC"
    },
    {
        "key": "ui_col_rank",
        "en": "Rank",
        "vi": "Hạng"
    },
    {
        "key": "ui_col_name",
        "en": "Name",
        "vi": "Tên"
    },
    {
        "key": "ui_col_power",
        "en": "Power",
        "vi": "Chiến Lực"
    },
    {
        "key": "ui_title_shop",
        "en": "GENERAL SHOP",
        "vi": "CỬA HÀNG BÁCH HÓA"
    },
    {
        "key": "ui_shop_buy",
        "en": "Buy",
        "vi": "Mua"
    },
    {
        "key": "ui_shop_recruit_ticket",
        "en": "Recruit Ticket",
        "vi": "Vé Chiêu Mộ"
    },
    {
        "key": "ui_shop_stamina_potion",
        "en": "Stamina Potion",
        "vi": "Dược Thể Lực"
    },
    {
        "key": "ui_shop_speed_hourglass",
        "en": "Speed Hourglass",
        "vi": "Đồng Hồ Cát"
    },
    {
        "key": "ui_shop_not_enough_gold",
        "en": "Not enough Gold!",
        "vi": "Không đủ Vàng!"
    },
    {
        "key": "ui_shop_not_enough_xu",
        "en": "Not enough Xu!",
        "vi": "Không đủ Xu!"
    },
    {
        "key": "ui_shop_buy_success",
        "en": "Purchased successfully!",
        "vi": "Mua thành công!"
    }
]

shared_path = r"c:\Project\TheFirstSectOrigin\client\Assets\Resources\Localization\Table\UI_System Shared Data.asset"
en_path = r"c:\Project\TheFirstSectOrigin\client\Assets\Resources\Localization\Table\UI_System_en-US.asset"
vi_path = r"c:\Project\TheFirstSectOrigin\client\Assets\Resources\Localization\Table\UI_System_vi-VN.asset"

def generate_id():
    # Generate a random 48-bit integer similar to Unity's generated IDs
    return random.randint(10000000000000, 999999999999999)

def main():
    # Read the files
    with open(shared_path, "r", encoding="utf-8-sig") as f:
        shared_lines = f.readlines()
    with open(en_path, "r", encoding="utf-8-sig") as f:
        en_lines = f.readlines()
    with open(vi_path, "r", encoding="utf-8-sig") as f:
        vi_lines = f.readlines()

    # We will generate IDs and append the entries
    # Let's find if key already exists in shared
    existing_keys = {}
    for i, line in enumerate(shared_lines):
        if "m_Key:" in line:
            k = line.strip().split("m_Key:")[1].strip()
            # Find the m_Id just above it
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
        
        # Append to vi-VN (unescape/unicode format if needed, but since it's UTF-8, we can write directly or escape standard unicode characters)
        vi_val = item["vi"]
        # Format the Vietnamese text as a standard YAML string or quoted UTF-8
        new_vi.append(f"  - m_Id: {m_id}\n")
        new_vi.append(f"    m_Localized: \"{vi_val}\"\n")
        new_vi.append(f"    m_Metadata:\n")
        new_vi.append(f"      m_Items: []\n")

    if new_shared:
        # Write to files by appending before the EOF or at the end of the lists.
        # Let's find the end of list in shared_lines
        # The list is under 'm_Entries:'
        # Since it is a list of entries, we can append before the end of file or if there's any closing elements.
        # In Unity tables, it ends directly with the lists. Let's just find the last list item and append.
        
        # Shared Data
        # We find the last line and write
        with open(shared_path, "w", encoding="utf-8-sig") as f:
            f.writelines(shared_lines)
            f.writelines(new_shared)
        print("Updated Shared Data table.")

        # en-US
        with open(en_path, "w", encoding="utf-8-sig") as f:
            f.writelines(en_lines)
            f.writelines(new_en)
        print("Updated en-US table.")

        # vi-VN
        with open(vi_path, "w", encoding="utf-8-sig") as f:
            f.writelines(vi_lines)
            f.writelines(new_vi)
        print("Updated vi-VN table.")
    else:
        print("No new keys to add.")

if __name__ == "__main__":
    main()
