"use client";

import { useCallback, useEffect, useState } from "react";
import { api, formatBaht, type Cart, type Order, type Product } from "@/lib/api";

const CART_KEY = "cartId";

function getCartId() {
  let id = localStorage.getItem(CART_KEY);
  if (!id) {
    id = crypto.randomUUID();
    localStorage.setItem(CART_KEY, id);
  }
  return id;
}

export default function Home() {
  const [cartId, setCartId] = useState<string | null>(null);
  const [products, setProducts] = useState<Product[]>([]);
  const [cart, setCart] = useState<Cart | null>(null);
  const [order, setOrder] = useState<Order | null>(null);
  const [history, setHistory] = useState<Order[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const refresh = useCallback(async (id: string) => {
    const [p, c] = await Promise.all([api.getProducts(), api.getCart(id)]);
    setProducts(p);
    setCart(c);
  }, []);

  useEffect(() => {
    const id = getCartId();
    setCartId(id);
    refresh(id).catch((e) => setError(`เชื่อมต่อ Backend ไม่ได้: ${e.message}`));
  }, [refresh]);

  // เรียก API แล้วโหลดสินค้า/ตะกร้าใหม่ เพื่อให้ยอด Stock ตรงกับฐานข้อมูลเสมอ
  async function run<T>(action: (id: string) => Promise<T>) {
    if (!cartId) return;
    setBusy(true);
    setError(null);
    try {
      return await action(cartId);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      await refresh(cartId).catch(() => {});
      setBusy(false);
    }
  }

  async function checkout() {
    const result = await run((id) => api.checkout(id));
    if (result) setOrder(result);
  }

  async function openHistory() {
    const result = await run((id) => api.getOrders(id));
    if (result) setHistory(result);
  }

  const cartQty = (code: string) => cart?.items.find((i) => i.productCode === code)?.quantity ?? 0;

  return (
    <main className="container">
      <header className="header">
        <h1>ร้านค้าออนไลน์</h1>
        <div className="header-actions">
          <button className="btn" disabled={busy} onClick={openHistory}>
            ประวัติการสั่งซื้อ
          </button>
          <span className="cart-icon" title={`ตะกร้า: ${cart?.totalQuantity ?? 0} ชิ้น`} aria-label={`ตะกร้า ${cart?.totalQuantity ?? 0} ชิ้น`}>
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
              <circle cx="9" cy="21" r="1" />
              <circle cx="20" cy="21" r="1" />
              <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6" />
            </svg>
            <span className="cart-count">{cart?.totalQuantity ?? 0}</span>
          </span>
        </div>
      </header>

      {error && (
        <div className="alert" role="alert">
          {error}
          <button className="link" onClick={() => setError(null)}>ปิด</button>
        </div>
      )}

      <div className="layout">
        <section>
          <h2>สินค้า (Stock)</h2>
          <table className="table">
            <thead>
              <tr>
                <th>รหัสสินค้า</th>
                <th>ชื่อสินค้า</th>
                <th className="num">ราคา/หน่วย</th>
                <th className="num">คงเหลือในสต๊อก</th>
                <th className="num">ในตะกร้า</th>
                <th className="num">เลือกได้อีก</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {products.map((p) => (
                <tr key={p.productCode}>
                  <td className="mono">{p.productCode}</td>
                  <td className="name">{p.name}</td>
                  <td className="num">{formatBaht(p.unitPrice)}</td>
                  <td className="num">{p.stockQuantity}</td>
                  <td className="num">{cartQty(p.productCode)}</td>
                  <td className={`num ${p.available === 0 ? "out" : ""}`}>
                    {p.available === 0 ? "หมด" : p.available}
                  </td>
                  <td className="num">
                    <button
                      className="btn"
                      disabled={busy || p.available === 0}
                      onClick={() => run((id) => api.addItem(id, p.productCode))}
                    >
                      เพิ่มลงตะกร้า
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>

        <aside className="cart">
          <h2>ตะกร้าสินค้า (Shopping Cart)</h2>
          {!cart || cart.items.length === 0 ? (
            <p className="muted">ยังไม่มีสินค้าในตะกร้า</p>
          ) : (
            <>
              <ul className="cart-list">
                {cart.items.map((i) => (
                  <li key={i.productCode}>
                    <div className="cart-row">
                      <span>{i.name}</span>
                      <span>{formatBaht(i.lineTotal)}</span>
                    </div>
                    <div className="cart-row">
                      <div className="qty">
                        <button
                          className="btn small"
                          disabled={busy}
                          onClick={() => run((id) => api.updateItem(id, i.productCode, i.quantity - 1))}
                          aria-label="ลด"
                        >
                          −
                        </button>
                        <span>{i.quantity}</span>
                        <button
                          className="btn small"
                          disabled={busy || i.available === 0}
                          onClick={() => run((id) => api.updateItem(id, i.productCode, i.quantity + 1))}
                          aria-label="เพิ่ม"
                        >
                          +
                        </button>
                        <span className="muted">× {formatBaht(i.unitPrice)}</span>
                      </div>
                      <button
                        className="link danger"
                        disabled={busy}
                        onClick={() => run((id) => api.removeItem(id, i.productCode))}
                      >
                        ลบ
                      </button>
                    </div>
                  </li>
                ))}
              </ul>
              <div className="cart-row total">
                <span>ยอดรวม</span>
                <span>{formatBaht(cart.totalAmount)}</span>
              </div>
              <div className="actions">
                <button className="btn secondary" disabled={busy} onClick={() => run((id) => api.clearCart(id))}>
                  ล้างตะกร้า
                </button>
                <button className="btn primary" disabled={busy} onClick={checkout}>
                  ชำระเงิน (Check out)
                </button>
              </div>
            </>
          )}
        </aside>
      </div>

      {order && (
        <div className="modal-backdrop" onClick={() => setOrder(null)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h2>ชำระเงินสำเร็จ</h2>
            <p className="muted">
              คำสั่งซื้อ #{order.orderId} · {new Date(order.createdAt).toLocaleString("th-TH")}
            </p>
            <OrderItemsTable order={order} />
            <div className="cart-row total">
              <span>ยอดที่ต้องชำระ</span>
              <span>{formatBaht(order.totalAmount)}</span>
            </div>
            <button className="btn primary full" onClick={() => setOrder(null)}>
              ตกลง
            </button>
          </div>
        </div>
      )}

      {history && (
        <div className="modal-backdrop" onClick={() => setHistory(null)}>
          <div className="modal wide" onClick={(e) => e.stopPropagation()}>
            <div className="cart-row">
              <h2>ประวัติการสั่งซื้อ</h2>
              <button className="link" onClick={() => setHistory(null)}>ปิด</button>
            </div>
            {history.length === 0 ? (
              <p className="muted">ยังไม่มีรายการสั่งซื้อ</p>
            ) : (
              <div className="history-list">
                {history.map((o, idx) => (
                  <details key={o.orderId} className="history-item" open={idx === 0}>
                    <summary className="cart-row">
                      <span>
                        <strong>คำสั่งซื้อ #{o.orderId}</strong>{" "}
                        <span className="muted">
                          · {new Date(o.createdAt).toLocaleString("th-TH")} ·{" "}
                          {o.items.reduce((sum, i) => sum + i.quantity, 0)} ชิ้น
                        </span>
                      </span>
                      <strong>{formatBaht(o.totalAmount)}</strong>
                    </summary>
                    <OrderItemsTable order={o} />
                  </details>
                ))}
              </div>
            )}
          </div>
        </div>
      )}
    </main>
  );
}

function OrderItemsTable({ order }: { order: Order }) {
  return (
    <table className="table">
      <thead>
        <tr>
          <th>สินค้า</th>
          <th className="num">จำนวน</th>
          <th className="num">ราคา/หน่วย</th>
          <th className="num">รวม</th>
        </tr>
      </thead>
      <tbody>
        {order.items.map((i) => (
          <tr key={i.productCode}>
            <td>{i.productName}</td>
            <td className="num">{i.quantity}</td>
            <td className="num">{formatBaht(i.unitPrice)}</td>
            <td className="num">{formatBaht(i.lineTotal)}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
