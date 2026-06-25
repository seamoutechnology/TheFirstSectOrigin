import sys
sys.stdout.reconfigure(encoding='utf-8')

filepath = r'c:\Project\TheFirstSectOrigin\doc\Chapter\4_Design_Implementation.tex'

with open(filepath, 'r', encoding='utf-8') as f:
    lines = f.readlines()

start_idx = -1
end_idx = -1

for i, line in enumerate(lines):
    if r'\begin{tikzpicture}[node distance=1.3cm and 1.8cm' in line:
        start_idx = i - 2 # Capture \begin{figure} \centering \resizebox
    if r'\caption{Sơ đồ thực thể liên kết' in line:
        end_idx = i - 1 # Capture \end{tikzpicture} }

if start_idx != -1 and end_idx != -1:
    new_erd = r"""\begin{figure}[H]
\centering
\resizebox{0.95\textwidth}{!}{
\begin{tikzpicture}[node distance=1.3cm and 1.8cm, 
  tablebox/.style={draw, rectangle, rounded corners=2pt, minimum width=3.3cm, align=left, fill=gray!5, font=\scriptsize}]
  
  % === ROW 1: Global DB (y=12) ===
  \node[tablebox] (social) at (-5.5, 12) {
    \textbf{social\_accounts} \\
    \underline{id: BIGSERIAL} (PK) \\
    user\_id: BIGINT (FK) \\
    provider: VARCHAR(32) \\
    provider\_id: VARCHAR(255) \\
    created\_at: TIMESTAMPTZ
  };

  \node[tablebox] (users) at (0, 12) {
    \textbf{users (Global DB)} \\
    \underline{id: BIGSERIAL} (PK) \\
    username: VARCHAR(32) (Unique) \\
    email: VARCHAR(128) (Unique) \\
    password\_hash: VARCHAR(255) \\
    is\_banned: BOOLEAN \\
    ban\_reason: TEXT \\
    created\_at: TIMESTAMPTZ \\
    updated\_at: TIMESTAMPTZ
  };

  % === ROW 2: Core Game Tables (y=6) ===
  \node[tablebox] (zones) at (-7, 6) {
    \textbf{zones} \\
    \underline{id: SERIAL} (PK) \\
    name: VARCHAR(64) \\
    status: VARCHAR(16) \\
    gateway\_url: VARCHAR(255) \\
    is\_active: BOOLEAN \\
    created\_at: TIMESTAMPTZ
  };

  \node[tablebox] (players) at (0, 6) {
    \textbf{players (Game Shard DB)} \\
    \underline{id: BIGSERIAL} (PK) \\
    user\_id: BIGINT (Unique, FK chéo) \\
    nickname: VARCHAR(32) \\
    level: INT \\
    exp: BIGINT \\
    stamina: INT \\
    max\_stamina: INT \\
    last\_stamina\_at: TIMESTAMPTZ \\
    created\_at: TIMESTAMPTZ \\
    updated\_at: TIMESTAMPTZ
  };

  \node[tablebox] (player_heroes) at (7, 6) {
    \textbf{player\_heroes} \\
    \underline{id: BIGSERIAL} (PK) \\
    player\_id: BIGINT (FK) \\
    hero\_code: VARCHAR(32) (FK) \\
    level: INT \\
    star: INT \\
    exp: BIGINT \\
    created\_at: TIMESTAMPTZ
  };

  \node[tablebox] (hero_templates) at (14, 6) {
    \textbf{hero\_templates} \\
    \underline{code: VARCHAR(32)} (PK) \\
    name: VARCHAR(64) \\
    rarity: VARCHAR(8) \\
    element: VARCHAR(16) \\
    role: VARCHAR(16) \\
    base\_hp: INT \\
    base\_atk: INT \\
    base\_def: INT \\
    base\_speed: INT \\
    gacha\_weight: INT \\
    is\_active: BOOLEAN
  };

  % === ROW 3: Junction & Item Tables (y=0) ===
  \node[tablebox] (buildings) at (-12, 0) {
    \textbf{buildings} \\
    \underline{code: VARCHAR(32)} (PK) \\
    name: VARCHAR(64) \\
    max\_level: INT \\
    description: TEXT
  };

  \node[tablebox] (player_buildings) at (-6, 0) {
    \textbf{player\_buildings} \\
    \underline{id: BIGSERIAL} (PK) \\
    player\_id: BIGINT (FK) \\
    building\_code: VARCHAR(32) (FK) \\
    level: INT \\
    upgrade\_end\_at: TIMESTAMPTZ \\
    last\_collect\_at: TIMESTAMPTZ \\
    created\_at: TIMESTAMPTZ
  };

  \node[tablebox] (user_items) at (7, 0) {
    \textbf{user\_items} \\
    \underline{id: BIGSERIAL} (PK) \\
    player\_id: BIGINT (FK) \\
    item\_code: VARCHAR(100) (FK) \\
    quantity: INT \\
    stats: JSONB \\
    created\_at: TIMESTAMPTZ
  };

  \node[tablebox] (item_configs) at (14, 0) {
    \textbf{item\_configs} \\
    \underline{item\_code: VARCHAR(100)} (PK) \\
    name\_key: VARCHAR(255) \\
    type: VARCHAR(50) \\
    rarity: VARCHAR(50) \\
    icon: VARCHAR(255) \\
    max\_stack: INT
  };

  % === ROW 4: Gacha & Formation (y=-6) ===
  \node[tablebox] (banners) at (-6, -6) {
    \textbf{gacha\_banners} \\
    \underline{id: SERIAL} (PK) \\
    name: VARCHAR(64) \\
    description: TEXT \\
    cost\_diamond: INT \\
    pity\_count: INT \\
    is\_active: BOOLEAN \\
    end\_at: TIMESTAMPTZ
  };

  \node[tablebox] (player_pity) at (0, -6) {
    \textbf{player\_gacha\_pity} \\
    \underline{player\_id: BIGINT} (PK, FK) \\
    \underline{banner\_id: INT} (PK, FK) \\
    pull\_count: INT
  };

  \node[tablebox] (player_formations) at (7, -6) {
    \textbf{player\_formations} \\
    \underline{player\_id: BIGINT} (PK, FK) \\
    \underline{position: INT} (PK) \\
    player\_hero\_id: BIGINT (FK)
  };

  % ========== CONNECTORS ==========

  % Row 1: users <-> social (horizontal, clear)
  \draw[thick] (users.west) -- (social.east) node[midway, above, font=\scriptsize] {1:N};

  % Row 1 -> Row 2: users -> players (vertical, clear center column)
  \draw[thick, dashed] (users.south) -- (players.north) node[midway, right, font=\scriptsize] {1:1};

  % Row 2: players -> player_heroes (horizontal, clear)
  \draw[thick] (players.east) -- (player_heroes.west) node[midway, above, font=\scriptsize] {1:N};

  % Row 2: player_heroes -> hero_templates (horizontal, clear)
  \draw[thick] (player_heroes.east) -- (hero_templates.west) node[midway, above, font=\scriptsize] {1:N};

  % Row 2 -> Row 3: players -> player_buildings (down then left to top of player_buildings)
  \draw[thick] ([xshift=-1.0cm]players.south) -- ++(0, -2.0) -| (player_buildings.north) node[pos=0.25, right, font=\scriptsize] {1:N};

  % Row 3: buildings -> player_buildings (horizontal, clear)
  \draw[thick] (buildings.east) -- (player_buildings.west) node[midway, above, font=\scriptsize] {1:N};

  % Row 2 -> Row 3: players -> user_items (down then right to west of user_items)
  \draw[thick] ([xshift=1.0cm]players.south) -- ++(0, -2.5) -| (user_items.west) node[pos=0.25, left, font=\scriptsize] {1:N};

  % Row 3: item_configs -> user_items (horizontal, clear)
  \draw[thick] (item_configs.west) -- (user_items.east) node[midway, above, font=\scriptsize] {1:N};

  % Row 2 -> Row 4: players -> player_pity (vertical center column)
  \draw[thick] (players.south) -- (player_pity.north) node[midway, right, font=\scriptsize] {1:N};

  % Row 4: banners -> player_pity (horizontal, clear)
  \draw[thick] (banners.east) -- (player_pity.west) node[midway, above, font=\scriptsize] {1:N};

  % Row 2 -> Row 4: players -> player_formations (right to x=3.5, down to y=-6, right to player_formations)
  \draw[thick] ([yshift=-1.0cm]players.east) -- ++(3.5, 0) |- (player_formations.west) node[pos=0.25, right, font=\scriptsize] {1:N};

  % Row 2 -> Row 4: player_heroes -> player_formations (right to x=10, down to y=-6, left to player_formations)
  \draw[thick] ([yshift=-1.0cm]player_heroes.east) -- ++(3.0, 0) |- (player_formations.east) node[pos=0.25, right, font=\scriptsize] {0..1:N};

\end{tikzpicture}
}
"""
    new_lines = lines[:start_idx] + [new_erd] + lines[end_idx+1:]
    with open(filepath, 'w', encoding='utf-8', newline='') as f:
        f.writelines(new_lines)
    print("ERD updated successfully!")
else:
    print("Could not find ERD bounds!")
