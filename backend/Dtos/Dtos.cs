namespace Backend.Dtos;

public record ProductDto(string ProductCode, string Name, decimal UnitPrice, int StockQuantity, int Reserved, int Available);

public record CartItemDto(string ProductCode, string Name, decimal UnitPrice, int Quantity, decimal LineTotal, int Available);

public record CartDto(Guid CartId, List<CartItemDto> Items, int TotalQuantity, decimal TotalAmount);

public record AddCartItemRequest(string ProductCode, int Quantity = 1);

public record UpdateCartItemRequest(int Quantity);

public record OrderItemDto(string ProductCode, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal);

public record OrderDto(int OrderId, DateTime CreatedAt, List<OrderItemDto> Items, decimal TotalAmount);
