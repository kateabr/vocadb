using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using VocaDb.Model;
using VocaDb.Model.Database.Queries;
using VocaDb.Model.DataContracts;
using VocaDb.Model.DataContracts.Aggregate;
using VocaDb.Model.DataContracts.PVs;
using VocaDb.Model.DataContracts.Songs;
using VocaDb.Model.DataContracts.Tags;
using VocaDb.Model.DataContracts.UseCases;
using VocaDb.Model.DataContracts.Users;
using VocaDb.Model.Domain;
using VocaDb.Model.Domain.Globalization;
using VocaDb.Model.Domain.PVs;
using VocaDb.Model.Domain.Security;
using VocaDb.Model.Domain.Songs;
using VocaDb.Model.Service;
using VocaDb.Model.Service.QueryableExtenders;
using VocaDb.Model.Service.Search;
using VocaDb.Model.Service.Search.AlbumSearch;
using VocaDb.Model.Service.Search.SongSearch;
using VocaDb.Model.Service.VideoServices;
using VocaDb.Web.Code.Exceptions;
using VocaDb.Web.Code.WebApi;
using WebApi.OutputCache.V2;

namespace VocaDb.Web.Controllers.Api {

	/// <summary>
	/// API queries for songs.
	/// </summary>
	[System.Web.Http.RoutePrefix("api/songs")]
	public class SongApiController : ApiController {

		private const int hourInSeconds = 3600;
		private const int absoluteMax = 100;
		private const int defaultMax = 10;
		private readonly IEntryLinkFactory entryLinkFactory;
		private readonly OtherService otherService;
		private readonly SongQueries queries;
		private readonly SongService service;
		private readonly SongAggregateQueries songAggregateQueries;
		private readonly UserService userService;
		private readonly IUserPermissionContext userPermissionContext;

		/// <summary>
		/// Initializes controller.
		/// </summary>
		public SongApiController(SongService service, SongQueries queries, SongAggregateQueries songAggregateQueries, 
			IEntryLinkFactory entryLinkFactory, IUserPermissionContext userPermissionContext, 
			UserService userService, OtherService otherService) {

			this.service = service;
			this.queries = queries;
			this.userService = userService;
			this.songAggregateQueries = songAggregateQueries;
			this.entryLinkFactory = entryLinkFactory;
			this.userPermissionContext = userPermissionContext;
			this.otherService = otherService;

		}

		/// <summary>
		/// Deletes a comment.
		/// </summary>
		/// <param name="commentId">ID of the comment to be deleted.</param>
		/// <remarks>
		/// Normal users can delete their own comments, moderators can delete all comments.
		/// Requires login.
		/// </remarks>
		[System.Web.Http.Route("comments/{commentId:int}")]
		[System.Web.Http.Authorize]
		public void DeleteComment(int commentId) => queries.DeleteComment(commentId);

		/// <summary>
		/// Deletes a song.
		/// </summary>
		/// <param name="id">ID of the song to be deleted.</param>
		/// <param name="notes">Notes.</param>
		[Route("{id:int}")]
		[Authorize]
		public void Delete(int id, string notes = "") => service.Delete(id, notes ?? string.Empty);

		/// <summary>
		/// Gets a list of comments for a song.
		/// </summary>
		/// <param name="id">ID of the song whose comments to load.</param>
		/// <returns>List of comments in no particular order.</returns>
		/// <remarks>
		/// Pagination and sorting might be added later.
		/// </remarks>
		[System.Web.Http.Route("{id:int}/comments")]
		public IEnumerable<CommentForApiContract> GetComments(int id) => queries.GetComments(id);

		[System.Web.Http.Route("findDuplicate")]
		[ApiExplorerSettings(IgnoreApi = true)]
		[System.Web.Http.HttpGet]
		public async Task<NewSongCheckResultContract> GetFindDuplicate([FromUri] string[] term = null, [FromUri] string[] pv = null, [FromUri] int[] artistIds = null, bool getPVInfo = false)
			=> await queries.FindDuplicates(
				(term ?? new string[0]).Where(p => p != null).ToArray(),
				(pv ?? new string[0]).Where(p => p != null).ToArray(),
				artistIds, getPVInfo);

