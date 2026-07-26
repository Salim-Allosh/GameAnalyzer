import sqlite3

conn = sqlite3.connect('SportsAnalytics_dev.db')
cur = conn.cursor()

cur.execute("SELECT Id, Name, League FROM Teams ORDER BY Name")
teams = cur.fetchall()

print(f"Total Teams in DB: {len(teams)}")
print("\nSample Teams:")
for t in teams:
    print(t)
