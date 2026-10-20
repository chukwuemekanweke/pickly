#!/usr/bin/env python3
"""Build reviewed Enugu street seed rows from saved Overpass way geometry.

The script is intentionally offline: download the exact OSM ways separately, keep
the JSON responses as evidence, then run this tool against those snapshots.
"""

from __future__ import annotations

import argparse
import csv
import json
import math
import pathlib
import re
from collections import Counter, defaultdict


ALLOWED_HIGHWAYS = {
    "primary",
    "secondary",
    "tertiary",
    "unclassified",
    "residential",
    "living_street",
}
STREET_SUFFIX = re.compile(
    r"(?:Road|Street|Avenue|Close|Crescent|Drive|Lane|Way|Boulevard|Highway)$"
)
MANUAL_EXCLUSIONS = {
    "old abakiliki road": "misspelled variant of Old Abakaliki Road",
    "abakiliki road": "misspelled variant of Abakaliki Road",
    "abakiliki link road": "misspelled variant of Abakaliki Link Road",
    "enugu porthacourt road": "misspelling of Port Harcourt",
    "enugu port hacourt road": "misspelling of Port Harcourt",
    "enugu porthacourt link road": "misspelling of Port Harcourt",
    "salvage crescent": "conflicts with seeded Savage Crescent",
    "udeji amechi ukononu": "inconsistent spelling and no street type",
    "udeji amechi ukunonu": "inconsistent spelling and no street type",
    "wales road": "unverified casing or spelling",
    "penoks street": "unverified casing or spelling",
    "fsp avenue": "unverified abbreviation",
    "ugbo odogwu": "area-like name without a street type",
    "artisan quarters": "area name, not a street",
    "link road": "generic name",
    "robison street": "possible misspelling of Robinson Street",
    "old garriki road": "spelling conflicts with the Gariki place name",
    "agu enugu street": "representative geometry point falls outside the active Enugu zone union",
    "amauzam road": "representative geometry point falls outside the active Enugu zone union",
    "ideato street": "representative geometry point falls outside the active Enugu zone union",
    "isiagu road": "representative geometry point falls outside the active Enugu zone union",
    "okpuno road": "representative geometry point falls outside the active Enugu zone union",
}


def normalize(value: str) -> str:
    return re.sub(r"\s+", " ", re.sub(r"[^\w\s]", " ", value.lower())).strip()


def haversine(a: tuple[float, float], b: tuple[float, float]) -> float:
    lat1, lon1 = map(math.radians, a)
    lat2, lon2 = map(math.radians, b)
    dlat, dlon = lat2 - lat1, lon2 - lon1
    h = math.sin(dlat / 2) ** 2 + math.cos(lat1) * math.cos(lat2) * math.sin(dlon / 2) ** 2
    return 6_371_008.8 * 2 * math.asin(math.sqrt(h))


def way_length(geometry: list[dict[str, float]]) -> float:
    points = [(point["lat"], point["lon"]) for point in geometry]
    return sum(haversine(a, b) for a, b in zip(points, points[1:]))


def midpoint(geometry: list[dict[str, float]]) -> tuple[float, float]:
    points = [(point["lat"], point["lon"]) for point in geometry]
    lengths = [haversine(a, b) for a, b in zip(points, points[1:])]
    target = sum(lengths) / 2
    traversed = 0.0
    for (lat1, lon1), (lat2, lon2), length in zip(points, points[1:], lengths):
        if traversed + length >= target and length:
            ratio = (target - traversed) / length
            return lat1 + (lat2 - lat1) * ratio, lon1 + (lon2 - lon1) * ratio
        traversed += length
    return points[-1]


class DisjointSet:
    def __init__(self, values: list[int]):
        self.parent = {value: value for value in values}

    def find(self, value: int) -> int:
        while self.parent[value] != value:
            self.parent[value] = self.parent[self.parent[value]]
            value = self.parent[value]
        return value

    def union(self, left: int, right: int) -> None:
        left_root, right_root = self.find(left), self.find(right)
        if left_root != right_root:
            self.parent[right_root] = left_root


def load_ways(geometry_dir: pathlib.Path) -> tuple[dict[int, dict], list[str]]:
    ways: dict[int, dict] = {}
    timestamps: list[str] = []
    for path in sorted(geometry_dir.glob("streets-*.json")):
        payload = json.loads(path.read_text(encoding="utf-8-sig"))
        timestamp = payload.get("osm3s", {}).get("timestamp_osm_base")
        if timestamp:
            timestamps.append(timestamp)
        for element in payload.get("elements", []):
            if element.get("type") == "way" and element.get("geometry"):
                candidate = dict(element)
                candidate["_source_timestamp"] = timestamp
                # Current split-batch files sort before the preserved fallback file.
                ways.setdefault(element["id"], candidate)
    return ways, timestamps


