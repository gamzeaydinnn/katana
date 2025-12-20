import { test, expect, type APIRequestContext } from "@playwright/test";

const API = process.env.E2E_API_BASE || "http://localhost:5055";

async function login(apiRequest: APIRequestContext) {
  const resp = await apiRequest.post(`${API}/api/Auth/login`, {
    data: { username: "admin", password: "Katana2025!" },
  });
  expect(resp.ok()).toBeTruthy();
  const json = await resp.json();
  expect(json.token).toBeTruthy();
  return json.token as string;
}

test.describe("Admin Pending Adjustment E2E (API)", () => {
  test("create → list → approve", async ({ request: api }) => {
    const token = await login(api);

    // create test pending
    const create = await api.post(
      `${API}/api/adminpanel/pending-adjustments/test-create`,
      {
        headers: { Authorization: `Bearer ${token}` },
      }
    );
    expect(create.ok()).toBeTruthy();
    const created = await create.json();
    expect(created.ok).toBeTruthy();
    const pendingId = created.pendingId as number;
    expect(pendingId).toBeGreaterThan(0);

    // list latest
    const list = await api.get(`${API}/api/adminpanel/pending-adjustments`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(list.ok()).toBeTruthy();
    const listed = await list.json();
    expect(Array.isArray(listed.items)).toBeTruthy();
    const found = (listed.items as any[]).some((i) => i.id === pendingId);
    expect(found).toBeTruthy();

    // approve
    const approve = await api.post(
      `${API}/api/adminpanel/pending-adjustments/${pendingId}/approve?approvedBy=e2e`,
      {
        headers: { Authorization: `Bearer ${token}` },
      }
    );
    expect(approve.ok()).toBeTruthy();
    const approved = await approve.json();
    expect(approved.ok).toBeTruthy();
  });

  test("create → reject", async ({ request: api }) => {
    const token = await login(api);

    // create
    const create = await api.post(
      `${API}/api/adminpanel/pending-adjustments/test-create`,
      {
        headers: { Authorization: `Bearer ${token}` },
      }
    );
    const created = await create.json();
    const pendingId = created.pendingId as number;

    // reject
    const reject = await api.post(
      `${API}/api/adminpanel/pending-adjustments/${pendingId}/reject`,
      {
        headers: {
          Authorization: `Bearer ${token}`,
          "Content-Type": "application/json",
        },
        data: { rejectedBy: "e2e", reason: "e2e test" },
      }
    );
    expect(reject.ok()).toBeTruthy();
    const rej = await reject.json();
    expect(rej.ok).toBeTruthy();
  });
});