		/// <summary>
		/// Gets derived (alternate versions) of a song.
		/// </summary>
		/// <param name="id">Song Id (required).</param>
		/// <param name="fields">
		/// List of optional fields (optional). 
		/// Possible values are Albums, Artists, Names, PVs, Tags, ThumbUrl, WebLinks.
		/// </param>
		/// <param name="lang">Content language preference (optional).</param>
		/// <example>https://vocadb.net/api/songs/121/derived</example>
		/// <returns>List of derived songs.</returns>
		/// <remarks>
		/// Pagination and sorting might be added later.
		/// </remarks>
		[System.Web.Http.Route("{id:int}/derived")]
		public IEnumerable<SongForApiContract> GetDerived(int id, SongOptionalFields fields = SongOptionalFields.None,
			ContentLanguagePreference lang = ContentLanguagePreference.Default) => queries.GetDerived(id, fields, lang);

		[System.Web.Http.Route("{id:int}/for-edit")]
		[ApiExplorerSettings(IgnoreApi=true)]
		public SongForEditContract GetForEdit(int id) => queries.GetSongForEdit(id);

		/// <summary>
		/// Gets a song by Id.
		/// </summary>
		/// <param name="id">Song Id (required).</param>
		/// <param name="fields">
		/// List of optional fields (optional). 
		/// Possible values are Albums, Artists, Names, PVs, Tags, ThumbUrl, WebLinks.
		/// </param>
		/// <param name="lang">Content language preference (optional).</param>
		/// <example>http://vocadb.net/api/songs/121</example>
		/// <returns>Song data.</returns>
		[System.Web.Http.Route("{id:int}")]
		public SongForApiContract GetById(int id, SongOptionalFields fields = SongOptionalFields.None, ContentLanguagePreference lang = ContentLanguagePreference.Default) => queries.GetSongForApi(id, fields, lang);

		/// <summary>
		/// Gets list of highlighted songs, same as front page.
		/// </summary>
		/// <remarks>
		/// Output is cached for 1 hour.
		/// </remarks>
		[Route("highlighted")]
		[CacheOutput(ClientTimeSpan = hourInSeconds, ServerTimeSpan = hourInSeconds)]
		public async Task<IEnumerable<SongForApiContract>> GetHighlightedSongs(
			ContentLanguagePreference languagePreference = ContentLanguagePreference.Default, 
			SongOptionalFields fields = SongOptionalFields.None) => await otherService.GetHighlightedSongs(languagePreference, fields);

		/// <summary>
		/// Get ratings for a song.
		/// </summary>
		/// <param name="id">Song ID.</param>
		/// <param name="userFields">Optional fields for the users.</param>
		/// <param name="lang">Content language preference.</param>
		/// <returns>List of ratings.</returns>
		/// <remarks>
		/// The result includes ratings and user information.
		/// For users who have requested not to make their ratings public, the user will be empty.
		/// </remarks>
		[Route("{id:int}/ratings")]
		public IEnumerable<RatedSongForUserForApiContract> GetRatings(int id, UserOptionalFields userFields, 
			ContentLanguagePreference lang = ContentLanguagePreference.Default) => queries.GetRatings(id, userFields, lang);

		/// <summary>
		/// Add or update rating for a specific song, for the currently logged in user.
		/// </summary>
		/// <param name="id">ID of the song to be rated.</param>
		/// <param name="rating">Rating to be given. Possible values are Nothing, Like, Favorite.</param>
		/// <remarks>
		/// If the user has already rated the song, any previous rating is replaced.
		/// Authorization cookie must be included.
		/// This API supports CORS.
		/// </remarks>
		[Route("{id:int}/ratings")]
		[Authorize]
		[AuthenticatedCorsApi(System.Web.Mvc.HttpVerbs.Post)]
		public void PostRating(int id, SongRatingContract rating) => userService.UpdateSongRating(userPermissionContext.LoggedUserId, id, rating.Rating);

		[Route("by-names")]
		[ApiExplorerSettings(IgnoreApi = true)]
		public SongForApiContract[] GetByNames([FromUri] string[] names, [FromUri] int[] ignoreIds, ContentLanguagePreference lang, [FromUri] SongType[] songTypes = null, int maxResults = 3)
			=> queries.GetByNames(names, songTypes, ignoreIds, lang, maxResults);

		/// <summary>
		/// Gets related songs.
		/// </summary>
		/// <param name="id">Song whose related songs are to be queried.</param>
		/// <param name="fields">Optional song fields.</param>
		/// <param name="lang">Content language preference.</param>
		/// <returns>Related songs.</returns>
		[Route("{id:int}/related")]
		public RelatedSongsContract GetRelated(int id, SongOptionalFields fields = SongOptionalFields.None, ContentLanguagePreference lang = ContentLanguagePreference.Default) => queries.GetRelatedSongs(id, fields, lang);

