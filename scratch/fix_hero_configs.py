import os
import shutil

# Paths
game_data_dir = r"c:\Project\TheFirstSectOrigin\client\Assets\GameData\Heroes"
resources_dir = r"c:\Project\TheFirstSectOrigin\client\Assets\Resources\GameData\Heroes"
nested_heroes_dir = r"c:\Project\TheFirstSectOrigin\client\Assets\Resources\GameData\Heroes\Heroes"

# 1. Clean up nested directory if it exists
if os.path.exists(nested_heroes_dir):
    print(f"Removing nested duplicate directory: {nested_heroes_dir}")
    shutil.rmtree(nested_heroes_dir)

# 2. List of obsolete files to delete
obsolete_files = ["Hero_100.asset", "Hero_101.asset", "Hero_100.asset.meta", "Hero_101.asset.meta"]
for d in [game_data_dir, resources_dir]:
    if os.path.exists(d):
        for f in obsolete_files:
            file_path = os.path.join(d, f)
            if os.path.exists(file_path):
                print(f"Deleting obsolete file: {file_path}")
                os.remove(file_path)

# 3. Define the hero configurations
heroes = [
    {
        "code": "DARK_ASSASSIN_01",
        "name": "Hero_DARK_ASSASSIN_01",
        "heroId": 100,
        "heroName": "Hắc Sát Thần",
        "description": "char_03_desc",
        "prefabAddress": "char_03_img",
        "iconAddress": "avt_03_img",
        "rarity": "SSR",
        "element": "dark",
        "role": "assassin",
        "baseHp": 1000,
        "baseAtk": 250,
        "baseDef": 50,
        "baseSpeed": 140,
        "gachaWeight": 40
    },
    {
        "code": "FIRE_GENERAL_01",
        "name": "Hero_FIRE_GENERAL_01",
        "heroId": 101,
        "heroName": "Hỏa Thần Tướng",
        "description": "char_01_desc",
        "prefabAddress": "char_01_img",
        "iconAddress": "avt_01_img",
        "rarity": "SSR",
        "element": "fire",
        "role": "warrior",
        "baseHp": 1800,
        "baseAtk": 220,
        "baseDef": 120,
        "baseSpeed": 125,
        "gachaWeight": 40
    },
    {
        "code": "LIGHT_DEITY_01",
        "name": "Hero_LIGHT_DEITY_01",
        "heroId": 102,
        "heroName": "Thần Ánh Sáng",
        "description": "char_01_des",
        "prefabAddress": "char_01_img",
        "iconAddress": "avt_01_img",
        "rarity": "UR",
        "element": "light",
        "role": "mage",
        "baseHp": 2000,
        "baseAtk": 350,
        "baseDef": 100,
        "baseSpeed": 150,
        "gachaWeight": 10
    },
    {
        "code": "LIGHT_MAGE_01",
        "name": "Hero_LIGHT_MAGE_01",
        "heroId": 103,
        "heroName": "Thánh Pháp Sư",
        "description": "char_02_desc",
        "prefabAddress": "char_02_img",
        "iconAddress": "avt_02_img",
        "rarity": "SR",
        "element": "light",
        "role": "mage",
        "baseHp": 1100,
        "baseAtk": 180,
        "baseDef": 60,
        "baseSpeed": 115,
        "gachaWeight": 150
    },
    {
        "code": "FIRE_WARRIOR_01",
        "name": "Hero_FIRE_WARRIOR_01",
        "heroId": 104,
        "heroName": "Lửa Chiến Binh",
        "description": "char_01_desc",
        "prefabAddress": "char_01_img",
        "iconAddress": "avt_01_img",
        "rarity": "R",
        "element": "fire",
        "role": "warrior",
        "baseHp": 1200,
        "baseAtk": 120,
        "baseDef": 80,
        "baseSpeed": 105,
        "gachaWeight": 500
    },
    {
        "code": "WATER_TANK_01",
        "name": "Hero_WATER_TANK_01",
        "heroId": 105,
        "heroName": "Thủy Hộ Thuẫn",
        "description": "char_02_desc",
        "prefabAddress": "char_02_img",
        "iconAddress": "avt_02_img",
        "rarity": "R",
        "element": "water",
        "role": "tank",
        "baseHp": 2000,
        "baseAtk": 80,
        "baseDef": 150,
        "baseSpeed": 90,
        "gachaWeight": 500
    },
    {
        "code": "WOOD_HEALER_01",
        "name": "Hero_WOOD_HEALER_01",
        "heroId": 106,
        "heroName": "Mộc Trị Liệu",
        "description": "char_03_desc",
        "prefabAddress": "char_03_img",
        "iconAddress": "avt_03_img",
        "rarity": "R",
        "element": "wood",
        "role": "healer",
        "baseHp": 1000,
        "baseAtk": 90,
        "baseDef": 70,
        "baseSpeed": 100,
        "gachaWeight": 500
    }
]

# Write out files
def generate_yaml(h):
    return f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: d7369c1fe66ca2c4eba7f698b0f58694, type: 3}}
  m_Name: {h['name']}
  m_EditorClassIdentifier: Assembly-CSharp::GameClient.Gameplay.Heroes.HeroConfig
  heroId: {h['heroId']}
  heroName: "{h['heroName']}"
  description: {h['description']}
  MaxLifespan: 100
  BaseLoyalty: 80
  prefabAddress: {h['prefabAddress']}
  iconAddress: {h['iconAddress']}
  rarity: {h['rarity']}
  basePower: 100
  code: {h['code']}
  element: {h['element']}
  role: {h['role']}
  baseHp: {h['baseHp']}
  baseAtk: {h['baseAtk']}
  baseDef: {h['baseDef']}
  baseSpeed: {h['baseSpeed']}
  gachaWeight: {h['gachaWeight']}
  isActive: 1
"""

for h in heroes:
    yaml_content = generate_yaml(h)
    
    # Write to game data
    path1 = os.path.join(game_data_dir, f"Hero_{h['code']}.asset")
    os.makedirs(os.path.dirname(path1), exist_ok=True)
    with open(path1, "w", encoding="utf-8") as f:
        f.write(yaml_content)
    print(f"Generated: {path1}")

    # Write to resources
    path2 = os.path.join(resources_dir, f"Hero_{h['code']}.asset")
    os.makedirs(os.path.dirname(path2), exist_ok=True)
    with open(path2, "w", encoding="utf-8") as f:
        f.write(yaml_content)
    print(f"Generated: {path2}")

print("Hero configs generation complete.")
