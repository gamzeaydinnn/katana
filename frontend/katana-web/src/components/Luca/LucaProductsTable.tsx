import { useEffect, useState } from "react";
import { stockAPI } from "../../services/api";

type LucaProduct = {
  id: number;
  lucaCode: string;
  lucaName: string;
  lucaCategory?: string | null;
};

export function LucaProductsTable() {
  const [products, setProducts] = useState<LucaProduct[]>([]);
  const [loading, setLoading] = useState(false);
  const [syncing, setSyncing] = useState(false);

  const load = async () => {
    setLoading(true);
    try {
      const res = await fetch("/api/adminpanel/luca/products");
      const data = await res.json();
      setProducts(data);
    } finally {
      setLoading(false);
    }
  };

  const syncFromKoza = async () => {
    setSyncing(true);
    try {
      // Call the sync endpoint to refresh Luca products from Koza
      const res = await fetch("/api/adminpanel/luca/sync-products", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
      });

      if (!res.ok) {
        throw new Error(`Sync failed: ${res.status}`);
      }

      const result = await res.json();
      console.log("Sync result:", result);

      // Reload products from cache
      await load();
    } catch (err) {
      console.error("Sync error:", err);
      alert("Senkronizasyon başarısız oldu. Lütfen tekrar deneyin.");
    } finally {
      setSyncing(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold">Koza Ürünleri (Cache)</h2>
        <button
          onClick={syncFromKoza}
          disabled={syncing}
          className="px-3 py-1 rounded bg-blue-600 text-white disabled:opacity-50"
        >
          {syncing ? "Senkronize ediliyor..." : "Koza'dan Yenile"}
        </button>
      </div>

      {loading ? (
        <div>Yükleniyor...</div>
      ) : (
        <table className="min-w-full text-sm border">
          <thead className="bg-gray-100">
            <tr>
              <th className="px-2 py-1 border">Kod</th>
              <th className="px-2 py-1 border">Ad</th>
              <th className="px-2 py-1 border">Kategori</th>
            </tr>
          </thead>
          <tbody>
            {products.map((p) => (
              <tr key={p.id} className="hover:bg-gray-50">
                <td className="px-2 py-1 border font-mono">{p.lucaCode}</td>
                <td className="px-2 py-1 border">{p.lucaName}</td>
                <td className="px-2 py-1 border">{p.lucaCategory ?? "-"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
