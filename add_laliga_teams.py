import sqlite3
import datetime

db_path = r"d:\Bets\SportsAnalytics_dev.db"

teams = [
    "Atletico Madrid", "Sevilla", "Real Sociedad", "Villarreal",
    "Athletic Club", "Real Betis", "Valencia", "Osasuna",
    "Getafe", "Celta Vigo", "Mallorca", "Girona",
    "Rayo Vallecano", "Alaves", "Las Palmas", "Granada",
    "Cadiz", "Almeria"
]

conn = sqlite3.connect(db_path)
cursor = conn.cursor()

now = datetime.datetime.utcnow().isoformat()

for team in teams:
    # Check if team already exists
    cursor.execute("SELECT Id FROM Teams WHERE Name = ?", (team,))
    if not cursor.fetchone():
        cursor.execute("INSERT INTO Teams (Name, Country, League, CreatedAt) VALUES (?, ?, ?, ?)", 
                       (team, "Spain", "La Liga", now))
        print(f"Added {team}")
    else:
        print(f"{team} already exists")

conn.commit()
conn.close()
print("Finished adding La Liga teams.")