def load_existing_streets(seed_path: pathlib.Path) -> tuple[dict[int, str], dict[str, str]]:
    sql = seed_path.read_text(encoding="utf-8")
    streets_sql = sql.split("UNION ALL", 1)[0]
    matches = re.findall(
        r"\('way/(\d+)',\s*'((?:[^']|'')+)',\s*\d+(?:\.\d+)?,\s*\d+(?:\.\d+)?\)",
        streets_sql,
    )
    by_id = {int(identifier): name.replace("''", "'") for identifier, name in matches}
    by_normalized = {normalize(name): name for name in by_id.values()}
    if len(by_id) != 130:
        raise RuntimeError(f"Expected 130 streets in 0005, found {len(by_id)}")
    return by_id, by_normalized


def connected_components(ways: list[dict]) -> list[list[dict]]:
    identifiers = [way["id"] for way in ways]
    sets = DisjointSet(identifiers)
    owners: dict[tuple[float, float], int] = {}
    for way in ways:
        for point in way["geometry"]:
            key = round(point["lat"], 7), round(point["lon"], 7)
            owner = owners.setdefault(key, way["id"])
            sets.union(owner, way["id"])
    grouped: dict[int, list[dict]] = defaultdict(list)
    for way in ways:
        grouped[sets.find(way["id"])].append(way)
    return list(grouped.values())


