import json
import subprocess

# Define the correct stage configurations matching the client's actual assets
stage_configs = {
    "Stage_01": {"staminaCost": 5, "rewards": [{"itemId": "00000", "amount": 50}, {"itemId": "00002", "amount": 5}, {"itemId": "00003", "amount": 5}]},
    "Stage_02": {"staminaCost": 5, "rewards": [{"itemId": "00000", "amount": 100}, {"itemId": "00002", "amount": 10}, {"itemId": "00003", "amount": 10}]},
    "Stage_03": {"staminaCost": 5, "rewards": [{"itemId": "00000", "amount": 150}, {"itemId": "00002", "amount": 15}, {"itemId": "00003", "amount": 15}]},
    "Stage_04": {"staminaCost": 5, "rewards": [{"itemId": "00000", "amount": 200}, {"itemId": "00002", "amount": 20}, {"itemId": "00003", "amount": 20}]},
    "Stage_05": {"staminaCost": 5, "rewards": [{"itemId": "00000", "amount": 250}, {"itemId": "00002", "amount": 25}, {"itemId": "00003", "amount": 25}]},
    "Stage_06": {"staminaCost": 5, "rewards": [{"itemId": "00000", "amount": 300}, {"itemId": "00002", "amount": 30}, {"itemId": "00003", "amount": 30}]},
    "Stage_07": {"staminaCost": 5, "rewards": [{"itemId": "00000", "amount": 350}, {"itemId": "00002", "amount": 35}, {"itemId": "00003", "amount": 35}]},
    "Stage_08": {"staminaCost": 5, "rewards": [{"itemId": "00000", "amount": 400}, {"itemId": "00002", "amount": 40}, {"itemId": "00003", "amount": 40}]},
    "Stage_09": {"staminaCost": 5, "rewards": [{"itemId": "00000", "amount": 450}, {"itemId": "00002", "amount": 45}, {"itemId": "00003", "amount": 45}]},
    "Stage_10": {"staminaCost": 5, "rewards": [{"itemId": "00000", "amount": 500}, {"itemId": "00002", "amount": 50}, {"itemId": "00003", "amount": 50}]},
    "Stage_Fallback": {"staminaCost": 5, "rewards": [{"itemId": "00000", "amount": 20}]}
}

def main():
    sql = "TRUNCATE stage_configs;\n"
    for stage_id, data in stage_configs.items():
        json_str = json.dumps(data)
        # Handle single quotes for SQL
        json_str = json_str.replace("'", "''")
        
        # Insert with both Title Case and lowercase to be safe for fallback matching
        sql += f"INSERT INTO stage_configs (stage_id, json_data) VALUES ('{stage_id}', '{json_str}') ON CONFLICT (stage_id) DO UPDATE SET json_data = '{json_str}';\n"
        sql += f"INSERT INTO stage_configs (stage_id, json_data) VALUES ('{stage_id.lower()}', '{json_str}') ON CONFLICT (stage_id) DO UPDATE SET json_data = '{json_str}';\n"
        
    print("Executing database updates...")
    process = subprocess.Popen(
        ["docker", "exec", "-i", "thefirstsect_dev_postgres_game", "psql", "-U", "mmo_game", "-d", "mmo_game_zone1"],
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        encoding="utf-8"
    )
    stdout, stderr = process.communicate(input=sql)
    
    if process.returncode != 0:
        print("Error:")
        print(stderr)
    else:
        print("Success:")
        print(stdout)

if __name__ == "__main__":
    main()
