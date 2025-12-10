# Implementation Plan

- [x] 1. Update KozaIntegration stock cards state and search functionality

  - [x] 1.1 Add searchTerm and filteredStockCards state variables to KozaIntegration.tsx

    - Add `const [searchTerm, setSearchTerm] = useState("")`
    - Add `const [filteredStockCards, setFilteredStockCards] = useState<KozaStokKarti[]>([])`
    - _Requirements: 2.1, 2.2_

  - [x] 1.2 Implement search filter useEffect

    - Filter stockCards by kartKodu or kartAdi containing searchTerm (case-insensitive)
    - Update filteredStockCards when searchTerm or stockCards changes
    - _Requirements: 2.2_

  - [ ] 1.3 Write property test for search filter
    - **Property 1: Search filter returns matching items only**
    - **Validates: Requirements 2.2**

- [x] 2. Add search UI and statistics chips to stock cards tab

  - [ ] 2.1 Add TextField search input with SearchIcon

    - Match LucaProducts search input styling

    - _Requirements: 2.1_

  - [ ] 2.2 Add Chip components for total and filtered counts
    - Show "Toplam: {stockCards.length}" and "Görüntülenen: {filteredStockCards.length}"
    - _Requirements: 2.3, 3.1, 3.2_
  - [x] 2.3 Write property test for count display

    - **Property 2: Filtered count matches actual filtered array length**
    - **Property 4: Total count matches original data length**

    - **Validates: Requirements 2.3, 3.1, 3.2**

- [ ] 3. Expand stock cards table columns

  - [x] 3.1 Update TableHead with new columns

    - Add columns: Ürün Kodu, Ürün Adı, Barkod, Kategori, Ölçü Birimi, Miktar, Birim Fiyat, KDV Oranı, Durum, Son Güncelleme
    - _Requirements: 1.1_

  - [ ] 3.2 Update TableBody to display all fields with placeholder handling

    - Display "-" for missing optional fields (barkod, olcumBirimi, miktar, birimFiyat, sonGuncelleme)
    - Add Chip for durum (Aktif/Pasif)
    - _Requirements: 1.2, 1.3_

  - [x] 3.3 Write property test for placeholder display

    - **Property 3: Missing fields display placeholder**
    - **Validates: Requirements 1.3**

- [ ] 4. Add mobile card view for stock cards

  - [x] 4.1 Add useMediaQuery hook for mobile detection

    - Use breakpoint similar to LucaProducts (max-width: 900px)
    - _Requirements: 4.1_

  - [ ] 4.2 Implement mobile card layout with Paper components

    - Show all essential fields in card format
    - Match LucaProducts mobile card styling
    - _Requirements: 4.1, 4.2_

- [ ] 5. Update table to use filteredStockCards

  - [ ] 5.1 Replace stockCards with filteredStockCards in table rendering
    - Update both desktop table and mobile card views
    - _Requirements: 2.2_
  - [ ] 5.2 Add empty state message for no search results
    - Show "Arama sonucu bulunamadı" when filteredStockCards is empty but stockCards has items
    - _Requirements: 2.2_

- [ ] 6. Final Checkpoint - Make sure all tests pass
  - Ensure all tests pass, ask the user if questions arise.