def sql_string(value: str) -> str:
    return value.replace("'", "''")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--geometry-dir", required=True, type=pathlib.Path)
    parser.add_argument("--seed", required=True, type=pathlib.Path)
    parser.add_argument("--output-dir", required=True, type=pathlib.Path)
    args = parser.parse_args()

    ways, timestamps = load_ways(args.geometry_dir)
    existing_by_id, existing_by_name = load_existing_streets(args.seed)
    if not ways:
        raise RuntimeError("No geometry ways were found")

    by_name: dict[str, list[dict]] = defaultdict(list)
    for way in ways.values():
        name = way.get("tags", {}).get("name", "").strip()
        if name:
            by_name[normalize(name)].append(way)

    rows: list[dict[str, object]] = []
    accepted: list[dict[str, object]] = []
    for normalized_name, named_ways in sorted(by_name.items()):
        components = connected_components(named_ways)
        disconnected = len(components) > 1
        for component_index, component in enumerate(components, 1):
            exact_names = Counter(way["tags"]["name"].strip() for way in component)
            name = sorted(exact_names, key=lambda candidate: (-exact_names[candidate], candidate))[0]
            identifiers = {way["id"] for way in component}
            seeded_identifiers = identifiers.intersection(existing_by_id)
            length = sum(way_length(way["geometry"]) for way in component)
            highway_types = sorted({way.get("tags", {}).get("highway", "") for way in component})
            access_values = sorted({way.get("tags", {}).get("access", "") for way in component if way.get("tags", {}).get("access")})

            decision = "accepted_new"
            reason = "single connected, public drivable street with reviewed naming and length"
            canonical_name = name
            existing = False
            if seeded_identifiers:
                seeded_names = {existing_by_id[identifier] for identifier in seeded_identifiers}
                if len({normalize(value) for value in seeded_names}) != 1:
                    raise RuntimeError(f"Component contains conflicting seeded streets: {seeded_names}")
                canonical_name = sorted(seeded_names)[0]
                decision = "accepted_existing_reference"
                reason = "connected to a street already seeded by 0005"
                existing = True
            elif normalized_name in existing_by_name:
                decision = "excluded_existing_name_unconnected"
                reason = "same canonical identity as 0005 but not connected to its seeded OSM way"
            elif disconnected:
                decision = "excluded_disconnected_duplicate_name"
                reason = f"name forms {len(components)} disconnected street identities"
            elif normalized_name in MANUAL_EXCLUSIONS:
                decision = "excluded_manual_data_quality"
                reason = MANUAL_EXCLUSIONS[normalized_name]
            elif not name or not name[0].isupper() or not STREET_SUFFIX.search(name):
                decision = "excluded_name_shape"
                reason = "name lacks reviewed capitalization or street-type suffix"
            elif not set(highway_types).issubset(ALLOWED_HIGHWAYS):
                decision = "excluded_road_class"
                reason = "contains a non-public/non-drivable or intercity road class"
            elif any(value in {"private", "no", "customers", "permit"} for value in access_values):
                decision = "excluded_access"
                reason = "access tag is private, prohibited, customer-only or permit-only"
            elif length < 300:
                decision = "excluded_too_short"
                reason = "connected geometry is shorter than 300 metres"
            elif length > 10_000:
                decision = "excluded_too_long"
                reason = "connected geometry is longer than 10 kilometres"

            row = {
                "normalized_name": normalized_name,
                "name": canonical_name,
                "component": component_index,
                "component_count_for_name": len(components),
                "way_count": len(component),
                "length_metres": round(length, 1),
                "highway_types": ";".join(highway_types),
                "access_values": ";".join(access_values),
                "decision": decision,
                "reason": reason,
                "osm_refs": ";".join(f"way/{identifier}" for identifier in sorted(identifiers)),
            }
            rows.append(row)
            if decision.startswith("accepted_"):
                longest = max(component, key=lambda way: way_length(way["geometry"]))
                lat, lon = midpoint(longest["geometry"])
                accepted.append(
                    {
                        "name": canonical_name,
                        "normalized_name": normalize(canonical_name),
                        "ways": sorted(component, key=lambda way: way["id"]),
                        "representative_id": longest["id"],
                        "latitude": round(lat, 7),
                        "longitude": round(lon, 7),
                        "existing": existing,
                    }
                )

    args.output_dir.mkdir(parents=True, exist_ok=True)
    review_path = args.output_dir / "street-geometry-review.csv"
    with review_path.open("w", newline="", encoding="utf-8-sig") as output:
        writer = csv.DictWriter(output, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)

    sql_lines = []
    for street in sorted(accepted, key=lambda item: (item["normalized_name"], item["representative_id"])):
        ways_to_emit = [
            way
            for way in street["ways"]
            if not street["existing"] or way["id"] not in existing_by_id
        ]
        for way in ways_to_emit:
            create = not street["existing"] and way["id"] == street["representative_id"]
            latitude = f"{street['latitude']:.7f}" if create else "NULL"
            longitude = f"{street['longitude']:.7f}" if create else "NULL"
            sql_lines.append(
                "    ('way/{id}', 'street', 'Street', '{name}', {lat}, {lon}, {create})".format(
                    id=way["id"],
                    name=sql_string(str(street["name"])),
                    lat=latitude,
                    lon=longitude,
                    create="TRUE" if create else "FALSE",
                )
            )
    (args.output_dir / "street-values.generated.sql").write_text(
        ",\n".join(sql_lines) + "\n", encoding="utf-8"
    )

    decisions = Counter(str(row["decision"]) for row in rows)
    latest_source_date = max(timestamps)[:10] if timestamps else None
    summary = {
        "geometry_source_files": len(list(args.geometry_dir.glob("streets-*.json"))),
        "osm_base_timestamp_min": min(timestamps) if timestamps else None,
        "osm_base_timestamp_max": max(timestamps) if timestamps else None,
        "source_ways": len(ways),
        "stale_fallback_ways": sum(
            1
            for way in ways.values()
            if latest_source_date
            and str(way.get("_source_timestamp", ""))[:10] < latest_source_date
        ),
        "distinct_normalized_names": len(by_name),
        "connected_components": len(rows),
        "new_streets": sum(not bool(street["existing"]) for street in accepted),
        "new_street_provider_references": sum(
            len(street["ways"]) for street in accepted if not street["existing"]
        ),
        "additional_existing_street_references": sum(
            len([way for way in street["ways"] if way["id"] not in existing_by_id])
            for street in accepted
            if street["existing"]
        ),
        "decisions": dict(sorted(decisions.items())),
        "selection_rules": {
            "connectivity": "exact shared OSM geometry vertex within one normalized name",
            "allowed_highways": sorted(ALLOWED_HIGHWAYS),
            "length_metres": [300, 10000],
            "ambiguous_names": "exclude every disconnected component unless one contains the 0005 seeded way; only that connected seeded component receives references",
            "representative_point": "distance midpoint of the longest way in the connected component",
        },
    }
    (args.output_dir / "street-expansion-summary.json").write_text(
        json.dumps(summary, indent=2) + "\n", encoding="utf-8"
    )
    print(json.dumps(summary, indent=2))


if __name__ == "__main__":
    main()
