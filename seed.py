"""
seed.py - Police Portal fake data generator.

Install deps:  pip install faker requests
Run:           python seed.py
"""

import random

import requests
from faker import Faker

# ─── Config ───────────────────────────────────────────────────────────────────
API_BASE   = "http://localhost:5000/api"
USERNAME   = "admin"
PASSWORD   = "Password123!"
NUM_OFFICERS  = 15
NUM_LOCATIONS = 30
NUM_INCIDENTS = 100
fake = Faker()
Faker.seed(42)
random.seed(42)

# ─── Reference data (must match DB seeds) ────────────────────────────────────
INCIDENT_TYPE_IDS   = list(range(1, 8))   # 7 types seeded
INCIDENT_STATUS_IDS = list(range(1, 5))   # 4 statuses seeded
RANKS = ["Officer", "Senior Officer", "Corporal", "Sergeant", "Lieutenant", "Detective"]
PRECINCTS = ["1st Precinct", "2nd Precinct", "3rd Precinct", "4th Precinct"]


# ─── Auth ─────────────────────────────────────────────────────────────────────
def get_token() -> str:
    resp = requests.post(f"{API_BASE}/auth/login", json={
        "username": USERNAME,
        "password": PASSWORD
    }, timeout=30)
    resp.raise_for_status()
    token = resp.json()["token"]
    print("Authenticated - token obtained")
    return token


# ─── Seed helpers ─────────────────────────────────────────────────────────────
def seed_officers(headers: dict) -> list[int]:
    ids = []
    for _ in range(NUM_OFFICERS):
        payload = {
            "badgeNumber": f"RVA-{fake.unique.numerify('####')}",
            "firstName":   fake.first_name(),
            "lastName":    fake.last_name(),
            "rank":        random.choice(RANKS),
            "precinct":    random.choice(PRECINCTS),
            "isActive":    True
        }
        resp = requests.post(f"{API_BASE}/officers", json=payload, headers=headers, timeout=30)
        resp.raise_for_status()
        ids.append(resp.json()["id"])
    print(f"Created {len(ids)} officers")
    return ids


def seed_locations(headers: dict) -> list[int]:
    ids = []
    for _ in range(NUM_LOCATIONS):
        payload = {
            "street": fake.street_address(),
            "city": "Richmond",
            "state": "VA",
            "zipCode": fake.postcode(),
            "precinct": random.choice(PRECINCTS)
        }
        resp = requests.post(f"{API_BASE}/locations", json=payload, headers=headers, timeout=30)
        resp.raise_for_status()
        ids.append(resp.json()["id"])
    print(f"Created {len(ids)} locations")
    return ids


def seed_incidents(headers: dict, officer_ids: list[int], location_ids: list[int]):
    count = 0
    for _ in range(NUM_INCIDENTS):
        occurred = fake.date_time_between(start_date="-1y", end_date="now")
        payload = {
            "description":      fake.paragraph(nb_sentences=3),
            "occurredAt":       occurred.isoformat(),
            "incidentTypeId":   random.choice(INCIDENT_TYPE_IDS),
            "incidentStatusId": random.choice(INCIDENT_STATUS_IDS),
            "locationId":       random.choice(location_ids),
            "officerId":        random.choice(officer_ids)
        }
        resp = requests.post(f"{API_BASE}/incidents", json=payload, headers=headers, timeout=30)
        resp.raise_for_status()
        count += 1
    print(f"Created {count} incidents")


def smoke_test_reports(headers: dict):
    checks = [
        ("/reports/open-by-precinct", {"precinct": PRECINCTS[0]}),
        ("/reports/monthly-summary", {}),
        ("/reports/officer-workload", {"activeOnly": "true"})
    ]
    for route, params in checks:
        resp = requests.get(f"{API_BASE}{route}", headers=headers, params=params, timeout=30)
        resp.raise_for_status()
        print(f"Smoke test passed: GET {route} -> {resp.status_code}")

# ─── Main ─────────────────────────────────────────────────────────────────────
if __name__ == "__main__":
    print("Starting Police Portal seed...\n")

    # Step 1: Authenticate
    token   = get_token()
    headers = {"Authorization": f"Bearer {token}"}

    # Step 2: Seed locations and officers via API
    location_ids = seed_locations(headers)
    officer_ids = seed_officers(headers)

    # Step 3: Seed incidents via API
    seed_incidents(headers, officer_ids, location_ids)
    print("\nIncident seed complete!")

    # Step 4: Smoke test report endpoints backed by stored procedures
    smoke_test_reports(headers)
    print("\nSeed complete!")