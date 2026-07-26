import sqlite3

conn = sqlite3.connect('SportsAnalytics_dev.db')
cur = conn.cursor()

cur.execute("SELECT Id, Name, League FROM Teams WHERE Name LIKE '%Wolves%' OR Name LIKE '%Wolverhampton%'")
print("Teams in DB matching Wolves/Wolverhampton:")
for r in cur.fetchall():
    print(r)
