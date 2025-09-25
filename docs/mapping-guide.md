# Data Mapping Guide

## Overview

This document describes how data is mapped between Katana and Luca systems.

## Katana to Luca Mapping

### Product Mapping

| Katana Field | Luca Field  | Type    | Notes                       |
| ------------ | ----------- | ------- | --------------------------- |
| sku          | itemCode    | string  | Primary identifier          |
| name         | description | string  | Product description         |
| price        | unitPrice   | decimal | Unit price in base currency |
| categoryId   | accountCode | string  | Mapped via MappingTable     |

### Stock Mapping

| Katana Field | Luca Field    | Type     | Notes                 |
| ------------ | ------------- | -------- | --------------------- |
| sku          | itemCode      | string   | Product identifier    |
| quantity     | stockLevel    | int      | Current stock level   |
| location     | warehouseCode | string   | Storage location      |
| lastUpdated  | syncDate      | datetime | Last update timestamp |

### Invoice Mapping

| Katana Field  | Luca Field   | Type     | Notes               |
| ------------- | ------------ | -------- | ------------------- |
| invoiceNumber | documentNo   | string   | Invoice number      |
| customerId    | customerCode | string   | Customer identifier |
| totalAmount   | netAmount    | decimal  | Invoice total       |
| vatAmount     | taxAmount    | decimal  | Tax amount          |
| invoiceDate   | documentDate | datetime | Invoice date        |

### Customer Mapping

| Katana Field | Luca Field   | Type   | Notes                 |
| ------------ | ------------ | ------ | --------------------- |
| customerId   | customerCode | string | Customer identifier   |
| companyName  | customerName | string | Company name          |
| taxNumber    | taxId        | string | Tax identification    |
| email        | contactEmail | string | Primary contact email |
| phone        | contactPhone | string | Primary contact phone |

## Mapping Tables

### Category to Account Code Mapping

```csv
CategoryId,AccountCode,Description
1,600.10,Raw Materials
2,600.20,Finished Goods
3,600.30,Packaging Materials
4,600.40,Consumables
5,600.50,Electronic Components
```

### Location to Warehouse Mapping

```csv
KatanaLocation,LucaWarehouseCode,Description
MAIN,WH001,Main Warehouse
PROD,WH002,Production Floor
QC,WH003,Quality Control
SHIP,WH004,Shipping Area
```

### Currency Mapping

```csv
KatanaCurrency,LucaCurrencyCode,ExchangeRate
USD,USD,1.0000
EUR,EUR,0.8500
GBP,GBP,0.7300
TRY,TRY,18.5000
```

## Data Transformation Rules

### 1. SKU Normalization

- Remove special characters except hyphens and underscores
- Convert to uppercase
- Maximum length: 50 characters

### 2. Price Conversion

- Always convert to base currency (USD)
- Apply current exchange rates
- Round to 2 decimal places

### 3. Date Formatting

- All dates in UTC format
- ISO 8601 standard: `YYYY-MM-DDTHH:mm:ssZ`

### 4. Text Field Handling

- Trim whitespace
- Maximum lengths as per Luca requirements
- Encode special characters properly

## Error Handling

### Invalid Mappings

When a mapping is not found:

1. Log the error with details
2. Mark record as failed
3. Continue processing other records
4. Generate report for manual review

### Data Validation Errors

Common validation issues:

- Missing required fields
- Invalid data formats
- Constraint violations
- Foreign key mismatches

## Mapping Maintenance

### Adding New Mappings

1. Update the MappingTable in database
2. Verify mapping logic in MappingHelper
3. Test with sample data
4. Deploy and monitor

### Updating Existing Mappings

1. Create backup of current mappings
2. Update mapping entries
3. Run validation checks
4. Update affected records if needed

## Best Practices

1. **Always validate mappings** before processing large batches
2. **Use staging environment** for testing new mappings
3. **Monitor failed records** regularly
4. **Keep mapping history** for audit purposes
5. **Document custom mappings** for future reference
