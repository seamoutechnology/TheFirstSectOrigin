import os
import re

def main():
    stages_dir = r"c:\Project\TheFirstSectOrigin\client\Assets\Resources\GameData\Stages"
    files = sorted([f for f in os.listdir(stages_dir) if f.endswith(".asset")])
    
    for filename in files:
        filepath = os.path.join(stages_dir, filename)
        with open(filepath, "r", encoding="utf-8") as f:
            content = f.read()
            
        stage_id_match = re.search(r"stageId:\s*(Stage_\d+|Stage_Fallback)", content)
        if not stage_id_match:
            continue
            
        stage_id = stage_id_match.group(1)
        
        # Simple parser to find rewards
        rewards = []
        rewards_match = re.search(r"rewards:(.*?)(?:\n\w|\Z)", content, re.DOTALL)
        if rewards_match:
            rewards_section = rewards_match.group(1)
            item_ids = re.findall(r"itemId:\s*(\w+)", rewards_section)
            amounts = re.findall(r"amount:\s*(\d+)", rewards_section)
            for item_id, amount in zip(item_ids, amounts):
                rewards.append({"itemId": item_id, "amount": int(amount)})
                
        # Also find staminaCost
        stamina_cost = 5
        stamina_match = re.search(r"staminaCost:\s*(\d+)", content)
        if stamina_match:
            stamina_cost = int(stamina_match.group(1))
            
        print(f'"{stage_id}": {{"staminaCost": {stamina_cost}, "rewards": {rewards}}},')

if __name__ == "__main__":
    main()
