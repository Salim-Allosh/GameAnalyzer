import sqlite3

conn = sqlite3.connect('SportsAnalytics_dev.db')
cur = conn.cursor()
cur.execute("""
    SELECT m.MatchDate, t1.Name, t2.Name, m.HomeGoals, m.AwayGoals 
    FROM Matches m 
    JOIN Teams t1 ON m.HomeTeamId=t1.Id 
    JOIN Teams t2 ON m.AwayTeamId=t2.Id 
    WHERE (t1.Name LIKE '%Wolves%' OR t1.Name LIKE '%Wolverhampton%') 
      AND (t2.Name LIKE '%Arsenal%') 
    ORDER BY m.MatchDate DESC 
    LIMIT 5
""")
rows = cur.fetchall()
print("Latest Wolves vs Arsenal matches in database:")
for r in rows:
    print(r)