		/// <summary>
		/// Find songs.
		/// </summary>
		/// <param name="query">Song name query (optional).</param>
		/// <param name="songTypes">
		/// Filtered song types (optional). 
		/// Possible values are Original, Remaster, Remix, Cover, Instrumental, Mashup, MusicPV, DramaPV, Other.
		/// </param>
		/// <param name="afterDate">Filter by songs published after this date (inclusive).</param>
		/// <param name="beforeDate">Filter by songs published before this date (exclusive).</param>
		/// <param name="tagName">Filter by one or more tag names (optional).</param>
		/// <param name="tagId">Filter by one or more tag Ids (optional).</param>
		/// <param name="childTags">Include child tags, if the tags being filtered by have any.</param>
		/// <param name="unifyTypesAndTags">When searching by song type, search by associated tag as well, and vice versa.</param>
		/// <param name="artistId">Filter by artist Id.</param>
		/// <param name="artistParticipationStatus">
		/// Filter by artist participation status. Only valid if artistId is specified.
		/// Everything (default): Show all songs by that artist (no filter).
		/// OnlyMainAlbums: Show only main songs by that artist.
		/// OnlyCollaborations: Show only collaborations by that artist.
		/// </param>
		/// <param name="childVoicebanks">Include child voicebanks, if the artist being filtered by has any.</param>
		/// <param name="includeMembers">Include members of groups. This applies if <paramref name="artistId"/> is a group.</param>
		/// <param name="onlyWithPvs">Whether to only include songs with at least one PV.</param>
		/// <param name="pvServices">Filter by one or more PV services (separated by commas). The song will pass the filter if it has a PV for any of the matched services.</param>
		/// <param name="since">Allow only entries that have been created at most this many hours ago. By default there is no filtering.</param>
		/// <param name="minScore">Minimum rating score. Optional.</param>
		/// <param name="userCollectionId">Filter by user's rated songs. By default there is no filtering.</param>
		/// <param name="releaseEventId">Filter by release event. By default there is no filtering.</param>
		/// <param name="parentSongId">Filter by parent song. By default there is no filtering.</param>
		/// <param name="status">Filter by entry status (optional).</param>
		/// <param name="advancedFilters">List of advanced filters (optional).</param>
		/// <param name="start">First item to be retrieved (optional, defaults to 0).</param>
		/// <param name="maxResults">Maximum number of results to be loaded (optional, defaults to 10, maximum of 50).</param>
		/// <param name="getTotalCount">Whether to load total number of items (optional, default to false).</param>
		/// <param name="sort">Sort rule (optional, defaults to Name). Possible values are None, Name, AdditionDate, FavoritedTimes, RatingScore.</param>
		/// <param name="preferAccurateMatches">
		/// Whether the search should prefer accurate matches. 
		/// If this is true, entries that match by prefix will be moved first, instead of being sorted alphabetically.
		/// Requires a text query. Does not support pagination.
		/// This is mostly useful for autocomplete boxes.
		/// </param>
		/// <param name="nameMatchMode">Match mode for song name (optional, defaults to Exact).</param>
		/// <param name="fields">
		/// List of optional fields (optional). Possible values are Albums, Artists, Names, PVs, Tags, ThumbUrl, WebLinks.
		/// </param>
		/// <param name="lang">Content language preference (optional).</param>
		/// <returns>Page of songs.</returns>
		/// <example>http://vocadb.net/api/songs?query=Nebula&amp;songTypes=Original</example>
		[System.Web.Http.Route("")]
		public PartialFindResult<SongForApiContract> GetList(
			string query = "", 
			string songTypes = null,
			DateTime? afterDate = null,
			DateTime? beforeDate = null,
			[FromUri] string[] tagName = null,
			[FromUri] int[] tagId = null,
			bool childTags = false,
			bool unifyTypesAndTags = false,
			[FromUri] int[] artistId = null,
			ArtistAlbumParticipationStatus artistParticipationStatus = ArtistAlbumParticipationStatus.Everything,
			bool childVoicebanks = false,
			bool includeMembers = false,
			bool onlyWithPvs = false,
			[FromUri] PVServices? pvServices = null,
			int? since = null,
			int? minScore = null,
			int? userCollectionId = null,
			int? releaseEventId = null,
			int? parentSongId = null,
			EntryStatus? status = null,
			[FromUri] AdvancedSearchFilter[] advancedFilters = null,
			int start = 0, int maxResults = defaultMax, bool getTotalCount = false,
			SongSortRule sort = SongSortRule.Name,
			bool preferAccurateMatches = false,
			NameMatchMode nameMatchMode = NameMatchMode.Exact,
			SongOptionalFields fields = SongOptionalFields.None, 
			ContentLanguagePreference lang = ContentLanguagePreference.Default) {

			var textQuery = SearchTextQuery.Create(query, nameMatchMode);
			var types = EnumVal<SongType>.ParseMultiple(songTypes);

			var param = new SongQueryParams(textQuery, types, start, Math.Min(maxResults, absoluteMax), getTotalCount, sort, false, preferAccurateMatches, null) {
				ArtistParticipation = {
					ArtistIds = artistId,
					Participation = artistParticipationStatus,
					ChildVoicebanks = childVoicebanks,
					IncludeMembers = includeMembers
				},
                AfterDate = afterDate,
                BeforeDate = beforeDate,
				TagIds = tagId,
				Tags = tagName, 
				ChildTags = childTags,
				UnifyEntryTypesAndTags = unifyTypesAndTags,
				OnlyWithPVs = onlyWithPvs,
				TimeFilter = since.HasValue ? TimeSpan.FromHours(since.Value) : TimeSpan.Zero,
				MinScore = minScore ?? 0,
				PVServices = pvServices,
				UserCollectionId = userCollectionId ?? 0,
				ReleaseEventId = releaseEventId ?? 0,
				ParentSongId = parentSongId ?? 0,
				AdvancedFilters = advancedFilters,
				LanguagePreference = lang
			};
			param.Common.EntryStatus = status;

			var artists = service.Find(s => new SongForApiContract(s, null, lang, fields), param);

			return artists;			

		}

