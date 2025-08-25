namespace UtilityBillSplitter.DTOs
{
    public class PaymentCreateDto
    {
        public int UserId { get; set; }
        public int BillShareId { get; set; }
        public decimal AmountPaid { get; set; }
        public string Method { get; set; }
    }
}

