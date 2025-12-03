import { useEffect, useState } from 'react';
import api from '../services/api';

interface PurchaseOrder {
  id: number;
  orderNumber: string;
  supplierName: string;
  status: string;
  totalAmount: number;
  createdDate: string;
}

interface Pagination {
  currentPage: number;
  totalPages: number;
  totalCount: number;
  pageSize: number;
}

interface PurchaseOrdersResponse {
  items: PurchaseOrder[];
  pagination: Pagination;
}

export const usePurchaseOrders = () => {
  const [orders, setOrders] = useState<PurchaseOrder[]>([]);
  const [loading, setLoading] = useState(false);
  const [pagination, setPagination] = useState<Pagination>({
    currentPage: 1,
    totalPages: 0,
    totalCount: 0,
    pageSize: 20,
  });

  const loadOrders = async (page = 1, pageSize = 20) => {
    setLoading(true);
    try {
      const response = await api.get<PurchaseOrdersResponse>('/purchase-orders', {
        params: { page, pageSize }
      });
      
      setOrders(response.data.items);
      setPagination(response.data.pagination);
    } catch (error) {
      console.error('Error loading purchase orders:', error);
      setOrders([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadOrders();
  }, []);

  return { orders, loading, pagination, loadOrders };
};
