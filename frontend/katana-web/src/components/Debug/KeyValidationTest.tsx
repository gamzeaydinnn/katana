/**
 * Key Validation Test Component
 * Demonstrates and tests the key validation utilities
 * 
 * Usage: Remove/comment out in production
 * This component should only be used in development for testing
 */

import {
    Alert,
    Box,
    Button,
    Card,
    CardContent,
    Paper,
    Stack,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    Typography,
} from '@mui/material';
import React, { useState } from 'react';
import { createKeyGenerator, validateKeys } from '../../utils/keyValidator';

interface TestItem {
  id: string;
  name: string;
  value: number;
}

/**
 * Test component demonstrating key validation
 * Shows how duplicate keys are detected and logged
 */
export const KeyValidationTest: React.FC = () => {
  // Test data with duplicates
  const [testItems, setTestItems] = useState<TestItem[]>([
    { id: '1', name: 'Item A', value: 100 },
    { id: '2', name: 'Item B', value: 200 },
    { id: '1', name: 'Item C (DUPLICATE)', value: 300 }, // Duplicate ID
    { id: '3', name: 'Item D', value: 400 },
    { id: '2', name: 'Item E (DUPLICATE)', value: 500 }, // Another duplicate
  ]);

  // Test with validateKeys utility
  const validationResult = validateKeys<TestItem>(
    testItems,
    (item: TestItem) => item.id,
    'KeyValidationTest'
  );

  // Test with createKeyGenerator
  const getKey = createKeyGenerator('KeyValidationTest');

  const handleAddValidItem = () => {
    const newId = `${Date.now()}`;
    setTestItems([
      ...testItems,
      { id: newId, name: `Item ${newId}`, value: Math.random() * 1000 },
    ]);
  };

  const handleAddDuplicate = () => {
    setTestItems([
      ...testItems,
      { id: '1', name: 'Another Duplicate', value: 999 },
    ]);
  };

  const handleClear = () => {
    setTestItems([
      { id: '1', name: 'Item A', value: 100 },
      { id: '2', name: 'Item B', value: 200 },
      { id: '3', name: 'Item C', value: 300 },
    ]);
  };

  return (
    <Box sx={{ p: 3 }}>
      <Card>
        <CardContent>
          <Typography variant="h5" gutterBottom>
            🧪 React Key Validation Test Component
          </Typography>
          <Typography variant="body2" color="textSecondary" paragraph>
            This component demonstrates the key validation utilities.
            Check the browser console for validation warnings.
          </Typography>

          {/* Status Section */}
          <Stack spacing={2} sx={{ mb: 3 }}>
            {validationResult.hasDuplicates && (
              <Alert severity="warning">
                ⚠️ Duplicate keys detected: {validationResult.duplicateKeys?.join(', ')}
              </Alert>
            )}
            {!validationResult.hasDuplicates && (
              <Alert severity="success">
                ✅ All keys are unique!
              </Alert>
            )}
          </Stack>

          {/* Statistics */}
          <Stack direction="row" spacing={2} sx={{ mb: 3 }}>
            <Box sx={{ p: 2, bgcolor: '#f5f5f5', borderRadius: 1 }}>
              <Typography variant="body2" color="textSecondary">
                Total Items
              </Typography>
              <Typography variant="h6">{testItems.length}</Typography>
            </Box>
            <Box sx={{ p: 2, bgcolor: '#f5f5f5', borderRadius: 1 }}>
              <Typography variant="body2" color="textSecondary">
                Duplicate Keys
              </Typography>
              <Typography variant="h6" color={validationResult.hasDuplicates ? 'error' : 'success'}>
                {validationResult.duplicateKeys?.length || 0}
              </Typography>
            </Box>
            <Box sx={{ p: 2, bgcolor: '#f5f5f5', borderRadius: 1 }}>
              <Typography variant="body2" color="textSecondary">
                Validation Status
              </Typography>
              <Typography variant="h6" color={validationResult.hasDuplicates ? 'error' : 'success'}>
                {validationResult.hasDuplicates ? 'Has Duplicates' : 'Valid'}
              </Typography>
            </Box>
          </Stack>

          {/* Action Buttons */}
          <Stack direction="row" spacing={2} sx={{ mb: 3 }}>
            <Button variant="contained" onClick={handleAddValidItem}>
              Add Valid Item
            </Button>
            <Button variant="contained" color="warning" onClick={handleAddDuplicate}>
              Add Duplicate ID '1'
            </Button>
            <Button variant="outlined" onClick={handleClear}>
              Reset
            </Button>
          </Stack>

          {/* Table with Validation */}
          <TableContainer component={Paper}>
            <Table>
              <TableHead>
                <TableRow sx={{ backgroundColor: '#f5f5f5' }}>
                  <TableCell>ID</TableCell>
                  <TableCell>Name</TableCell>
                  <TableCell align="right">Value</TableCell>
                  <TableCell align="center">Generated Key</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {testItems.map((item, idx) => {
                  const isDuplicate =
                    validationResult.duplicateKeys?.includes(item.id);

                  return (
                    <TableRow
                      key={`${item.id}-${idx}`}
                      sx={{
                        backgroundColor: isDuplicate ? '#fff3cd' : 'inherit',
                        '&:hover': { backgroundColor: '#f9f9f9' },
                      }}
                    >
                      <TableCell>
                        {item.id}
                        {isDuplicate && (
                          <Typography
                            component="span"
                            variant="caption"
                            color="error"
                            sx={{ ml: 1 }}
                          >
                            (DUPLICATE)
                          </Typography>
                        )}
                      </TableCell>
                      <TableCell>{item.name}</TableCell>
                      <TableCell align="right">{item.value}</TableCell>
                      <TableCell align="center">
                        <code style={{ fontSize: '0.75rem' }}>
                          {getKey(item.id, idx)}
                        </code>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </TableContainer>

          {/* Console Output Info */}
          <Box sx={{ mt: 3, p: 2, bgcolor: '#f5f5f5', borderRadius: 1 }}>
            <Typography variant="body2" color="textSecondary" gutterBottom>
              💡 Tips:
            </Typography>
            <ul style={{ margin: '0.5rem 0', paddingLeft: '1.5rem' }}>
              <li>Open browser console (F12) to see validation warnings</li>
              <li>Click "Add Duplicate ID '1'" to trigger validation warnings</li>
              <li>Validation only runs in development mode</li>
              <li>Each item must have a unique key for React to work correctly</li>
            </ul>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
};

export default KeyValidationTest;
