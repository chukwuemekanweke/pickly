# Enugu OSM inventory — 2026-10-20

This directory preserves the raw Overpass responses and the derived CSV inventory used to review the incremental
Enugu location registry expansion in `0008.enugu-locations-expansion.postdeploy.sql`.

## Contents

- `raw/*.json`: one complete Overpass response for the streets or landmarks query in each of Enugu North,
  Enugu South and Enugu East.
- `raw-geometry/streets-*.json`: batched full geometry for the exact 3,248 street ways. Most responses are current
  to 2026-10-20; `streets-15-fallback.json` is the preserved 2026-07-24 response used for 150 ways after current
  public Overpass replicas repeatedly timed out. Current split responses supersede it for 50 other ways.
- `enugu-all-osm.csv`: all distinct named source features after combining the six responses.
- `enugu-streets.csv`: raw named highway ways; repeated ways can be segments of one physical street.
- `enugu-landmarks-and-areas.csv`: named POIs and areas; areas are not selectable delivery locations.
- `enugu-new-candidates.csv`: features absent by OSM reference and normalized seed name.
- `enugu-needs-review.csv`: duplicate-name, access and possible-name-match review queue.
- `pickly-current-seed.csv`: the 173-row `0005` seed snapshot used by the comparison.
- `summary.json`: extraction counts and caveats.
- `street-geometry-review.csv`: one row per connected same-name component, with its length, contributing OSM ways,
  inclusion/exclusion decision and reason.
- `street-expansion-summary.json`: deterministic street review counts and rules.
- `street-values.generated.sql`: generated SQL value rows consumed by post-deploy `0008`; retained for audit.

`../build_enugu_street_expansion.py` reproduces the three street review artifacts from the saved geometry without
network access. It groups ways only when they share an exact geometry vertex, excludes disconnected duplicate names,
and selects the distance midpoint of the longest contributing way as the canonical representative point.

The `osm3s.timestamp_osm_base` values in the raw responses range from `2026-10-20T13:34:39Z` to
`2026-10-20T13:39:51Z`. The inventory contains 4,537 distinct features. It is reference material, not an approved
bulk import: OSM can be incomplete or inaccurate, repeated road ways require connectivity review, and way/area
centres are not necessarily navigable pickup points.

Source: OpenStreetMap contributors, licensed under ODbL 1.0. Preserve attribution and review the derived-database
obligations before redistributing this dataset.
