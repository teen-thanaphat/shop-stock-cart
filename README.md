# ระบบสต๊อกสินค้า (Stock) และตะกร้าสินค้า (Shopping Cart)

- **Frontend:** Next.js 15 (`frontend/`)
- **Backend:** ASP.NET Core 9 Web API, C# (`backend/`)
- **Database:** SQL Server (Entity Framework Core)
- การสื่อสารระหว่าง Frontend และ Backend เป็น REST API

## วิธีรัน

### 1. Database (SQL Server)
ค่าเริ่มต้นเชื่อมต่อกับ `localhost\SQLEXPRESS` ด้วย Windows Authentication
แก้ได้ที่ `backend/appsettings.json` → `ConnectionStrings:DefaultConnection`

ตอน Backend เริ่มทำงาน จะสร้างฐานข้อมูล `ShopDb` พร้อม Mock up ข้อมูลสินค้าและ Stock ให้อัตโนมัติ
(หรือจะรัน `database.sql` เองก็ได้)

### 2. Backend
```bash
cd backend
dotnet run --launch-profile http     # http://localhost:5080
```

### 3. Frontend
```bash
cd frontend
npm install
npm run dev                          # http://localhost:3000
```
URL ของ Backend ตั้งค่าได้ใน `frontend/.env.local` → `NEXT_PUBLIC_API_URL`

## REST API

| Method | Path | คำอธิบาย |
|---|---|---|
| GET | `/api/products` | รายการสินค้า + จำนวนคงเหลือใน Stock |
| GET | `/api/carts/{cartId}` | ดูตะกร้า |
| POST | `/api/carts/{cartId}/items` | เพิ่มสินค้าลงตะกร้า `{ productCode, quantity }` |
| PUT | `/api/carts/{cartId}/items/{productCode}` | เพิ่ม/ลดจำนวน `{ quantity }` (0 = ลบ) |
| DELETE | `/api/carts/{cartId}/items/{productCode}` | ลบรายการสินค้า |
| DELETE | `/api/carts/{cartId}` | ล้างตะกร้าทั้งหมด |
| POST | `/api/carts/{cartId}/checkout` | ชำระเงิน: คืนยอดที่ต้องชำระ และตัด Stock |
| GET | `/api/carts/{cartId}/orders` | ประวัติการสั่งซื้อ (ล่าสุดก่อน) |

`cartId` เป็น GUID ที่ Frontend สร้างและเก็บไว้ใน localStorage

## หลักการจัดการ Stock

ตาราง `Stocks` มี `Quantity` (คงเหลือในสต๊อก) และ `Reserved` (จำนวนที่ถูกจองอยู่ในตะกร้า)

- **เพิ่มลงตะกร้า:** ระบบจะจองสินค้าได้เฉพาะเมื่อ `Quantity - Reserved` เพียงพอ ถ้าไม่พอจะตอบ `409` และเพิ่มลงตะกร้าไม่ได้
  (เช็กและจองในคำสั่ง SQL `UPDATE ... WHERE` เดียว จึงไม่มีปัญหาเมื่อมีผู้ใช้หลายคนพร้อมกัน)
- **ลด / ลบ / ล้างตะกร้า:** คืนยอดที่จองไว้กลับเข้า Stock
- **Check out:** ตัด `Quantity` จริง ปลดยอดจอง บันทึกคำสั่งซื้อ (`Orders`, `OrderItems`) และคืนยอดที่ต้องชำระ
  ทุกขั้นตอนอยู่ใน Transaction เดียวกัน
