namespace MiniECommerce.Api.Common.Exceptions;

public abstract class AppException : Exception
{
    protected AppException(string message) : base(message) { }
}

public class NotFoundException : AppException
{
    public NotFoundException(string entity, object key)
        : base($"{entity} bulunamadı. (Referans: {key})") { }
}

public class BadRequestException : AppException
{
    public BadRequestException(string message) : base(message) { }
}

public class InsufficientStockException : AppException
{
    public InsufficientStockException(string productName, int requested, int available)
        : base($"'{productName}' için yetersiz stok. İstenen miktar: {requested}, Mevcut stok: {available}.") { }
}

public class CouponInvalidException : AppException
{
    public CouponInvalidException(string message) : base(message) { }
}