import os

stages_dir = r"c:\Project\TheFirstSectOrigin\client\Assets\Resources\GameData\Stages"

def fix_prefabs():
    count = 0
    for root, dirs, files in os.walk(stages_dir):
        for file in files:
            if file.endswith(".asset"):
                filepath = os.path.join(root, file)
                with open(filepath, "r", encoding="utf-8") as f:
                    content = f.read()
                
                updated = False
                # Replace MonsterPrefab and SlimePrefab with char_mon_01
                if "prefabAddress: MonsterPrefab" in content:
                    content = content.replace("prefabAddress: MonsterPrefab", "prefabAddress: char_mon_01")
                    updated = True
                if "prefabAddress: SlimePrefab" in content:
                    content = content.replace("prefabAddress: SlimePrefab", "prefabAddress: char_mon_01")
                    updated = True
                
                if updated:
                    with open(filepath, "w", encoding="utf-8", newline="\n") as f:
                        f.write(content)
                    print(f"Updated: {filepath}")
                    count += 1
    print(f"Finished updating {count} stage files.")

if __name__ == "__main__":
    fix_prefabs()
