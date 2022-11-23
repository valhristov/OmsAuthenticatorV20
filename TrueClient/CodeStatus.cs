namespace TrueClient
{
    public enum CodeStatus
    {
        NOT_FOUND,
        // These are statuses returned by TRUE API
        EMITTED,
        APPLIED,
        INTRODUCED, // Sent to market
        WRITTEN_OFF,
        RETIRED,
        WITHDRAWN,
        DISAGGREGATION,
        DISAGGREGATED,
        APPLIED_NOT_PAID,
    }
}
