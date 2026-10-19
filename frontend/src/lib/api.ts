const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080";

export type Product = {
  productCode: string;
  name: string;
  unitPrice: number;
  stockQuantity: number;
  reserved: number;
  available: number;
};

export type CartItem = {
  productCode: string;
  name: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
  available: number;
};

export type Cart = {
  cartId: string;
  items: CartItem[];
  totalQuantity: number;
  totalAmount: number;
};

export type Order = {
  orderId: number;
  createdAt: string;
  items: {
    productCode: string;
    productName: string;
    unitPrice: number;
    quantity: number;
    lineTotal: number;
  }[];
  totalAmount: number;
};

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_URL}${path}`, {
    ...init,
    headers: { "Content-Type": "application/json", ...init?.headers },
    cache: "no-store",
  });
  if (!res.ok) {
    const problem = await res.json().catch(() => null);
    throw new Error(problem?.detail ?? problem?.title ?? `HTTP ${res.status}`);
  }
  return res.json();
}

export const api = {
  getProducts: () => request<Product[]>("/api/products"),
  getCart: (cartId: string) => request<Cart>(`/api/carts/${cartId}`),
  addItem: (cartId: string, productCode: string, quantity = 1) =>
    request<Cart>(`/api/carts/${cartId}/items`, {
      method: "POST",
      body: JSON.stringify({ productCode, quantity }),
    }),
  updateItem: (cartId: string, productCode: string, quantity: number) =>
    request<Cart>(`/api/carts/${cartId}/items/${productCode}`, {
      method: "PUT",
      body: JSON.stringify({ quantity }),
    }),
  removeItem: (cartId: string, productCode: string) =>
    request<Cart>(`/api/carts/${cartId}/items/${productCode}`, { method: "DELETE" }),
  clearCart: (cartId: string) => request<Cart>(`/api/carts/${cartId}`, { method: "DELETE" }),
  checkout: (cartId: string) => request<Order>(`/api/carts/${cartId}/checkout`, { method: "POST" }),
  getOrders: (cartId: string) => request<Order[]>(`/api/carts/${cartId}/orders`),
};

export const formatBaht = (n: number) =>
  n.toLocaleString("th-TH", { style: "currency", currency: "THB" });