		/// <summary>
		/// Gets lyrics by ID.
		/// </summary>
		/// <param name="lyricsId">Lyrics ID.</param>
		/// <returns>Lyrics information.</returns>
		/// <remarks>
		/// Output is cached. Specify song version as parameter to refresh.
		/// </remarks>
		[Route("lyrics/{lyricsId:int}")]
		[CacheOutput(ClientTimeSpan = 3600, ServerTimeSpan = 3600)]
		public LyricsForSongContract GetLyrics(int lyricsId) => queries.GetLyrics(lyricsId);

		/// <summary>
		/// Gets a list of song names. Ideal for autocomplete boxes.
		/// </summary>
		/// <param name="query">Text query.</param>
		/// <param name="nameMatchMode">Name match mode.</param>
		/// <param name="maxResults">Maximum number of results.</param>
		/// <returns>List of song names.</returns>
		[System.Web.Http.Route("names")]
		public string[] GetNames(string query = "", NameMatchMode nameMatchMode = NameMatchMode.Auto, int maxResults = 15) => service.FindNames(SearchTextQuery.Create(query, nameMatchMode), maxResults);

		/// <summary>
		/// Gets a song by PV.
		/// </summary>
		/// <param name="pvService">
		/// PV service (required). Possible values are NicoNicoDouga, Youtube, SoundCloud, Vimeo, Piapro, Bilibili.
		/// </param>
		/// <param name="pvId">PV Id (required). For example sm123456.</param>
		/// <param name="fields">
		/// List of optional fields (optional). Possible values are Albums, Artists, Names, PVs, Tags, ThumbUrl, WebLinks.
		/// </param>
		/// <param name="lang">Content language preference (optional).</param>
		/// <returns>Song data.</returns>
		/// <example>http://vocadb.net/api/songs?pvId=sm19923781&amp;pvService=NicoNicoDouga</example>
		[System.Web.Http.Route("")]
		[System.Web.Http.Route("byPv")]
		public SongForApiContract GetByPV(
			PVService pvService,
			string pvId,
			SongOptionalFields fields = SongOptionalFields.None,
			ContentLanguagePreference lang = ContentLanguagePreference.Default) => service.GetSongWithPV(s => new SongForApiContract(s, null, lang, fields), pvService, pvId);

		[System.Web.Http.Route("ids")]
		[ApiExplorerSettings(IgnoreApi = true)]
		public IEnumerable<int> GetIds() => queries.GetIds();

