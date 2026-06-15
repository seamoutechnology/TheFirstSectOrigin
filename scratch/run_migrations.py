import os
import subprocess
import sys

def main():
    migration_dir = r"c:\Project\TheFirstSectOrigin\server\db\migrations\game"
    files = sorted([f for f in os.listdir(migration_dir) if f.endswith(".sql")])
    
    for filename in files:
        filepath = os.path.join(migration_dir, filename)
        print(f"Parsing {filename}...")
        
        with open(filepath, "r", encoding="utf-8-sig") as f:
            content = f.read()
            
        # Extract the UP section
        up_idx = content.find("-- +goose Up")
        if up_idx == -1:
            up_idx = 0
        else:
            up_idx += len("-- +goose Up")
            
        down_idx = content.find("-- +goose Down")
        if down_idx == -1:
            up_sql = content[up_idx:]
        else:
            up_sql = content[up_idx:down_idx]
            
        up_sql = up_sql.strip()
        if not up_sql:
            continue
            
        print(f"Applying UP SQL from {filename}...")
        
        # Execute using docker exec
        process = subprocess.Popen(
            ["docker", "exec", "-i", "thefirstsect_dev_postgres_game", "psql", "-U", "mmo_game", "-d", "mmo_game_zone1"],
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            encoding="utf-8"
        )
        stdout, stderr = process.communicate(input=up_sql)
        
        if process.returncode != 0:
            print(f"Error applying {filename}:")
            print(stderr)
        else:
            if "ERROR" in stderr or "ERROR" in stdout:
                print(f"Finished {filename} with notices/errors:")
                print(stderr or stdout)
            else:
                print(f"Successfully applied {filename}")

if __name__ == "__main__":
    main()
