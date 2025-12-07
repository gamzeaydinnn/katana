/**
 * Key Validator Utility
 * Validates list keys in development mode to detect duplicates and improve React performance
 */

export interface ValidationResult<T> {
  items: T[];
  hasDuplicates: boolean;
  duplicateKeys?: string[];
}

/**
 * Validates keys in an array for duplicates
 * Only performs validation in development mode
 * 
 * @param items - Array of items to validate
 * @param keyExtractor - Function that extracts the key from each item
 * @param componentName - Optional component name for better logging
 * @returns ValidationResult with items and duplicate info
 * 
 * @example
 * const items = [
 *   { id: '1', name: 'Item 1' },
 *   { id: '2', name: 'Item 2' },
 *   { id: '1', name: 'Item 3' } // duplicate id
 * ];
 * 
 * const result = validateKeys(items, item => item.id, 'ProductList');
 * // In development, logs warning about duplicate key '1'
 */
export function validateKeys<T>(
  items: T[],
  keyExtractor: (item: T, index: number) => string | number,
  componentName?: string
): ValidationResult<T> {
  const result: ValidationResult<T> = {
    items,
    hasDuplicates: false,
    duplicateKeys: [],
  };

  // Only validate in development mode
  if (process.env.NODE_ENV !== "development") {
    return result;
  }

  // Track keys and their indices
  const keyMap = new Map<string | number, number[]>();
  const keys: (string | number)[] = [];

  // Extract all keys
  items.forEach((item, index) => {
    const key = keyExtractor(item, index);
    keys.push(key);

    if (!keyMap.has(key)) {
      keyMap.set(key, []);
    }
    keyMap.get(key)!.push(index);
  });

  // Find duplicates
  const duplicates: (string | number)[] = [];
  keyMap.forEach((indices, key) => {
    if (indices.length > 1) {
      duplicates.push(key);
      result.hasDuplicates = true;
    }
  });

  // Log warnings for duplicates
  if (duplicates.length > 0) {
    result.duplicateKeys = duplicates.map(String);
    
    const componentInfo = componentName ? ` in <${componentName}>` : "";
    const keysList = duplicates.map((k) => `'${k}'`).join(", ");
    const itemsInfo = duplicates
      .flatMap((key) => keyMap.get(key) || [])
      .map((idx) => `index ${idx}`)
      .join(", ");

    console.warn(
      `[React Key Validation] Duplicate key${duplicates.length > 1 ? "s" : ""} detected${componentInfo}:\n` +
        `  Duplicate keys: ${keysList}\n` +
        `  Appearing at: ${itemsInfo}\n` +
        `  This may cause React to skip re-renders and lose component state.\n` +
        `  Each item in a list must have a unique key.`
    );
  }

  return result;
}

/**
 * Strict validation that throws an error on duplicates
 * Useful for critical lists where duplicates are unacceptable
 * 
 * @throws Error if duplicate keys are found
 */
export function validateKeysStrict<T>(
  items: T[],
  keyExtractor: (item: T, index: number) => string | number,
  componentName?: string
): T[] {
  const result = validateKeys(items, keyExtractor, componentName);

  if (result.hasDuplicates) {
    const componentInfo = componentName ? ` in <${componentName}>` : "";
    throw new Error(
      `[React Key Validation] Duplicate keys found${componentInfo}: ${result.duplicateKeys?.join(", ")}`
    );
  }

  return result.items;
}

/**
 * Batch validation for multiple lists
 * Useful when validating multiple arrays in the same component
 * 
 * @example
 * const results = validateMultipleKeys(
 *   { products: productList, categories: categoryList },
 *   {
 *     products: (item) => item.id,
 *     categories: (item) => item.code
 *   },
 *   'DashboardComponent'
 * );
 */
export function validateMultipleKeys<T extends Record<string, any[]>>(
  itemsMap: T,
  extractorsMap: Record<keyof T, (item: any, index: number) => string | number>,
  componentName?: string
): Record<keyof T, ValidationResult<any>> {
  const results: Record<string, ValidationResult<any>> = {};

  Object.keys(itemsMap).forEach((key) => {
    const items = itemsMap[key as keyof T];
    const extractor = extractorsMap[key as keyof T];

    if (items && Array.isArray(items) && extractor) {
      results[key] = validateKeys(items, extractor, `${componentName}.${key}`);
    }
  });

  return results as Record<keyof T, ValidationResult<any>>;
}

/**
 * Creates a safe key generator for use with array.map()
 * Validates keys at map time and logs warnings
 * 
 * @example
 * const getKey = createKeyGenerator(componentName);
 * 
 * items.map((item, idx) => (
 *   <div key={getKey(item.id, idx)}>
 *     {item.name}
 *   </div>
 * ))
 */
export function createKeyGenerator(componentName?: string) {
  const seenKeys = new Set<string>();

  return (key: string | number, index: number): string => {
    const keyStr = String(key);

    if (process.env.NODE_ENV === "development" && seenKeys.has(keyStr)) {
      const componentInfo = componentName ? ` in <${componentName}>` : "";
      console.warn(
        `[React Key Validation] Duplicate key '${keyStr}' detected${componentInfo} ` +
          `at index ${index}. This may cause React to skip re-renders.`
      );
    }

    seenKeys.add(keyStr);
    return keyStr;
  };
}

export default validateKeys;
