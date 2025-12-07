/**
 * Key Validation Integration Examples
 * Demonstrates how to use the key validation utilities in your components
 */

import { Alert, TableBody, TableCell, TableRow } from '@mui/material';
import { useListKeyValidation, useMultipleListsValidation } from './hooks/useListKeyValidation';
import { createKeyGenerator, validateKeys, validateKeysStrict } from './utils/keyValidator';

// ============================================================================
// EXAMPLE 1: Using validateKeys utility function directly
// ============================================================================

interface Product {
  id: string;
  name: string;
  price: number;
}

function ProductListExample({ products }: { products: Product[] }) {
  // Validate keys once when component renders
  const { items: validatedProducts, hasDuplicates } = validateKeys(
    products,
    (product) => product.id,
    'ProductListExample'
  );

  if (hasDuplicates) {
    console.warn('Product list has duplicate IDs!');
  }

  return (
    <ul>
      {validatedProducts.map((product) => (
        <li key={product.id}>
          {product.name} - ${product.price}
        </li>
      ))}
    </ul>
  );
}

// ============================================================================
// EXAMPLE 2: Using useListKeyValidation hook
// ============================================================================

interface Order {
  id: string;
  orderNo: string;
  total: number;
}

function OrderListComponent({ orders }: { orders: Order[] }) {
  // Hook automatically validates keys in development mode
  const { items: validatedOrders, hasDuplicates, duplicateKeys } = useListKeyValidation<Order>(
    orders,
    (order: Order) => order.id,
    { componentName: 'OrderListComponent', logWarnings: true }
  );

  if (hasDuplicates) {
    return (
      <div style={{ color: 'red' }}>
        Error: Duplicate order IDs: {duplicateKeys?.join(', ')}
      </div>
    );
  }

  return (
    <table>
      <tbody>
        {validatedOrders.map((order) => (
          <tr key={order.id}>
            <td>{order.orderNo}</td>
            <td>{order.total}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

// ============================================================================
// EXAMPLE 3: Using createKeyGenerator for inline key creation
// ============================================================================

interface DynamicItem {
  code: string;
  name: string;
}

function DynamicListComponent({ items }: { items: DynamicItem[] }) {
  const getKey = createKeyGenerator('DynamicListComponent');

  return (
    <div>
      {items.map((item: DynamicItem, idx: number) => (
        <div key={getKey(item.code, idx)}>
          {item.name}
        </div>
      ))}
    </div>
  );
}

// ============================================================================
// EXAMPLE 4: Strict validation (throws error on duplicates)
// ============================================================================

interface CriticalItem {
  uniqueId: string;
  data: string;
}

function CriticalDataList({ criticalItems }: { criticalItems: CriticalItem[] }) {
  try {
    // This will throw an error if duplicates are found
    const validated = validateKeysStrict<CriticalItem>(
      criticalItems,
      (item: CriticalItem) => item.uniqueId,
      'CriticalDataList'
    );

    return (
      <ul>
        {validated.map((item) => (
          <li key={item.uniqueId}>{item.data}</li>
        ))}
      </ul>
    );
  } catch (error: any) {
    return <div>Error: {error.message}</div>;
  }
}

// ============================================================================
// EXAMPLE 5: Validating multiple lists in one component
// ============================================================================

interface Category {
  code: string;
  name: string;
}

interface Supplier {
  vendorId: string;
  name: string;
}

function DashboardComponent({ products, categories, suppliers }: {
  products: Product[];
  categories: Category[];
  suppliers: Supplier[];
}) {
  const results = useMultipleListsValidation(
    { products, categories, suppliers },
    {
      products: (p: Product) => p.id,
      categories: (c: Category) => c.code,
      suppliers: (s: Supplier) => s.vendorId,
    },
    { componentName: 'DashboardComponent', logWarnings: true }
  );

  // Check individual list validations
  const productsHaveDuplicates = results.products.hasDuplicates;

  return (
    <div>
      {productsHaveDuplicates && (
        <Alert severity="warning">Warning: Product list has duplicate keys</Alert>
      )}
      {/* Render lists... */}
    </div>
  );
}

// ============================================================================
// EXAMPLE 6: Integration with existing maps (updated versions)
// ============================================================================

// BEFORE (using index as key - problematic)
export function OrdersTableBefore({ orders }: { orders: Order[] }) {
  return (
    <TableBody>
      {orders.map((order, idx) => (
        <TableRow key={idx}>  {/* ❌ Wrong: using index */}
          <TableCell>{order.orderNo}</TableCell>
        </TableRow>
      ))}
    </TableBody>
  );
}

// AFTER (using validated unique key)
export function OrdersTableAfter({ orders }: { orders: Order[] }) {
  const { items: validatedOrders } = useListKeyValidation<Order>(
    orders,
    (order: Order) => order.id,
    { componentName: 'OrdersTable' }
  );

  return (
    <TableBody>
      {validatedOrders.map((order) => (
        <TableRow key={order.id}>  {/* ✅ Correct: unique identifier */}
          <TableCell>{order.orderNo}</TableCell>
        </TableRow>
      ))}
    </TableBody>
  );
}

// ============================================================================
// DEVELOPMENT MODE ONLY BEHAVIOR
// ============================================================================

/**
 * Key validation utilities ONLY perform checks in development mode:
 * - Development (process.env.NODE_ENV === 'development'): Full validation
 * - Production: No validation, zero performance overhead
 * 
 * This means:
 * ✅ Safe to add to all components without affecting production
 * ✅ Zero performance cost in production builds
 * ✅ Helps catch bugs during development
 * ✅ No runtime overhead for users
 */

// ============================================================================
// CONSOLE OUTPUT EXAMPLES
// ============================================================================

/**
 * When duplicate keys are detected, you'll see:
 *
 * [React Key Validation] Duplicate keys detected in <ProductList>:
 *   Duplicate keys: '1', '5'
 *   Appearing at: index 2, index 5
 *   This may cause React to skip re-renders and lose component state.
 *   Each item in a list must have a unique key.
 */

const KeyValidationExamples = {};

export default KeyValidationExamples;
