public class PurchaseInvoiceUploadDto
{
    public string Invoice { get; set; } = string.Empty;
    public List<IFormFile> Files { get; set; } = new();
}
