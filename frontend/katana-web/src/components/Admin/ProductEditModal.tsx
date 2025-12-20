import React, { useState, useEffect } from "react";
import {
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalFooter,
  ModalBody,
  ModalCloseButton,
  Button,
  FormControl,
  FormLabel,
  Input,
  Textarea,
  Select,
  NumberInput,
  NumberInputField,
  VStack,
  HStack,
  useToast,
  Text,
  Badge,
  Divider,
  Box,
} from "@chakra-ui/react";
import axios from "axios";

interface Category {
  id: number;
  name: string;
}

interface Product {
  id: number;
  sku: string;
  name: string;
  categoryId: number;
  stock: number;
  price: number;
  isActive: boolean;
  uzunAdi?: string;
  barcode?: string;
  unitId?: number;
  purchasePrice?: number;
  kdvRate?: number;
  gtipCode?: string;
}

interface UpdateProductData {
  name?: string;
  uzunAdi?: string;
  barcode?: string;
  categoryId?: number;
  unitId?: number;
  quantity?: number;
  purchasePrice?: number;
  salesPrice?: number;
  kdvRate?: number;
  gtipCode?: string;
}

interface ProductEditModalProps {
  isOpen: boolean;
  onClose: () => void;
  product: Product | null;
  categories: Category[];
  onSave: () => void;
}

// Luca'da güncellenebilen ölçü birimleri
const UNIT_OPTIONS = [
  { value: 5, label: "ADET" },
  { value: 1, label: "KG" },
  { value: 2, label: "METRE" },
  { value: 3, label: "LITRE" },
  { value: 4, label: "M²" },
  { value: 6, label: "M³" },
];

const KDV_OPTIONS = [
  { value: 0, label: "%0" },
  { value: 1, label: "%1" },
  { value: 8, label: "%8" },
  { value: 10, label: "%10" },
  { value: 18, label: "%18" },
  { value: 20, label: "%20" },
];

