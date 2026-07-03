using System.ComponentModel.DataAnnotations;

namespace TmsApi;

public class PaymentOptions
{
    // TODO 1: Required external payment vendor access route link
    [Required(ErrorMessage = "The GatewayUrl field is required.")]
    public required string GatewayUrl { get; init; }

    // TODO 1: Transaction range parameters enforced for tuition snapshots (In Ethiopian Birr / ETB)
    [Range(100, 100000, ErrorMessage = "MaxDepositBirr must remain within boundaries of 100 and 100,000 Birr.")]
    public decimal MaxDepositBirr { get; init; }
}
