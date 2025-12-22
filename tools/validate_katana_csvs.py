#!/usr/bin/env python3
from __future__ import annotations

import argparse
import csv
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable, Optional


def _try_read_text(path: Path, encodings: Iterable[str]) -> tuple[str, str]:
    last_err: Optional[Exception] = None
    for enc in encodings:
        try:
            return path.read_text(encoding=enc), enc
        except Exception as e:  # noqa: BLE001
            last_err = e
    raise RuntimeError(f"Failed to read {path} with known encodings: {last_err}")


def _sniff_dialect(sample: str) -> csv.Dialect:
    try:
        return csv.Sniffer().sniff(sample, delimiters=[",", ";", "\t", "|"])
    except Exception:  # noqa: BLE001
        # Fallback: comma
        class _Comma(csv.Dialect):
            delimiter = ","
            quotechar = '"'
            doublequote = True
            skipinitialspace = True
            lineterminator = "\n"
            quoting = csv.QUOTE_MINIMAL

        return _Comma()


def _norm_header(value: str) -> str:
    return " ".join((value or "").strip().lower().split())


def _norm_sku(value: str) -> str:
    # Katana SKU comparisons should be stable even if input has whitespace / casing differences.
    return " ".join((value or "").strip().upper().split())


def _parse_decimal(value: str) -> Optional[float]:
    if value is None:
        return None
    raw = str(value).strip()
    if raw == "":
        return None
    # Support TR decimal comma
    raw = raw.replace(" ", "")
    if raw.count(",") == 1 and raw.count(".") == 0:
        raw = raw.replace(",", ".")
    elif raw.count(",") >= 1 and raw.count(".") >= 1:
        # Likely thousands separator, keep last decimal separator.
        raw = raw.replace(".", "").replace(",", ".")
    try:
        return float(raw)
    except ValueError:
        return None


def _pick_column(fieldnames: list[str], candidates: list[str]) -> Optional[str]:
    normalized = {_norm_header(fn): fn for fn in fieldnames if fn is not None}
    for cand in candidates:
        key = _norm_header(cand)
        if key in normalized:
            return normalized[key]
    return None


@dataclass(frozen=True)
class ProductRow:
    sku: str
    name: str


@dataclass(frozen=True)
class RecipeRow:
    parent_sku: str
    component_sku: str
    qty: float


def read_csv_rows(path: Path) -> tuple[list[dict[str, str]], list[str], str, str]:
    text, encoding = _try_read_text(path, ["utf-8-sig", "utf-8", "cp1254", "iso-8859-9"])
    sample = "\n".join(text.splitlines()[:50])
    dialect = _sniff_dialect(sample)
    reader = csv.DictReader(text.splitlines(), dialect=dialect)
    if reader.fieldnames is None:
        raise RuntimeError(f"{path} appears to have no header row.")
    rows = []
    for row in reader:
        # DictReader may return None keys if row length mismatches; ignore them.
        clean = {k: (v if v is not None else "") for k, v in row.items() if k is not None}
        rows.append(clean)
    return rows, list(reader.fieldnames), encoding, dialect.delimiter


def validate_products(path: Path) -> tuple[list[ProductRow], list[str]]:
    rows, fieldnames, encoding, delim = read_csv_rows(path)
    sku_col = _pick_column(
        fieldnames,
        [
            "sku",
            "item sku",
            "item_code",
            "item code",
            "product sku",
            "product_code",
            "product code",
            "stok_kodu",
            "stok kodu",
            "kart_kodu",
            "kart kodu",
            "code",
        ],
    )
    name_col = _pick_column(
        fieldnames,
        [
            "name",
            "item name",
            "item_name",
            "product name",
            "product_name",
            "stok_adi",
            "stok adi",
            "kart_adi",
            "kart adi",
            "description",
        ],
    )

    issues: list[str] = []
    if not sku_col:
        issues.append(f"[products] SKU column not found. Headers: {fieldnames}")
        return [], issues
    if not name_col:
        issues.append(f"[products] Name column not found. Headers: {fieldnames}")
        return [], issues

    parsed: list[ProductRow] = []
    seen: dict[str, int] = {}

    for i, r in enumerate(rows, start=2):
        sku_raw = r.get(sku_col, "")
        sku = _norm_sku(sku_raw)
        name = (r.get(name_col, "") or "").strip()

        if not sku:
            issues.append(f"[products] Line {i}: empty SKU")
            continue
        if not name:
            issues.append(f"[products] Line {i}: empty Name for SKU={sku}")
        if sku in seen:
            issues.append(f"[products] Line {i}: duplicate SKU={sku} (first at line {seen[sku]})")
        else:
            seen[sku] = i
        parsed.append(ProductRow(sku=sku, name=name))

    issues.insert(0, f"[products] Read {len(rows)} rows (encoding={encoding}, delimiter='{delim}')")
    return parsed, issues


