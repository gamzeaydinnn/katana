import { useEffect, useState } from "react";

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
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await fetch("/api/adminpanel/luca/products");
      if (!res.ok) throw new Error(`Load failed (${res.status})`);
      const data = (await res.json()) as LucaProduct[];
      setProducts(data);
    } catch (e: any) {
      setError(e?.message ?? "Load failed");
    } finally {
      setLoading(false);
    }
  };

  const syncFromKoza = async () => {
    setSyncing(true);
    setError(null);
    try {
      const res = await fetch("/api/adminpanel/luca/sync-products", { method: "POST" });
      if (!res.ok) throw new Error(`Sync failed (${res.status})`);
      await load();
    } catch (e: any) {
      setError(e?.message ?? "Sync failed");
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

      {error && <div className="text-sm text-red-600">{error}</div>}

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

export default LucaProductsTable;
