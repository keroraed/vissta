namespace VISSTA.Web.Models;

public static class PaymentSettingKeys
{
    public const string InstaPayPhoneNumber = "InstaPayPhoneNumber";
    public const string VodafoneCashPhoneNumber = "VodafoneCashPhoneNumber";
    public const string OrangeCashPhoneNumber = "OrangeCashPhoneNumber";
    public const string EtisalatCashPhoneNumber = "EtisalatCashPhoneNumber";
    public const string WePayPhoneNumber = "WePayPhoneNumber";

    public static readonly string[] ManualPaymentPhoneNumbers =
    [
        InstaPayPhoneNumber,
        VodafoneCashPhoneNumber,
        OrangeCashPhoneNumber,
        EtisalatCashPhoneNumber,
        WePayPhoneNumber
    ];
}
