import csv
p='src/Katana.API/logs/luca_parsed_report.csv'
count=0
num=0
rows=[]
with open(p, encoding='utf-8') as f:
    r=csv.DictReader(f)
    for row in r:
        count+=1
        v=row.get('IsNumericOnly','').strip().lower()
        if v in ('y','true','1'):
            num+=1
            if len(rows)<10:
                rows.append(row)
print(f'Total:{count} NumericOnly:{num} NonNumeric:{count-num}')
print('\nSample numeric-only rows (up to 10):')
for r in rows:
    print(r)
