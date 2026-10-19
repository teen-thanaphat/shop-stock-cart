using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

/// <summary>ข้อมูลสินค้า: รหัสสินค้า, ชื่อสินค้า, ราคาขายต่อหน่วย</summary>
public class Product
{
    [Key, MaxLength(20)]
    public string ProductCode { get; set; } = "";

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    public Stock? Stock { get; set; }
}

/// <summary>
/// ข้อมูล Stock: รหัสสินค้า, จำนวนคงเหลือในสต๊อก
/// Reserved = จำนวนที่ถูกจองไว้ในตะกร้า (ยังไม่ชำระเงิน)
/// จำนวนที่ยังหยิบใส่ตะกร้าได้ = Quantity - Reserved
/// </summary>
public class Stock
{
    [Key, MaxLength(20)]
    public string ProductCode { get; set; } = "";

    public int Quantity { get; set; }

    public int Reserved { get; set; }

    public Product? Product { get; set; }
}

public class CartItem
{
    public int Id { get; set; }

    public Guid CartId { get; set; }

    [MaxLength(20)]
    public string ProductCode { get; set; } = "";

    public int Quantity { get; set; }

    public Product? Product { get; set; }
}

public class Order
{
    public int Id { get; set; }

    public Guid CartId { get; set; }

    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    public List<OrderItem> Items { get; set; } = [];
}

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    [MaxLength(20)]
    public string ProductCode { get; set; } = "";

    [MaxLength(200)]
    public string ProductName { get; set; } = "";

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal LineTotal { get; set; }
}