const ProductEditModal: React.FC<ProductEditModalProps> = ({
  isOpen,
  onClose,
  product,
  categories,
  onSave,
}) => {
  const toast = useToast();
  const [loading, setLoading] = useState(false);
  const [formData, setFormData] = useState<UpdateProductData>({});

  useEffect(() => {
    if (product && isOpen) {
      setFormData({
        name: product.name,
        uzunAdi: product.uzunAdi || "",
        barcode: product.barcode || "",
        categoryId: product.categoryId,
        unitId: product.unitId || 5,
        quantity: product.stock,
        purchasePrice: product.purchasePrice || 0,
        salesPrice: product.price,
        kdvRate: product.kdvRate || 18,
        gtipCode: product.gtipCode || "",
      });
    }
  }, [product, isOpen]);

  const handleSave = async () => {
    if (!product) return;

    try {
      setLoading(true);

      const response = await axios.put(
        `/api/products/${product.id}/sync-to-luca`,
        formData
      );

      if (response.data.success) {
        toast({
          title: "Başarılı",
          description: response.data.lucaUpdated
            ? "Ürün güncellendi ve Luca'ya senkronize edildi"
            : "Ürün güncellendi (Luca senkronizasyonu başarısız)",
          status: response.data.lucaUpdated ? "success" : "warning",
          duration: 3000,
          isClosable: true,
        });
        onSave();
        onClose();
      }
    } catch (error: any) {
      console.error("Ürün güncelleme hatası:", error);
      toast({
        title: "Hata",
        description:
          error.response?.data?.error || "Ürün güncellenirken hata oluştu",
        status: "error",
        duration: 5000,
        isClosable: true,
      });
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (field: keyof UpdateProductData, value: any) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
  };

  if (!product) return null;

  return (
    <Modal isOpen={isOpen} onClose={onClose} size="xl">
      <ModalOverlay />
      <ModalContent>
        <ModalHeader>
          Ürünü Düzenle
          <Badge ml={2} colorScheme="blue">
            Luca Sync
          </Badge>
        </ModalHeader>
        <ModalCloseButton />

        <ModalBody>
          <VStack spacing={4} align="stretch">
            {/* ÜRÜN KODU - READ ONLY */}
            <FormControl>
              <FormLabel>Ürün Kodu (SKU)</FormLabel>
              <Input
                value={product.sku}
                isReadOnly
                bg="gray.100"
                fontWeight="bold"
              />
              <Text fontSize="xs" color="gray.500" mt={1}>
                Ürün kodu değiştirilemez
              </Text>
            </FormControl>

            <Divider />

            {/* ÜRÜN ADI */}
            <FormControl isRequired>
              <FormLabel>Ürün Adı</FormLabel>
              <Input
                value={formData.name || ""}
                onChange={(e) => handleChange("name", e.target.value)}
                placeholder="Ürün adını giriniz"
              />
            </FormControl>

            {/* UZUN ADI */}
            <FormControl>
              <FormLabel>Uzun Adı / Açıklama</FormLabel>
              <Textarea
                value={formData.uzunAdi || ""}
                onChange={(e) => handleChange("uzunAdi", e.target.value)}
                placeholder="Detaylı ürün açıklaması"
                rows={2}
              />
            </FormControl>

            {/* BARKOD */}
            <FormControl>
              <FormLabel>Barkod</FormLabel>
              <Input
                value={formData.barcode || ""}
                onChange={(e) => handleChange("barcode", e.target.value)}
                placeholder="Barkod numarası"
              />
            </FormControl>

            <HStack spacing={4}>
              {/* KATEGORİ */}
              <FormControl isRequired flex={1}>
                <FormLabel>Kategori</FormLabel>
                <Select
                  value={formData.categoryId || ""}
                  onChange={(e) =>
                    handleChange("categoryId", parseInt(e.target.value))
                  }
                  placeholder="Kategori seçiniz"
                >
                  {categories.map((cat) => (
                    <option key={cat.id} value={cat.id}>
                      {cat.name}
                    </option>
                  ))}
                </Select>
              </FormControl>

              {/* ÖLÇÜ BİRİMİ */}
              <FormControl isRequired flex={1}>
                <FormLabel>Ölçü Birimi</FormLabel>
                <Select
                  value={formData.unitId || 5}
                  onChange={(e) =>
                    handleChange("unitId", parseInt(e.target.value))
                  }
                >
                  {UNIT_OPTIONS.map((unit) => (
                    <option key={unit.value} value={unit.value}>
                      {unit.label}
                    </option>
                  ))}
                </Select>
              </FormControl>
            </HStack>

            <HStack spacing={4}>
              {/* MİKTAR */}
              <FormControl flex={1}>
                <FormLabel>Miktar</FormLabel>
                <NumberInput
                  value={formData.quantity || 0}
                  min={0}
                  onChange={(_, val) => handleChange("quantity", val)}
                >
                  <NumberInputField placeholder="Stok miktarı" />
                </NumberInput>
              </FormControl>

              {/* KDV ORANI */}
              <FormControl flex={1}>
                <FormLabel>KDV Oranı</FormLabel>
                <Select
                  value={formData.kdvRate || 18}
                  onChange={(e) =>
                    handleChange("kdvRate", parseInt(e.target.value))
                  }
                >
                  {KDV_OPTIONS.map((kdv) => (
                    <option key={kdv.value} value={kdv.value}>
                      {kdv.label}
                    </option>
                  ))}
                </Select>
              </FormControl>
            </HStack>

            <HStack spacing={4}>
              {/* ALIŞ FİYATI */}
              <FormControl flex={1}>
                <FormLabel>Alış Fiyatı (TL)</FormLabel>
                <NumberInput
                  value={formData.purchasePrice || 0}
                  min={0}
                  precision={2}
                  onChange={(_, val) => handleChange("purchasePrice", val)}
                >
                  <NumberInputField placeholder="0.00" />
                </NumberInput>
              </FormControl>

              {/* SATIŞ FİYATI */}
              <FormControl flex={1}>
                <FormLabel>Satış Fiyatı (TL)</FormLabel>
                <NumberInput
                  value={formData.salesPrice || 0}
                  min={0}
                  precision={2}
                  onChange={(_, val) => handleChange("salesPrice", val)}
                >
                  <NumberInputField placeholder="0.00" />
                </NumberInput>
              </FormControl>
            </HStack>

            {/* GTIP KODU */}
            <FormControl>
              <FormLabel>GTIP Kodu</FormLabel>
              <Input
                value={formData.gtipCode || ""}
                onChange={(e) => handleChange("gtipCode", e.target.value)}
                placeholder="GTIP kodu"
              />
            </FormControl>

            <Box bg="blue.50" p={3} borderRadius="md">
              <Text fontSize="sm" color="blue.700">
                💡 Bu form ile yapılan değişiklikler hem yerel veritabanına hem
                de Luca/Koza sistemine kaydedilir.
              </Text>
            </Box>
          </VStack>
        </ModalBody>

        <ModalFooter>
          <Button variant="ghost" mr={3} onClick={onClose}>
            İptal
          </Button>
          <Button
            colorScheme="blue"
            onClick={handleSave}
            isLoading={loading}
            loadingText="Kaydediliyor..."
          >
            Kaydet ve Luca'ya Gönder
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
};

export default ProductEditModal;
