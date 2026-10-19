using Backend.Dtos;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/carts/{cartId:guid}")]
public class CartsController(CartService carts) : ControllerBase
{
    // GET api/carts/{cartId}
    [HttpGet]
    public Task<IActionResult> Get(Guid cartId) =>
        Run(() => carts.GetCartAsync(cartId));

    // POST api/carts/{cartId}/items — เพิ่มสินค้าลงตะกร้า
    [HttpPost("items")]
    public Task<IActionResult> AddItem(Guid cartId, AddCartItemRequest req) =>
        Run(() => carts.AddItemAsync(cartId, req.ProductCode, req.Quantity));

    // PUT api/carts/{cartId}/items/{productCode} — เพิ่ม/ลดจำนวน
    [HttpPut("items/{productCode}")]
    public Task<IActionResult> UpdateItem(Guid cartId, string productCode, UpdateCartItemRequest req) =>
        Run(() => carts.UpdateItemAsync(cartId, productCode, req.Quantity));

    // DELETE api/carts/{cartId}/items/{productCode} — ลบรายการสินค้า
    [HttpDelete("items/{productCode}")]
    public Task<IActionResult> RemoveItem(Guid cartId, string productCode) =>
        Run(() => carts.RemoveItemAsync(cartId, productCode));

    // DELETE api/carts/{cartId} — ล้างตะกร้าทั้งหมด
    [HttpDelete]
    public Task<IActionResult> Clear(Guid cartId) =>
        Run(() => carts.ClearAsync(cartId));

    // POST api/carts/{cartId}/checkout — ชำระเงิน
    [HttpPost("checkout")]
    public Task<IActionResult> Checkout(Guid cartId) =>
        Run(() => carts.CheckoutAsync(cartId));

    // GET api/carts/{cartId}/orders — ประวัติการสั่งซื้อ
    [HttpGet("orders")]
    public Task<IActionResult> GetOrders(Guid cartId) =>
        Run(() => carts.GetOrdersAsync(cartId));

    private async Task<IActionResult> Run<T>(Func<Task<T>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (CartException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode);
        }
    }
}
