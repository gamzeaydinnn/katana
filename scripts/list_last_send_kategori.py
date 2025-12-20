import re
from pathlib import Path
p=Path('src/Katana.API/logs/luca-raw.log')
text=p.read_text(encoding='utf-8',errors='replace')
parts=[p.strip() for p in re.split(r"\n-+\n", text) if p.strip()]
rows=[]
for part in parts:
    if 'SEND_STOCK_CARD' not in part and 'SEND_STOCK_CARDS' not in part:
        continue
    # find Request JSON
    m=re.search(r"Request:\s*(\[|\{)", part)
    if not m:
        continue
    start=m.start(1)
    resp_idx=part.find('ResponseStatus:')
    if resp_idx==-1:
        resp_idx=part.find('Response:')
    req_text=part[start:resp_idx].strip()
    try:
        import json
        req_json=json.loads(req_text)
    except Exception:
        try:
            first = min((req_text.find('[') if '[' in req_text else 1e9),(req_text.find('{') if '{' in req_text else 1e9))
            req_json=json.loads(req_text[first:])
        except Exception:
            continue
    items=req_json if isinstance(req_json,list) else [req_json]
    for it in items:
        kart=it.get('kartKodu') or it.get('kartKod') or ''
        kategori=it.get('kategoriAgacKod') or ''
        rows.append((kart,kategori))

for kart,kategori in rows[-50:]:
    print(f"{kart} -> {kategori}")
print(f"Total send entries parsed: {len(rows)}")
