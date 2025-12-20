import re
import json
import csv
from pathlib import Path


LOG_PATH = Path("src/Katana.API/logs/luca-raw.log")
OUT_CSV = Path("src/Katana.API/logs/luca_parsed_report.csv")
OUT_SUMMARY = Path("src/Katana.API/logs/luca_parsed_summary.txt")


def split_chunks(text):
    # Split on the ---- separator used in the log
    parts = [p.strip() for p in re.split(r"\n-+\n", text) if p.strip()]
    return parts


def extract_request_and_response(chunk):
    # Find request JSON after the first occurrence of 'Request:'
    req_match = re.search(r"Request:\s*(\[|\{)", chunk)
    if not req_match:
        return None

    start = req_match.start(1)
    # Find ResponseStatus marker to determine end of request JSON
    resp_status_idx = chunk.find("ResponseStatus:")
    if resp_status_idx == -1:
        # fallback: try to find 'Response:'
        resp_status_idx = chunk.find("Response:")
    request_json_text = chunk[start:resp_status_idx].strip()

    # Find response body after 'Response:' marker
    resp_idx = chunk.find("Response:")
    response_text = ""
    if resp_idx != -1:
        response_text = chunk[resp_idx + len("Response:"):].strip()

    # Trim any trailing '----' markers etc
    request_json_text = request_json_text.rstrip('-\n ')
    response_text = response_text.rstrip('-\n ')

    return request_json_text, response_text


def summarize_response(resp_text):
    if not resp_text:
        return ""
    trimmed = resp_text.strip()
    # If it's JSON, try to parse and summarize
    try:
        j = json.loads(trimmed)
        # For objects with 'code' or 'error' and 'message'
        if isinstance(j, dict):
            if 'code' in j and 'message' in j:
                return f"code={j.get('code')} message={j.get('message')}"
            if 'error' in j and 'message' in j:
                return f"error={j.get('error')} message={j.get('message')}"
        # Otherwise return compact json
        return json.dumps(j)[:200]
    except Exception:
        # Not JSON — return first meaningful line (strip HTML tags)
        one = trimmed.splitlines()[0]
        # Remove HTML tags for short summary
        no_tags = re.sub(r'<[^>]+>', '', one)
        return no_tags[:200]


def parse():
    if not LOG_PATH.exists():
        print(f"Log file not found: {LOG_PATH}")
        return 1

    text = LOG_PATH.read_text(encoding='utf-8', errors='replace')
    chunks = split_chunks(text)

    rows = []
    for c in chunks:
        # Process stock card and stock movement sends
        kind = None
        if 'SEND_STOCK_CARD' in c or 'SEND_STOCK_CARDS' in c:
            kind = 'STOCK_CARD'
        elif 'SEND_STOCK_MOVEMENT' in c or 'EkleStkWsDshBaslik' in c or 'OtherStockMovement' in c:
            kind = 'STOCK_MOVEMENT'
        else:
            continue

        extracted = extract_request_and_response(c)
        if not extracted:
            continue
        req_text, resp_text = extracted

        # request may be a JSON array (batch) or object
        try:
            req_json = json.loads(req_text)
        except Exception:
            # try to recover by trimming trailing commas or control chars
            try:
                # naive attempt: find first '{' or '[' and last matching brace
                first = min((req_text.find('[') if '[' in req_text else 1e9),
                            (req_text.find('{') if '{' in req_text else 1e9))
                req_json = json.loads(req_text[first:])
            except Exception:
                # give up on this chunk
                continue

        items = req_json if isinstance(req_json, list) else [req_json]
        resp_summary = summarize_response(resp_text)

        for it in items:
            # Identify possible SKU / product identifier fields
            kart = ''
            if isinstance(it, dict):
                for k in ('kartKodu', 'kartKod', 'productCode', 'productcode'):
                    if k in it and it.get(k):
                        kart = it.get(k)
                        break

            # Category field may be present for stock cards; movements may not include it
            kategori = ''
            if isinstance(it, dict):
                for ck in ('kategoriAgacKod', 'kategoriagacKod', 'kategoriagackod'):
                    if ck in it and it.get(ck) is not None:
                        kategori = it.get(ck) or ''
                        break

            # BelgeSeri if present
            belge = ''
            if isinstance(it, dict) and 'belgeSeri' in it and it.get('belgeSeri') is not None:
                belge = it.get('belgeSeri') or ''

            is_numeric = str(kategori).strip().isdigit()
            is_empty = (kategori is None) or (str(kategori).strip() == '')

            rows.append({
                'KartKodu': kart,
                'KategoriAgacKod': kategori,
                'IsNumericOnly': 'Y' if is_numeric else 'N',
                'IsEmptyKategori': 'Y' if is_empty else 'N',
                'BelgeSeri': belge,
                'RequestType': kind + (':BATCH' if isinstance(req_json, list) else ':SINGLE'),
                'ResponseSummary': resp_summary,
            })

    if not rows:
        print("No SEND_STOCK_CARD entries found in log.")
        return 0

    OUT_CSV.parent.mkdir(parents=True, exist_ok=True)
    with OUT_CSV.open('w', newline='', encoding='utf-8') as f:
        writer = csv.DictWriter(f, fieldnames=['KartKodu', 'KategoriAgacKod', 'IsNumericOnly', 'IsEmptyKategori', 'BelgeSeri', 'RequestType', 'ResponseSummary'])
        writer.writeheader()
        for r in rows:
            writer.writerow(r)

    # Build a simple summary
    total = len(rows)
    by_type = {}
    numeric_only = 0
    empty_kategori = 0
    resp_counts = {}
    for r in rows:
        typ = r.get('RequestType') or 'UNKNOWN'
        by_type[typ] = by_type.get(typ, 0) + 1
        if r.get('IsNumericOnly') == 'Y':
            numeric_only += 1
        if r.get('IsEmptyKategori') == 'Y':
            empty_kategori += 1
        resp = r.get('ResponseSummary') or ''
        resp_counts[resp] = resp_counts.get(resp, 0) + 1

    summary_lines = []
    summary_lines.append(f"TotalRows={total}")
    summary_lines.append("ByRequestType:")
    for k, v in sorted(by_type.items(), key=lambda x: x[0]):
        summary_lines.append(f"  {k}: {v}")
    summary_lines.append(f"NumericOnlyKategoriCount={numeric_only}")
    summary_lines.append(f"EmptyKategoriCount={empty_kategori}")
    summary_lines.append("ResponseSummaryCounts:")
    for k, v in sorted(resp_counts.items(), key=lambda x: -x[1]):
        display = k if k else '(empty)'
        # truncate long response keys for readability
        if len(display) > 200:
            display = display[:197] + '...'
        summary_lines.append(f"  {v}x {display}")

    try:
        OUT_SUMMARY.parent.mkdir(parents=True, exist_ok=True)
        with OUT_SUMMARY.open('w', encoding='utf-8') as sf:
            sf.write('\n'.join(summary_lines))
    except Exception:
        pass

    print(f"Wrote report to {OUT_CSV} ({len(rows)} rows)")
    print(f"Wrote summary to {OUT_SUMMARY}")
    return 0


if __name__ == '__main__':
    exit(parse())
