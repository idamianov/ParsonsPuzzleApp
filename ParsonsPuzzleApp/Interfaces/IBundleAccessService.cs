namespace ParsonsPuzzleApp.Interfaces
{
    public interface IBundleAccessService
    {
        void GrantAccess(int bundleId, string studentIdentifier);
        bool HasAccess(int bundleId, string studentIdentifier);
        void RevokeAccess(int bundleId);
        void RevokeAllAccess();
    }
}
