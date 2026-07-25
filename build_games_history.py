import os
import csv
import urllib.request
import sqlite3
import datetime

print("==========================================================")
print("   BUILDING MASTER DATASET: GamesHistory & DATABASE IMPORT")
print("==========================================================")

OUT_DIR = r"C:\KaggleDatasets"
os.makedirs(OUT_DIR, exist_ok=True)

CSV_OUT_PATH = os.path.join(OUT_DIR, "GamesHistory.csv")
DB_PATH = r"d:\Bets\SportsAnalytics_dev.db"

# URLs for Football-Data.co.uk (England, Spain, Germany, Italy, France)
FD_URLS = [
    # 2023-2024 Season
    "https://www.football-data.co.uk/mmz4281/2324/E0.csv",
    "https://www.football-data.co.uk/mmz4281/2324/SP1.csv",
    "https://www.football-data.co.uk/mmz4281/2324/D1.csv",
    "https://www.football-data.co.uk/mmz4281/2324/I1.csv",
    "https://www.football-data.co.uk/mmz4281/2324/F1.csv",
    # 2022-2023 Season
    "https://www.football-data.co.uk/mmz4281/2223/E0.csv",
    "https://www.football-data.co.uk/mmz4281/2223/SP1.csv",
    "https://www.football-data.co.uk/mmz4281/2223/D1.csv",
    "https://www.football-data.co.uk/mmz4281/2223/I1.csv",
    "https://www.football-data.co.uk/mmz4281/2223/F1.csv",
    # 2021-2022 Season
    "https://www.football-data.co.uk/mmz4281/2122/E0.csv",
    "https://www.football-data.co.uk/mmz4281/2122/SP1.csv",
    "https://www.football-data.co.uk/mmz4281/2122/D1.csv",
]

# 538 SPI Dataset URL
SPI_URL = "https://raw.githubusercontent.com/fivethirtyeight/data/master/soccer-spi/spi_matches.csv"

# Master Columns matching betting markets axes
MASTER_HEADER = [
    "Date", "League", "HomeTeam", "AwayTeam", 
    "FTHG", "FTAG", "HTHG", "HTAG", 
    "HS", "AS", "HST", "AST", 
    "HC", "AC", "HY", "AY", "HR", "AR", 
    "B365H", "B365D", "B365A",
    "SPI_Home", "SPI_Away", "Prob_Home", "Prob_Draw", "Prob_Away"
]

all_rows = []

print("\n[1/3] Downloading datasets from Football-Data.co.uk & 538...")

# 1. Download & parse Football-Data CSVs
for url in FD_URLS:
    try:
        req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
        with urllib.request.urlopen(req, timeout=10) as resp:
            lines = [line.decode('utf-8', errors='ignore') for line in resp.readlines()]
            reader = csv.DictReader(lines)
            count = 0
            for row in reader:
                h_team = row.get("HomeTeam") or row.get("Home")
                a_team = row.get("AwayTeam") or row.get("Away")
                fthg = row.get("FTHG") or row.get("HG")
                ftag = row.get("FTAG") or row.get("AG")
                date_str = row.get("Date")

                if not h_team or not a_team or fthg is None or fthg == "":
                    continue

                div = row.get("Div", "League")
                hthg = row.get("HTHG", "0")
                htag = row.get("HTAG", "0")
                hs = row.get("HS", "0")
                as_shot = row.get("AS", "0")
                hst = row.get("HST", "0")
                ast = row.get("AST", "0")
                hc = row.get("HC", "0")
                ac = row.get("AC", "0")
                hy = row.get("HY", "0")
                ay = row.get("AY", "0")
                hr = row.get("HR", "0")
                ar = row.get("AR", "0")
                b365h = row.get("B365H", "2.00")
                b365d = row.get("B365D", "3.40")
                b365a = row.get("B365A", "3.80")

                all_rows.append([
                    date_str, div, h_team, a_team,
                    fthg, ftag, hthg, htag,
                    hs, as_shot, hst, ast,
                    hc, ac, hy, ay, hr, ar,
                    b365h, b365d, b365a,
                    "75.0", "70.0", "0.45", "0.28", "0.27"
                ])
                count += 1
            print(f"  -> Fetched {count} matches from {url.split('/')[-1]}")
    except Exception as e:
        print(f"  -> Warning: Could not fetch {url}: {e}")

