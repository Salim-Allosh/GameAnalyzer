import sqlite3
import os
import urllib.request
import urllib.parse

db_path = r"d:\Bets\SportsAnalytics_dev.db"
logos_dir = r"d:\Bets\SportsAnalytics.Desktop\Assets\Logos"

# Connect to database
conn = sqlite3.connect(db_path)
cursor = conn.cursor()

# Get all unique team names
cursor.execute("SELECT DISTINCT Name FROM Teams")
teams = cursor.fetchall()

print(f"Found {len(teams)} teams. Downloading logos...")

for team in teams:
    team_name = team[0]
    # Clean up filename
    safe_name = team_name.replace("/", "_").replace("\\", "_")
    file_path = os.path.join(logos_dir, f"{safe_name}.png")
    
    if not os.path.exists(file_path):
        try:
            # Using UI-Avatars for clean placeholder logos that look like real generic team logos
            encoded_name = urllib.parse.quote(team_name)
            url = f"https://ui-avatars.com/api/?name={encoded_name}&background=random&color=fff&size=128&bold=true"
            
            req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
            with urllib.request.urlopen(req) as response, open(file_path, 'wb') as out_file:
                out_file.write(response.read())
            print(f"Downloaded logo for: {team_name}")
        except Exception as e:
            print(f"Failed to download logo for {team_name}: {e}")
    else:
        print(f"Logo already exists for: {team_name}")

conn.close()
print("Done.")
