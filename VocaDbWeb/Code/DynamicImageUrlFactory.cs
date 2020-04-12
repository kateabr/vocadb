using System.Web.Mvc;
using VocaDb.Model.Domain;
using VocaDb.Model.Domain.Images;

namespace VocaDb.Web.Code {

	public class DynamicImageUrlFactory : IDynamicImageUrlFactory {

		public DynamicImageUrlFactory(UrlHelper urlHelper) {
			this.urlHelper = urlHelper;
		}

		private readonly UrlHelper urlHelper;

		public string GetRelativeDynamicUrl(IEntryImageInformation imageInfo, ImageSize size) {
			
			string dynamicUrl = null;
			if (imageInfo.EntryType == EntryType.Album) {
				if (size == ImageSize.Original)
					dynamicUrl = urlHelper.Action("CoverPicture", "Album", new { id = imageInfo.Id, v = imageInfo.Version });
				else
					dynamicUrl = urlHelper.Action("CoverPictureThumb", "Album", new { id = imageInfo.Id, v = imageInfo.Version });
			} else if (imageInfo.EntryType == EntryType.Artist) {				
				if (size == ImageSize.Original)
					dynamicUrl = urlHelper.Action("Picture", "Artist", new { id = imageInfo.Id, v = imageInfo.Version });
				else
					dynamicUrl = urlHelper.Action("PictureThumb", "Artist", new { id = imageInfo.Id, v = imageInfo.Version });
			}

			return dynamicUrl;

		}

	}

}