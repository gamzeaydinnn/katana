import { useEffect, useState } from "react";

export function ProductMappingPage() {
  const [katana, setKatana] = useState<any[]>([]);
  const [luca, setLuca] = useState<any[]>([]);
  const [selectedKatana, setSelectedKatana] = useState<any | null>(null);

  useEffect(() => {
    fetch("/api/adminpanel/luca/mapping/products")
      .then((res) => res.json())
      .then((data) => {
        setKatana(data.katanaProducts);
        setLuca(data.lucaProducts);
      });
  }, []);

  return (
    <div className="grid grid-cols-3 gap-4">
      <div className="border rounded p-2">
        <h3 className="font-semibold mb-2">Katana Ürünleri</h3>
        <ul className="max-h-96 overflow-auto text-sm">
          {katana.map((p) => (
            <li
              key={p.id}
              onClick={() => setSelectedKatana(p)}
              className={`px-2 py-1 cursor-pointer hover:bg-gray-100 ${
                selectedKatana?.id === p.id ? "bg-blue-50" : ""
              }`}
            >
              <span className="font-mono mr-2">{p.sku}</span>
              {p.name}
            </li>
          ))}
        </ul>
      </div>

      <div className="border rounded p-2">
        <h3 className="font-semibold mb-2">Eşleştirme</h3>
        {selectedKatana ? (
          <>
            <div className="mb-4 text-sm">
              <div className="font-medium">Seçili Katana ürünü:</div>
              <div className="font-mono">{selectedKatana.sku}</div>
              <div>{selectedKatana.name}</div>
            </div>
            <p className="text-xs text-gray-500">
              Sağdaki listeden bir Koza ürünü seçip eşleştirme kaydedebilirsin.
            </p>
          </>
        ) : (
          <div className="text-sm text-gray-500">
            Soldan bir Katana ürünü seç.
          </div>
        )}
      </div>

      <div className="border rounded p-2">
        <h3 className="font-semibold mb-2">Koza Ürünleri (Cache)</h3>
        <ul className="max-h-96 overflow-auto text-sm">
          {luca.map((p) => (
            <li
              key={p.id}
              className="px-2 py-1 hover:bg-gray-100 cursor-pointer"
            >
              <span className="font-mono mr-2">{p.code}</span>
              {p.name}
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}