		[System.Web.Http.Route("{id:int}/pvs")]
		[ApiExplorerSettings(IgnoreApi = true)]
		public string GetPVId(int id, PVService service) => queries.PVForSongAndService(id, service).PVId;

		[Route("over-time")]
		[ApiExplorerSettings(IgnoreApi = true)]
		[CacheOutput(ClientTimeSpan = 600, ServerTimeSpan = 600)]
		public CountPerDayContract[] GetSongsOverTime(TimeUnit timeUnit, int artistId = 0, int tagId = 0) => songAggregateQueries.SongsOverTime(timeUnit, true, null, artistId, tagId);

		[ApiExplorerSettings(IgnoreApi = true)]
		[Route("{id:int}/tagSuggestions")]
		public async Task<IEnumerable<TagUsageForApiContract>> GetTagSuggestions(int id) => await queries.GetTagSuggestionsAsync(id);

		/// <summary>
		/// Gets top rated songs.
		/// </summary>
		/// <param name="durationHours">Duration in hours from which to get songs.</param>
		/// <param name="startDate">Lower bound of the date. Optional.</param>
		/// <param name="filterBy">Filtering mode.</param>
		/// <param name="vocalist">Vocalist selection.</param>
		/// <param name="maxResults">Maximum number of results to be loaded (optional).</param>
		/// <param name="fields">Optional song fields to load.</param>
		/// <param name="languagePreference">Language preference.</param>
		/// <returns>List of sorts, sorted by the rating position.</returns>
		[System.Web.Http.Route("top-rated")]
		[CacheOutput(ClientTimeSpan = 600, ServerTimeSpan = 600)]
		public async Task<IEnumerable<SongForApiContract>> GetTopSongs(
			int? durationHours = null, 
			DateTime? startDate = null,
			TopSongsDateFilterType? filterBy = null,
			SongVocalistSelection? vocalist = null,
			int maxResults = 25,
			SongOptionalFields fields = SongOptionalFields.None,
			ContentLanguagePreference languagePreference = ContentLanguagePreference.Default) => await queries.GetTopRated(durationHours, startDate, filterBy, vocalist, maxResults, fields, languagePreference);

		/// <summary>
		/// Updates a comment.
		/// </summary>
		/// <param name="commentId">ID of the comment to be edited.</param>
		/// <param name="contract">New comment data. Only message can be edited.</param>
		/// <remarks>
		/// Normal users can edit their own comments, moderators can edit all comments.
		/// Requires login.
		/// </remarks>
		[System.Web.Http.Route("comments/{commentId:int}")]
		[System.Web.Http.Authorize]
		public void PostEditComment(int commentId, CommentForApiContract contract) => queries.PostEditComment(commentId, contract);

		/// <summary>
		/// Posts a new comment.
		/// </summary>
		/// <param name="id">ID of the song for which to create the comment.</param>
		/// <param name="contract">Comment data. Message and author must be specified. Author must match the logged in user.</param>
		/// <returns>Data for the created comment. Includes ID and timestamp.</returns>
		[System.Web.Http.Route("{id:int}/comments")]
		[System.Web.Http.Authorize]
		public CommentForApiContract PostNewComment(int id, CommentForApiContract contract) => queries.CreateComment(id, contract);

		[Route("")]
		[Authorize]
		[HttpPost]
		[ApiExplorerSettings(IgnoreApi = true)]
		[AuthenticatedCorsApi(System.Web.Mvc.HttpVerbs.Post)]
		public Task<SongContract> PostNewSong(CreateSongContract contract) {

			if (contract == null)
				throw new HttpBadRequestException("Message was empty");

			try {
				return queries.Create(contract);
			} catch (VideoParseException x) {
				throw new HttpBadRequestException(x.Message);
			} catch (ArgumentException x) {
				throw new HttpBadRequestException(x.Message);
			}

		}

		[System.Web.Http.Route("{id:int}/pvs")]
		[System.Web.Http.Authorize]
		[ApiExplorerSettings(IgnoreApi = true)]
		public async Task PostPVs(int id, PVContract[] pvs) => await queries.PostPVs(id, pvs);

		[Route("{id:int}/personal-description")]
		[ApiExplorerSettings(IgnoreApi = true)]
		[Authorize]
		public void PostPersonalDescription(int id, SongDetailsContract data) => queries.UpdatePersonalDescription(id, data);

	}

}