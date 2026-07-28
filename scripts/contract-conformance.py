#!/usr/bin/env python3
"""Contract conformance check (spec R9).

Compares the operations documented in contracts/responsabilimano-api.yaml against
the routes actually mapped in src/ResponsabiliMano.Web/Endpoints/*.cs. Fails if the
two drift apart, so the OpenAPI baseline stays honest as endpoints change.

Dependency-free (no PyYAML): both sides are parsed with regex, which is enough for
this repo's flat path list and MapGroup/MapVerb style.
"""
from __future__ import annotations

import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
CONTRACT = ROOT / "contracts" / "responsabilimano-api.yaml"
ENDPOINTS_DIR = ROOT / "src" / "ResponsabiliMano.Web" / "Endpoints"

# Verbs that carry no request/response body in this API and are documented as 302.
METHOD_RE = re.compile(r"Map(Get|Post|Put|Delete|Patch)\(")
GROUP_RE = re.compile(r'MapGroup\("([^"]*)"\)')
MAP_CALL_RE = re.compile(r'Map(Get|Post|Put|Delete|Patch)\(\s*(?:\(Delegate\)\s*)?"([^"]*)"')


def normalize(path: str) -> str:
    """Strip route constraints and collapse double/trailing slashes."""
    path = re.sub(r"\{(\w+):[^}]+\}", r"{\1}", path)  # {id:guid} -> {id}
    path = re.sub(r"//+", "/", path)
    if len(path) > 1 and path.endswith("/"):
        path = path[:-1]
    return path


def routes_from_code() -> set[tuple[str, str]]:
    found: set[tuple[str, str]] = set()
    for cs in sorted(ENDPOINTS_DIR.glob("*.cs")):
        text = cs.read_text(encoding="utf-8-sig")
        group_match = GROUP_RE.search(text)
        prefix = group_match.group(1) if group_match else ""
        for verb, sub in MAP_CALL_RE.findall(text):
            full = normalize(f"{prefix}/{sub}" if sub else prefix)
            found.add((verb.upper(), full))
    return found


def operations_from_contract() -> set[tuple[str, str]]:
    ops: set[tuple[str, str]] = set()
    in_paths = False
    current: str | None = None
    for raw in CONTRACT.read_text(encoding="utf-8").splitlines():
        if raw.startswith("paths:"):
            in_paths = True
            continue
        if not in_paths:
            continue
        if raw and not raw[0].isspace():  # left the paths block
            break
        path_match = re.match(r"^  (/\S*):\s*$", raw)
        if path_match:
            current = normalize(path_match.group(1))
            continue
        verb_match = re.match(r"^    (get|post|put|delete|patch):\s*$", raw)
        if verb_match and current:
            ops.add((verb_match.group(1).upper(), current))
    return ops


def main() -> int:
    if not CONTRACT.exists():
        print(f"::error::contract not found: {CONTRACT}")
        return 1

    code = routes_from_code()
    contract = operations_from_contract()

    missing_in_contract = sorted(code - contract)
    missing_in_code = sorted(contract - code)

    for verb, path in missing_in_contract:
        print(f"::error::route {verb} {path} is mapped but NOT documented in the contract")
    for verb, path in missing_in_code:
        print(f"::error::operation {verb} {path} is documented but NOT mapped in code")

    if missing_in_contract or missing_in_code:
        print(f"\nContract drift: {len(missing_in_contract)} undocumented, {len(missing_in_code)} unimplemented.")
        return 1

    print(f"Contract conformance OK — {len(contract)} operations match the mapped routes.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
