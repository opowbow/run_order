using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Spanner.Data;

namespace orders_service.Controllers;

[ApiController]
[Route("[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(ILogger<OrdersController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "GetOrders")]
    public JsonResult GetOrders()
    {
        string connectionString = $"Data Source={Environment.GetEnvironmentVariable("SPANNER_URI")}";
        using (var connection = new SpannerConnection(connectionString))
        {
            string selectSql = "SELECT * FROM Orders";
            var cmd = connection.CreateSelectCommand(selectSql);
            using (var reader = cmd.ExecuteReader())
            {
                var orders = new List<Order>();
                while (reader.Read())
                {
                    var order = new Order
                    {
                        OrderId = reader.GetFieldValue<int>(0),
                        ProductId = reader.GetFieldValue<int>(1),
                        CustomerId = reader.GetFieldValue<int>(2),
                        Quantity = reader.GetFieldValue<int>(3),
                        OrderDate = reader.GetFieldValue<string>(4),
                        FulfillmentHub = reader.GetFieldValue<string>(5),
                        LastUpdateZone = reader.GetFieldValue<string>(6),
                        LastUpdateTime = reader.GetFieldValue<string>(7),
                        Status = reader.GetFieldValue<string>(8)
                    };
                    orders.Add(order);
                }
                return new JsonResult(orders);
            }
        }
    }

    private Boolean CreateOrder(SpannerConnection connection, Order order)
    {
        //order.OrderId = Guid.NewGuid();

        Random rand = new Random();
        order.OrderId = rand.Next(100,99999999);

        var ordersCommand = connection.CreateInsertCommand("Orders",
            new SpannerParameterCollection {
                { "OrderId", SpannerDbType.Int64, order.OrderId },
                { "ProductId", SpannerDbType.Int64, order.ProductId },
                { "CustomerId", SpannerDbType.Int64, order.CustomerId },
                { "Quantity", SpannerDbType.Int64, order.Quantity },
                { "OrderDate", SpannerDbType.String, DateTime.UtcNow.ToString("o") },
                { "FulfillmentHub", SpannerDbType.String, order.FulfillmentHub },
                { "LastUpdateZone", SpannerDbType.String, order.LastUpdateZone },
                { "LastUpdateTime", SpannerDbType.Timestamp, SpannerParameter.CommitTimestamp },
                { "Status", SpannerDbType.String, "CREATED" }
            }
        );

        var historyCommand = connection.CreateInsertCommand("OrdersHistory",
                new SpannerParameterCollection {
                { "OrderId", SpannerDbType.Int64, order.OrderId },
                { "ProductId", SpannerDbType.Int64, order.ProductId },
                { "CustomerId", SpannerDbType.Int64, order.CustomerId },
                { "Quantity", SpannerDbType.Int64, order.Quantity },
                { "OrderDate", SpannerDbType.String, DateTime.UtcNow.ToString("o") },
                { "FulfillmentHub", SpannerDbType.String, order.FulfillmentHub },
                { "LastUpdateZone", SpannerDbType.String, order.LastUpdateZone },
                { "TimeStamp", SpannerDbType.Timestamp, SpannerParameter.CommitTimestamp },
                { "Status", SpannerDbType.String, "CREATED" }
            }
        );

        var result = false;

        connection.RunWithRetriableTransaction(transaction =>
        {
            ordersCommand.Transaction = transaction;
            ordersCommand.ExecuteNonQuery();

            historyCommand.Transaction = transaction;
            historyCommand.ExecuteNonQuery();

            result = true;
        });
        return result;
    }

    private Boolean UpdateOrderStatus(SpannerConnection connection, Int64 orderId)
    {
        var ordersCommand = connection.CreateUpdateCommand("Orders",
                new SpannerParameterCollection {
                { "OrderId", SpannerDbType.Int64, orderId },
                { "LastUpdateTime", SpannerDbType.Timestamp, SpannerParameter.CommitTimestamp },
                { "Status", SpannerDbType.String, "PENDING_DESPATCH" }
            }
        );

        var historyCommand = connection.CreateInsertCommand("OrdersHistory",
                new SpannerParameterCollection {
                { "OrderId", SpannerDbType.Int64, orderId },
                { "TimeStamp", SpannerDbType.Timestamp, SpannerParameter.CommitTimestamp },
                { "Status", SpannerDbType.String, "PENDING_DESPATCH" }
            }
        );

        var result = false;

        connection.RunWithRetriableTransaction(transaction =>
        {
            ordersCommand.Transaction = transaction;
            ordersCommand.ExecuteNonQuery();

            historyCommand.Transaction = transaction;
            historyCommand.ExecuteNonQuery();

            result = true;
        });
        return result;
    }

    [HttpPost(Name = "PostOrder")]
    public JsonResult PostOrder([FromBody] Order order)
    {
        string connectionString = $"Data Source={Environment.GetEnvironmentVariable("SPANNER_URI")}";
        using (var connection = new SpannerConnection(connectionString))
        {
            var result = CreateOrder(connection, order);
            return new JsonResult(result);
        }
    }

    [HttpPut(Name = "PutOrder")]
    [Route("{orderId}")]
    public JsonResult PutOrder(Int64 orderId, [FromBody] Order order)
    {
        string connectionString = $"Data Source={Environment.GetEnvironmentVariable("SPANNER_URI")}";
        using (var connection = new SpannerConnection(connectionString))
        {
            var result = UpdateOrderStatus(connection, orderId);
            return new JsonResult(result);
        }
    }
}
