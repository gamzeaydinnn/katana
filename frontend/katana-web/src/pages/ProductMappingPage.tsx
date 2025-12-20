import { useEffect, useState } from "react";

type KatanaProduct = {
  id: number;
  sku: string;
  name: string;
};

type LucaProduct = {
  id: number;
  code: string;
  name: string;
};

export function ProductMappingPage() {
  const [katana, setKatana] = useState<KatanaProduct[]>([]);
  const [luca, setLuca] = useState<LucaProduct[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedKatana, setSelectedKatana] = useState<KatanaProduct | null>(null);
  const [selectedLuca, setSelectedLuca] = useState<LucaProduct | null>(null);

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      setError(null);
      try {
        const res = await fetch("/api/adminpanel/luca/mapping/products");
        if (!res.ok) throw new Error(`Load failed (${res.status})`);
        const data = await res.json();
        setKatana(data.katanaProducts ?? []);
        setLuca(data.lucaProducts ?? []);
      } catch (e: any) {
        setError(e?.message ?? "Load failed");
      } finally {
        setLoading(false);
      }
    };

    load();
  }, []);

  const handleMap = async () => {
    if (!selectedKatana || !selectedLuca) return;
    // For now just log; optionally POST to backend if desired later
    console.log("MAP", { selectedKatana, selectedLuca });

    // Optional: send to backend
    // await fetch('/api/adminpanel/luca/mapping/products', { method: 'POST', body: JSON.stringify({ katanaId: selectedKatana.id, lucaId: selectedLuca.id }), headers: { 'Content-Type': 'application/json' } });
  };

  return (
    <div className="grid grid-cols-3 gap-4">
      {/* Katana listesi */}
      <div className="border rounded p-2">
        <h3 className="font-semibold mb-2">Katana Ürünleri</h3>
        {loading ? (
          <div>Yükleniyor...</div>
        ) : error ? (
          <div className="text-sm text-red-600">{error}</div>
        ) : (
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
        )}
      </div>

      {/* Orta kolon */}
      <div className="border rounded p-2">
        <h3 className="font-semibold mb-2">Eşleştirme</h3>

        {selectedKatana ? (
          <div className="mb-4 text-sm">
            <div className="font-medium">Katana:</div>
            <div className="font-mono">{selectedKatana.sku}</div>
            <div>{selectedKatana.name}</div>
          </div>
        ) : (
          <div className="text-sm text-gray-500">Soldan bir Katana ürünü seç.</div>
        )}

        {selectedLuca ? (
          <div className="mb-4 text-sm">
            <div className="font-medium">Koza:</div>
            <div className="font-mono">{selectedLuca.code}</div>
            <div>{selectedLuca.name}</div>
          </div>
        ) : (
          <div className="text-sm text-gray-500">Sağdan bir Koza ürünü seç.</div>
        )}

        <button
          className="mt-2 px-3 py-1 rounded bg-green-600 text-white disabled:opacity-50"
          disabled={!selectedKatana || !selectedLuca}
          onClick={handleMap}
        >
          Eşleştir
        </button>

        <p className="mt-2 text-xs text-gray-500">
          (Şimdilik sadece UI/state. Backend POST entegrasyonu sonra.)
        </p>
      </div>

      {/* Koza listesi */}
      <div className="border rounded p-2">
        <h3 className="font-semibold mb-2">Koza Ürünleri (Cache)</h3>
        {loading ? (
          <div>Yükleniyor...</div>
        ) : (
          <ul className="max-h-96 overflow-auto text-sm">
            {luca.map((p) => (
              <li
                key={p.id}
                onClick={() => setSelectedLuca(p)}
                className={`px-2 py-1 hover:bg-gray-100 cursor-pointer ${
                  selectedLuca?.id === p.id ? "bg-blue-50" : ""
                }`}
              >
                <span className="font-mono mr-2">{p.code}</span>
                {p.name}
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}

export default ProductMappingPage;
