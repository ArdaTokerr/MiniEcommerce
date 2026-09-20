namespace MiniECommerce.Api.Models;

public enum UserRole
{
    Customer = 0,
    Admin = 1
}

public enum OrderStatus
{
    Pending = 0,
    Completed = 1,
    Cancelled = 2
}

public enum DiscountType
{
    FixedAmount = 0,
    Percentage = 1
}
