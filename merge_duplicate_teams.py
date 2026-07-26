import sqlite3

print("==========================================================")
print(" MERGING DUPLICATE TEAMS IN DATABASE")
print("==========================================================")

db_path = r"d:\Bets\SportsAnalytics_dev.db"
conn = sqlite3.connect(db_path)
cur = conn.cursor()

# Aliases mapping: short/alternate name -> canonical full name
aliases = {
    "Wolves": "Wolverhampton Wanderers",
    "Man City": "Manchester City",
    "Man United": "Manchester United",
    "Tottenham": "Tottenham Hotspur",
    "West Ham": "West Ham United",
    "Newcastle": "Newcastle United",
    "Nott'm Forest": "Nottingham Forest",
    "Luton": "Luton Town",
    "Ath Bilbao": "Athletic Bilbao",
    "Ath Madrid": "Atletico Madrid",
    "Celta": "Celta Vigo",
    "Betis": "Real Betis",
    "Sociedad": "Real Sociedad",
    "Vallecano": "Rayo Vallecano",
    "Ein Frankfurt": "Eintracht Frankfurt",
    "Leverkusen": "Bayer Leverkusen",
    "M'gladbach": "Monchengladbach",
    "Milan": "AC Milan",
    "Verona": "Hellas Verona",
    "Paris SG": "PSG"
}

# Fetch canonical teams map
cur.execute("SELECT Id, Name FROM Teams")
teams = cur.fetchall()

name_to_id = {t[1]: t[0] for t in teams}

merged_count = 0
matches_reassigned = 0

for alt_name, canon_name in aliases.items():
    if alt_name in name_to_id and canon_name in name_to_id:
        alt_id = name_to_id[alt_name]
        canon_id = name_to_id[canon_name]
        
        # Reassign Matches HomeTeamId
        cur.execute("UPDATE Matches SET HomeTeamId = ? WHERE HomeTeamId = ?", (canon_id, alt_id))
        h_changes = cur.rowcount
        
        # Reassign Matches AwayTeamId
        cur.execute("UPDATE Matches SET AwayTeamId = ? WHERE AwayTeamId = ?", (canon_id, alt_id))
        a_changes = cur.rowcount
        
        matches_reassigned += (h_changes + a_changes)
        
        # Delete duplicate team
        cur.execute("DELETE FROM Teams WHERE Id = ?", (alt_id,))
        merged_count += 1
        
        print(f"  Merged '{alt_name}' (Id {alt_id}) -> '{canon_name}' (Id {canon_id}) [{h_changes + a_changes} matches updated]")

conn.commit()
conn.close()

print("\n==========================================================")
print(f" SUCCESS: Merged {merged_count} duplicate teams and updated {matches_reassigned:,} match records!")
print("==========================================================")
