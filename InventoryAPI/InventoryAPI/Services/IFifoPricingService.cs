namespace InventoryAPI.Services
{
    public interface IFifoPricingService
    {
        Task<decimal> GetFifoUnitPriceAsync(int productId, decimal requestedQty);
    }
}
