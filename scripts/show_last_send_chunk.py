from pathlib import Path
p=Path('src/Katana.API/logs/luca-raw.log')
text=p.read_text(encoding='utf-8',errors='replace')
key='SEND_STOCK_CARD'
idx=text.rfind(key)
if idx==-1:
    print('no match')
else:
    sub=text[idx:idx+20000]
    # try to locate Request: ... ResponseStatus:
    rpos=sub.find('Request:')
    rspos=sub.find('ResponseStatus:')
    if rpos!=-1 and rspos!=-1:
        req=sub[rpos+len('Request:'):rspos].strip()
        print('----REQUEST----')
        print(req[:4000])
    else:
        print('No Request/Response markers found in last chunk')
    # also print a small suffix
    print('\n----SUFFIX----\n')
    print(sub[-1000:])
