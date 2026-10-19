using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class CartException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

/// <summary>
/// ตะกร้าสินค้า: การเพิ่ม/ลดสินค้าในตะกร้าจะ "จอง" Stock (Stock.Reserved)
/// เพื่อให้จำนวนคงเหลือที่หยิบได้ถูกต้องเสมอ และเมื่อ Check out จะตัด Stock.Quantity จริง
/// </summary>
public class CartService(AppDbContext db)
{
    public async Task<CartDto> GetCartAsync(Guid cartId)
    {
        var items = await db.CartItems
            .Where(c => c.CartId == cartId)
            .OrderBy(c => c.ProductCode)
            .Select(c => new CartItemDto(
                c.ProductCode,
                c.Product!.Name,
                c.Product.UnitPrice,
                c.Quantity,
                c.Product.UnitPrice * c.Quantity,
                c.Product.Stock!.Quantity - c.Product.Stock.Reserved))
            .ToListAsync();

        return new CartDto(cartId, items, items.Sum(i => i.Quantity), items.Sum(i => i.LineTotal));
    }

    public async Task<CartDto> AddItemAsync(Guid cartId, string productCode, int quantity)
    {
        if (quantity <= 0)
            throw new CartException(400, "จำนวนสินค้าต้องมากกว่า 0");

        await InTransactionAsync(async () =>
        {
            var current = await db.CartItems
                .Where(c => c.CartId == cartId && c.ProductCode == productCode)
                .Select(c => (int?)c.Quantity)
                .FirstOrDefaultAsync() ?? 0;
            await SetQuantityCoreAsync(cartId, productCode, current + quantity);
        });
        return await GetCartAsync(cartId);
    }

    public async Task<CartDto> UpdateItemAsync(Guid cartId, string productCode, int quantity)
    {
        if (quantity < 0)
            throw new CartException(400, "จำนวนสินค้าต้องไม่ติดลบ");

        await InTransactionAsync(() => SetQuantityCoreAsync(cartId, productCode, quantity));
        return await GetCartAsync(cartId);
    }

    public async Task<CartDto> RemoveItemAsync(Guid cartId, string productCode)
    {
        await InTransactionAsync(() => SetQuantityCoreAsync(cartId, productCode, 0));
        return await GetCartAsync(cartId);
    }

    public async Task<CartDto> ClearAsync(Guid cartId)
    {
        await InTransactionAsync(async () =>
        {
            var codes = await db.CartItems
                .Where(c => c.CartId == cartId)
                .Select(c => c.ProductCode)
                .ToListAsync();
            foreach (var code in codes)
                await SetQuantityCoreAsync(cartId, code, 0);
        });
        return await GetCartAsync(cartId);
    }

    public async Task<OrderDto> CheckoutAsync(Guid cartId)
    {
        Order? order = null;

        await InTransactionAsync(async () =>
        {
            var items = await db.CartItems
                .Include(c => c.Product)
                .Where(c => c.CartId == cartId)
                .OrderBy(c => c.ProductCode)
                .ToListAsync();

            if (items.Count == 0)
                throw new CartException(400, "ไม่มีสินค้าในตะกร้า");

            foreach (var item in items)
            {
                // ตัด Stock จริง และปลดยอดที่จองไว้
                var updated = await db.Stocks
                    .Where(s => s.ProductCode == item.ProductCode && s.Reserved >= item.Quantity)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.Quantity, x => x.Quantity - item.Quantity)
                        .SetProperty(x => x.Reserved, x => x.Reserved - item.Quantity));
                if (updated == 0)
                    throw new CartException(409, $"สินค้า {item.ProductCode} มีจำนวนใน Stock ไม่เพียงพอ");
            }

            order = new Order
            {
                CartId = cartId,
                CreatedAt = DateTime.UtcNow,
                Items = items.Select(i => new OrderItem
                {
                    ProductCode = i.ProductCode,
                    ProductName = i.Product!.Name,
                    UnitPrice = i.Product.UnitPrice,
                    Quantity = i.Quantity,
                    LineTotal = i.Product.UnitPrice * i.Quantity,
                }).ToList(),
            };
            order.TotalAmount = order.Items.Sum(i => i.LineTotal);

            db.Orders.Add(order);
            db.CartItems.RemoveRange(items);
            await db.SaveChangesAsync();
        });

        return ToDto(order!);
    }

    /// <summary>ประวัติการสั่งซื้อของตะกร้านี้ (ล่าสุดก่อน)</summary>
    public async Task<List<OrderDto>> GetOrdersAsync(Guid cartId)
    {
        var orders = await db.Orders
            .Include(o => o.Items)
            .Where(o => o.CartId == cartId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(ToDto).ToList();
    }

    private static OrderDto ToDto(Order order) => new(
        order.Id,
        // SQL Server ไม่เก็บ DateTimeKind จึงระบุว่าเป็น UTC เพื่อให้ JSON มี "Z" ต่อท้าย
        DateTime.SpecifyKind(order.CreatedAt, DateTimeKind.Utc),
        order.Items.OrderBy(i => i.ProductCode)
            .Select(i => new OrderItemDto(i.ProductCode, i.ProductName, i.UnitPrice, i.Quantity, i.LineTotal))
            .ToList(),
        order.TotalAmount);

    /// <summary>ตั้งจำนวนสินค้าในตะกร้า และปรับยอดจองใน Stock ตามส่วนต่าง</summary>
    private async Task SetQuantityCoreAsync(Guid cartId, string productCode, int newQuantity)
    {
        if (!await db.Products.AnyAsync(p => p.ProductCode == productCode))
            throw new CartException(404, $"ไม่พบสินค้ารหัส {productCode}");

        var item = await db.CartItems
            .FirstOrDefaultAsync(c => c.CartId == cartId && c.ProductCode == productCode);
        var current = item?.Quantity ?? 0;
        var delta = newQuantity - current;

        if (delta > 0)
        {
            // จองเพิ่มได้เฉพาะเมื่อคงเหลือ (Quantity - Reserved) เพียงพอ — ทำแบบ atomic ใน SQL
            var updated = await db.Stocks
                .Where(s => s.ProductCode == productCode && s.Quantity - s.Reserved >= delta)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Reserved, x => x.Reserved + delta));
            if (updated == 0)
                throw new CartException(409, $"สินค้า {productCode} มีจำนวนคงเหลือไม่เพียงพอ");
        }
        else if (delta < 0)
        {
            await db.Stocks
                .Where(s => s.ProductCode == productCode)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Reserved, x => x.Reserved + delta));
        }

        if (newQuantity == 0)
        {
            if (item is not null) db.CartItems.Remove(item);
        }
        else if (item is null)
        {
            db.CartItems.Add(new CartItem { CartId = cartId, ProductCode = productCode, Quantity = newQuantity });
        }
        else
        {
            item.Quantity = newQuantity;
        }

        await db.SaveChangesAsync();
    }

    private async Task InTransactionAsync(Func<Task> action)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await action();
        await tx.CommitAsync();
    }
}
