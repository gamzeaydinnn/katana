/**
 * useListKeyValidation Hook
 * React hook for validating list keys in components
 * Automatically validates keys in development mode
 */

import { useEffect, useMemo } from 'react';
import { validateKeys, ValidationResult } from '../utils/keyValidator';

interface UseListKeyValidationOptions {
  componentName?: string;
  logWarnings?: boolean;
}

/**
 * Hook for validating keys in list components
 * 
 * @example
 * const { items: validatedItems, hasDuplicates } = useListKeyValidation(
 *   items,
 *   item => item.id,
 *   { componentName: 'ProductList' }
 * );
 * 
 * return (
 *   <ul>
 *     {validatedItems.map(item => (
 *       <li key={item.id}>{item.name}</li>
 *     ))}
 *   </ul>
 * );
 */
export function useListKeyValidation<T>(
  items: T[],
  keyExtractor: (item: T, index: number) => string | number,
  options?: UseListKeyValidationOptions
): ValidationResult<T> {
  const { componentName, logWarnings = true } = options || {};

  // Validate keys using useMemo to avoid re-validation on every render
  const result = useMemo(() => {
    return validateKeys(items, keyExtractor, componentName);
  }, [items, keyExtractor, componentName]);

  // Log warnings if specified
  useEffect(() => {
    if (logWarnings && result.hasDuplicates && process.env.NODE_ENV === 'development') {
      console.warn(
        `[useListKeyValidation] Component "${componentName}" has duplicate keys:`,
        result.duplicateKeys
      );
    }
  }, [result, logWarnings, componentName]);

  return result;
}

/**
 * Hook for validating multiple lists in a single component
 * 
 * @example
 * const results = useMultipleListsValidation(
 *   { products, categories },
 *   {
 *     products: item => item.id,
 *     categories: item => item.code
 *   },
 *   { componentName: 'Dashboard' }
 * );
 */
export function useMultipleListsValidation<T extends Record<string, any[]>>(
  itemsMap: T,
  extractorsMap: Record<keyof T, (item: any, index: number) => string | number>,
  options?: UseListKeyValidationOptions
) {
  const { componentName, logWarnings = true } = options || {};

  const results = useMemo(() => {
    const validated: Record<string, ValidationResult<any>> = {};

    Object.keys(itemsMap).forEach((key) => {
      const items = itemsMap[key as keyof T];
      const extractor = extractorsMap[key as keyof T];

      if (items && Array.isArray(items) && extractor) {
        validated[key] = validateKeys(items, extractor, `${componentName}.${key}`);
      }
    });

    return validated;
  }, [itemsMap, extractorsMap, componentName]);

  useEffect(() => {
    if (!logWarnings) return;

    Object.entries(results).forEach(([key, result]) => {
      if (result.hasDuplicates && process.env.NODE_ENV === 'development') {
        console.warn(
          `[useMultipleListsValidation] Component "${componentName}.${key}" has duplicate keys:`,
          result.duplicateKeys
        );
      }
    });
  }, [results, logWarnings, componentName]);

  return results;
}

export default useListKeyValidation;
