using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using VocaDb.Model.DataContracts.Artists;
using VocaDb.Model.DataContracts.PVs;
using VocaDb.Model.DataContracts.Songs;
using VocaDb.Model.DataContracts.Users;
using VocaDb.Model.Domain.Albums;
using VocaDb.Model.Domain.Globalization;
using VocaDb.Model.DataContracts.Tags;
using VocaDb.Model.Domain.Images;
using VocaDb.Model.Domain.Security;
using VocaDb.Model.Domain.Songs;
using VocaDb.Model.Domain.Tags;

namespace VocaDb.Model.DataContracts.Albums {

	[DataContract(Namespace = Schemas.VocaDb)]
	public class AlbumDetailsContract : AlbumContract {

		public AlbumDetailsContract() { }

		public AlbumDetailsContract(Album album, ContentLanguagePreference languagePreference, IUserPermissionContext userContext, IAggregatedEntryImageUrlFactory thumbPersister,
			Func<Song, SongVoteRating?> getSongRating = null, Tag discTypeTag = null)
			: base(album, languagePreference) {

			ArtistLinks = album.Artists.Select(a => new ArtistForAlbumContract(a, languagePreference)).OrderBy(a => a.Name).ToArray();
			CanEditPersonalDescription = EntryPermissionManager.CanEditPersonalDescription(userContext, album);
			CanRemoveTagUsages = EntryPermissionManager.CanRemoveTagUsages(userContext, album);
			Description = album.Description;
			Discs = album.Songs.Any(s => s.DiscNumber > 1) ? album.Discs.Select(d => new AlbumDiscPropertiesContract(d)).ToDictionary(a => a.DiscNumber) : new Dictionary<int, AlbumDiscPropertiesContract>(0);
			DiscTypeTypeTag = discTypeTag != null ? new TagBaseContract(discTypeTag, languagePreference) : null;
			OriginalRelease = (album.OriginalRelease != null ? new AlbumReleaseContract(album.OriginalRelease, languagePreference) : null);
			Pictures = album.Pictures.Select(p => new EntryPictureFileContract(p, thumbPersister)).ToArray();
			PVs = album.PVs.Select(p => new PVContract(p)).ToArray();
			Songs = album.Songs
				.OrderBy(s => s.DiscNumber).ThenBy(s => s.TrackNumber)
				.Select(s => new SongInAlbumContract(s, languagePreference, false, rating: getSongRating?.Invoke(s.Song)))
				.ToArray();
			Tags = album.Tags.ActiveUsages.Select(u => new TagUsageForApiContract(u, languagePreference)).OrderByDescending(t => t.Count).ToArray();
			WebLinks = album.WebLinks.Select(w => new WebLinkContract(w)).OrderBy(w => w.DescriptionOrUrl).ToArray();

			PersonalDescriptionText = album.PersonalDescriptionText;
			var author = album.PersonalDescriptionAuthor;
			PersonalDescriptionAuthor = author != null ? new ArtistForApiContract(author, languagePreference, thumbPersister, ArtistOptionalFields.MainPicture) : null;

			TotalLength = Songs.All(s => s.Song != null && s.Song.LengthSeconds > 0) ? TimeSpan.FromSeconds(Songs.Sum(s => s.Song.LengthSeconds)) : TimeSpan.Zero;

		}

		[DataMember]
		public AlbumForUserContract AlbumForUser { get; set; }

		[DataMember]
		public ArtistForAlbumContract[] ArtistLinks { get; set; }

		public bool CanEditPersonalDescription { get; set; }

		public bool CanRemoveTagUsages { get; set; }

		[DataMember]
		public int CommentCount { get; set; }

		[DataMember]
		public EnglishTranslatedString Description { get; set; }

		[DataMember]
		public Dictionary<int, AlbumDiscPropertiesContract> Discs { get; set; }

		[DataMember]
		public TagBaseContract DiscTypeTypeTag { get; set; }

		[DataMember]
		public int Hits { get; set; }

		[DataMember]
		public CommentForApiContract[] LatestComments { get; set; }

		[DataMember]
		public AlbumContract MergedTo { get; set; }

		[DataMember]
		public AlbumReleaseContract OriginalRelease { get; set; }

		[DataMember]
		public string PersonalDescriptionText { get; set; }

		[DataMember]
		public ArtistForApiContract PersonalDescriptionAuthor { get; set; }

		[DataMember]
		public EntryPictureFileContract[] Pictures { get; set; }

		[DataMember]
		public PVContract[] PVs { get; set; }

		[DataMember]
		public SongInAlbumContract[] Songs { get; set; }

		[DataMember]
		public SharedAlbumStatsContract Stats { get; set; }

		[DataMember]
		public TagUsageForApiContract[] Tags { get; set; }

		[DataMember]
		public TimeSpan TotalLength { get; set; }

		[DataMember]
		public WebLinkContract[] WebLinks { get; set; }

	}

	[DataContract(Namespace = Schemas.VocaDb)]
	public class SharedAlbumStatsContract {

		[DataMember]
		public AlbumReviewContract LatestReview { get; set; }

		[DataMember]
		public int LatestReviewRatingScore { get; set; }

		[DataMember]
		public int OwnedCount { get; set; }

		[DataMember]
		public int ReviewCount { get; set; }

		[DataMember]
		public int WishlistCount { get; set; }

	}

}