def validate_recipes(path: Path) -> tuple[list[RecipeRow], list[str]]:
    rows, fieldnames, encoding, delim = read_csv_rows(path)
    parent_col = _pick_column(
        fieldnames,
        [
            "parent_sku",
            "product_sku",
            "product sku",
            "recipe_sku",
            "recipe sku",
            "sku",
            "product",
            "mamül",
            "mamul",
        ],
    )
    component_col = _pick_column(
        fieldnames,
        [
            "component_sku",
            "ingredient_sku",
            "material_sku",
            "child_sku",
            "component sku",
            "material sku",
            "ingredient sku",
            "bom_sku",
            "bom sku",
            "hammadde",
            "malzeme",
        ],
    )
    qty_col = _pick_column(
        fieldnames,
        [
            "qty",
            "quantity",
            "amount",
            "miktar",
            "quantity per",
            "component_qty",
            "component qty",
        ],
    )

    issues: list[str] = []
    if not parent_col:
        issues.append(f"[recipes] Parent SKU column not found. Headers: {fieldnames}")
        return [], issues
    if not component_col:
        issues.append(f"[recipes] Component SKU column not found. Headers: {fieldnames}")
        return [], issues
    if not qty_col:
        issues.append(f"[recipes] Quantity column not found. Headers: {fieldnames}")
        return [], issues

    parsed: list[RecipeRow] = []
    for i, r in enumerate(rows, start=2):
        parent = _norm_sku(r.get(parent_col, ""))
        component = _norm_sku(r.get(component_col, ""))
        qty_raw = r.get(qty_col, "")
        qty = _parse_decimal(qty_raw)

        if not parent:
            issues.append(f"[recipes] Line {i}: empty parent SKU")
            continue
        if not component:
            issues.append(f"[recipes] Line {i}: empty component SKU (parent={parent})")
            continue
        if parent == component:
            issues.append(f"[recipes] Line {i}: parent SKU equals component SKU ({parent})")

        if qty is None:
            issues.append(f"[recipes] Line {i}: invalid qty '{qty_raw}' (parent={parent}, component={component})")
            continue
        if qty <= 0:
            issues.append(f"[recipes] Line {i}: non-positive qty {qty} (parent={parent}, component={component})")

        parsed.append(RecipeRow(parent_sku=parent, component_sku=component, qty=qty))

    issues.insert(0, f"[recipes] Read {len(rows)} rows (encoding={encoding}, delimiter='{delim}')")
    return parsed, issues


def main() -> int:
    ap = argparse.ArgumentParser(description="Validate Katana split CSVs: Items (products) + BOM (recipes).")
    ap.add_argument("products_csv", type=Path, help="cleaned_products_list.csv (or equivalent)")
    ap.add_argument("recipes_csv", type=Path, help="cleaned_recipes_list.csv (or equivalent)")
    args = ap.parse_args()

    products, prod_issues = validate_products(args.products_csv)
    recipes, rec_issues = validate_recipes(args.recipes_csv)

    product_skus = {p.sku for p in products if p.sku}

    missing_parents = sorted({r.parent_sku for r in recipes} - product_skus)
    missing_components = sorted({r.component_sku for r in recipes} - product_skus)

    print("\n".join(prod_issues[:25]))
    if len(prod_issues) > 25:
        print(f"[products] (+{len(prod_issues) - 25} more issues)")

    print()
    print("\n".join(rec_issues[:25]))
    if len(rec_issues) > 25:
        print(f"[recipes] (+{len(rec_issues) - 25} more issues)")

    print()
    print(f"[cross] Unique products: {len(product_skus)}")
    print(f"[cross] Recipes rows: {len(recipes)}")
    print(f"[cross] Parents missing from products: {len(missing_parents)}")
    if missing_parents[:10]:
        print("[cross] Sample missing parents: " + ", ".join(missing_parents[:10]))
    print(f"[cross] Components missing from products: {len(missing_components)}")
    if missing_components[:10]:
        print("[cross] Sample missing components: " + ", ".join(missing_components[:10]))

    # Non-zero exit if critical issues detected
    critical = any("column not found" in s for s in prod_issues + rec_issues)
    return 2 if critical else 0


if __name__ == "__main__":
    raise SystemExit(main())

