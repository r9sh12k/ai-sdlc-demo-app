namespace InvoiceApp.Domain;

public class ApprovalDeniedException : Exception
{
    public ApprovalDeniedException(string reason) : base(reason)
    {
    }
}
