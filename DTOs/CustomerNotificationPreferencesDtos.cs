namespace Backend.DTOs
{
    public class CustomerNotificationPreferencesResponseDto
    {
        public int CustomerId { get; set; }
        public bool InquiryUpdates { get; set; }
        public bool PriceChanges { get; set; }
    }

    public class UpdateCustomerNotificationPreferencesRequestDto
    {
        public bool InquiryUpdates { get; set; }
        public bool PriceChanges { get; set; }
    }
}
