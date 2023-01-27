namespace orders_service;

public class Order
{
    public int OrderId {get; set; }
    public int ProductId {get; set; }
    public int CustomerId {get; set; }
    public int Quantity {get; set; }
    public string OrderDate { get; set; }
    public string FulfillmentHub { get; set; }
    public string LastUpdateZone { get; set; }
    public string LastUpdateTime { get; set; }
    public string Status { get; set; }
}
