using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using dagangOnline.Application.DTOs;

namespace dagangOnline.Controllers.Api.v1;

[ApiController]
[Route("api/v1/orders")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    public class OrderItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderDto
    {
        public Guid OrderId { get; set; } = Guid.NewGuid();
        public string OrderNumber { get; set; } = $"DO-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class CreateOrderRequest
    {
        public List<OrderItemDto> Items { get; set; } = new();
        public string DeliveryAddress { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = "COD"; // COD, Transfer
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<OrderDto>>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetOrders()
    {
        // Mock sample orders for client display
        var sampleOrders = new List<OrderDto>
        {
            new OrderDto
            {
                OrderId = Guid.NewGuid(),
                OrderNumber = "DO-20260917-8821",
                TotalAmount = 250000,
                Status = "Dikirim",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto { ProductId = Guid.NewGuid(), ProductName = "Batik Tulis Tradisional", Price = 250000, Quantity = 1 }
                }
            }
        };

        return Task.FromResult<IActionResult>(Ok(ApiResponse<List<OrderDto>>.Ok(sampleOrders)));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
        {
            return Task.FromResult<IActionResult>(BadRequest(ApiResponse<OrderDto>.Fail("Pesanan harus memiliki minimal 1 produk.")));
        }

        decimal total = 0;
        foreach (var item in request.Items)
        {
            total += item.Price * item.Quantity;
        }

        var order = new OrderDto
        {
            TotalAmount = total,
            Status = "Menunggu Pembayaran",
            Items = request.Items
        };

        return Task.FromResult<IActionResult>(Ok(ApiResponse<OrderDto>.Ok(order, "Pesanan berhasil dibuat.")));
    }
}
