import os

def main():
    json_path = r"c:\Project\TheFirstSectOrigin\scratch\default_base_map.json"
    sql_path = r"c:\Project\TheFirstSectOrigin\server\db\migrations\game\001_init.sql"
    
    if not os.path.exists(json_path):
        print(f"Error: {json_path} not found.")
        return
        
    if not os.path.exists(sql_path):
        print(f"Error: {sql_path} not found.")
        return
        
    with open(json_path, "r", encoding="utf-8") as f:
        json_data = f.read().strip()
        
    with open(sql_path, "r", encoding="utf-8") as f:
        sql_content = f.read()
        
    target_table_def = """-- ============================================================
-- Admin Maps
-- ============================================================
CREATE TABLE IF NOT EXISTS admin_maps (
    id VARCHAR(50) PRIMARY KEY,
    json_data TEXT NOT NULL,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);"""

    if target_table_def not in sql_content:
        print("Error: Could not find Admin Maps table definition in SQL file.")
        return
        
    insert_statement = f"""

INSERT INTO admin_maps (id, json_data) VALUES
('default_base', $${json_data}$$)
ON CONFLICT (id) DO NOTHING;"""

    new_content = sql_content.replace(target_table_def, target_table_def + insert_statement)
    
    with open(sql_path, "w", encoding="utf-8") as f:
        f.write(new_content)
        
    print("Successfully seeded default_base map into 001_init.sql!")

if __name__ == "__main__":
    main()