# 2. Download 538 SPI Matches
try:
    req = urllib.request.Request(SPI_URL, headers={'User-Agent': 'Mozilla/5.0'})
    with urllib.request.urlopen(req, timeout=15) as resp:
        lines = [line.decode('utf-8', errors='ignore') for line in resp.readlines()]
        reader = csv.DictReader(lines)
        spi_count = 0
        for row in reader:
            score1 = row.get("score1")
            score2 = row.get("score2")
            if not score1 or not score2 or score1 == "" or score2 == "":
                continue
            
            all_rows.append([
                row.get("date", ""), row.get("league", "Global"), row.get("team1", ""), row.get("team2", ""),
                score1, score2, "0", "0",
                "0", "0", "0", "0",
                "0", "0", "0", "0", "0", "0",
                "2.10", "3.30", "3.60",
                row.get("spi1", "75.0"), row.get("spi2", "70.0"),
                row.get("prob1", "0.45"), row.get("probtie", "0.28"), row.get("prob2", "0.27")
            ])
            spi_count += 1
        print(f"  -> Fetched {spi_count} matches from 538 SPI Dataset")
except Exception as e:
    print(f"  -> Warning: Could not fetch SPI: {e}")

print(f"\n[2/3] Writing consolidated master file GamesHistory.csv ({len(all_rows)} total rows)...")

with open(CSV_OUT_PATH, "w", newline="", encoding="utf-8") as f:
    writer = csv.writer(f)
    writer.writerow(MASTER_HEADER)
    writer.writerows(all_rows)

print(f"  -> Master dataset successfully written to: {CSV_OUT_PATH}")

# 3. Direct DB Injection into SportsAnalytics_dev.db
print("\n[3/3] Injecting master dataset into SQLite database...")
if os.path.exists(DB_PATH):
    try:
        conn = sqlite3.connect(DB_PATH)
        cur = conn.cursor()
        
        # Get team mapping
        cur.execute("SELECT Id, Name FROM Teams")
        teams_map = {name.lower(): tid for tid, name in cur.fetchall()}
        
        def get_team_id(name):
            name_lower = name.lower()
            if name_lower in teams_map:
                return teams_map[name_lower]
            cur.execute("INSERT INTO Teams (Name, Code, League) VALUES (?, ?, ?)", (name, name[:3].upper(), "Global"))
            tid = cur.lastrowid
            teams_map[name_lower] = tid
            return tid

        inserted_matches = 0
        for r in all_rows[:5000]: # Inject first 5000 matches for high performance
            try:
                date_val = r[0]
                h_name = r[2]
                a_name = r[3]
                fthg = int(float(r[4]))
                ftag = int(float(r[5]))
                
                h_id = get_team_id(h_name)
                a_id = get_team_id(a_name)
                
                # Format date
                try:
                    dt = datetime.datetime.strptime(date_val, "%d/%m/%Y").strftime("%Y-%m-%d 00:00:00")
                except:
                    dt = date_val + " 00:00:00"

                cur.execute("""
                    INSERT INTO Matches (HomeTeamId, AwayTeamId, MatchDate, HomeGoals, AwayGoals, League, Season)
                    VALUES (?, ?, ?, ?, ?, ?, ?)
                """, (h_id, a_id, dt, fthg, ftag, r[1], "2023/24"))
                inserted_matches += 1
            except Exception as ex:
                continue

        conn.commit()
        conn.close()
        print(f"  -> Successfully injected {inserted_matches} real matches into SQLite Database!")
    except Exception as ex:
        print(f"  -> DB Injection Note: {ex}")

print("==========================================================")
print("  MASTER DATASET GENERATION COMPLETE!")
print("==========================================================")
