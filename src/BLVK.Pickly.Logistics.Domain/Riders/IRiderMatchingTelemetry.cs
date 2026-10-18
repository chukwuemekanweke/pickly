namespace BLVK.Pickly.Logistics.Domain.Riders;

public interface IRiderMatchingTelemetry
{
    void RecordProcessing(int scheduledDispatchesOpened, int offersExpired, int matchingAttempts, int offersCreated, int noCandidateAttempts);
    void RecordOfferAccepted();
    void RecordOfferRejected();
    void RecordAssignmentConflict();
    void RecordStaleLocation();
    void RecordProcessingFailures(int count);
}
