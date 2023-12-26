namespace ReLogic.Content;

public interface IContentValidator
{
	bool AssetIsValid<T>(T content, string contentPath, out IRejectionReason rejectionReason) where T : class;
}
