namespace TrashLib.Services.Sonarr.ReleaseProfile.Guide;

public interface ISonarrGuideService
{
    IReadOnlyCollection<ReleaseProfileData> GetReleaseProfileData();
    ReleaseProfileData? GetUnfilteredProfileById(string trashId);
}
