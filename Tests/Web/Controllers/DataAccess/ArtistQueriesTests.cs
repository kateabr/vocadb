using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Runtime.Caching;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VocaDb.Model.Database.Queries;
using VocaDb.Model.DataContracts;
using VocaDb.Model.DataContracts.Artists;
using VocaDb.Model.DataContracts.UseCases;
using VocaDb.Model.Domain;
using VocaDb.Model.Domain.Activityfeed;
using VocaDb.Model.Domain.Artists;
using VocaDb.Model.Domain.ExtLinks;
using VocaDb.Model.Domain.Globalization;
using VocaDb.Model.Domain.Images;
using VocaDb.Model.Domain.Security;
using VocaDb.Model.Domain.Users;
using VocaDb.Model.Resources.Messages;
using VocaDb.Tests.TestData;
using VocaDb.Tests.TestSupport;
using VocaDb.Web.Helpers;

namespace VocaDb.Tests.Web.Controllers.DataAccess {

	/// <summary>
	/// Tests for <see cref="ArtistQueries"/>.
	/// </summary>
	[TestClass]
	public class ArtistQueriesTests {
		
		private Artist artist;
		private CreateArtistContract newArtistContract;
		private InMemoryImagePersister imagePersister;
		private FakePermissionContext permissionContext;
		private ArtistQueries queries;
		private FakeArtistRepository repository;
		/// <summary>
		/// Logged in user
		/// </summary>
		private User user; 
		private User user2;
		private Artist vocalist;

		private async Task<ArtistForEditContract> CallUpdate(ArtistForEditContract contract) {
			contract.Id = await queries.Update(contract, null, permissionContext);
			return contract;
		}

		private async Task<ArtistForEditContract> CallUpdate(Stream image, string mime = MediaTypeNames.Image.Jpeg) {
			var contract = new ArtistForEditContract(artist, ContentLanguagePreference.English, new InMemoryImagePersister());
			using (var stream = image) {
				contract.Id = await queries.Update(contract, new EntryPictureFileContract(stream, mime, (int)stream.Length, ImagePurpose.Main), permissionContext);
			}		
			return contract;
		}

		[TestInitialize]
		public void SetUp() {

			artist = CreateEntry.Producer(name: "Tripshots");
			vocalist = CreateEntry.Vocalist(name: "Hatsune Miku");
			repository = new FakeArtistRepository(artist, vocalist);
			var weblink = new ArtistWebLink(artist, "Website", "http://tripshots.net", WebLinkCategory.Official);
			artist.WebLinks.Add(weblink);
			repository.Save(weblink);
			repository.SaveNames(artist, vocalist);

			user = CreateEntry.User(name: "Miku", group: UserGroupId.Moderator);
			user2 = CreateEntry.User(name: "Rin", group: UserGroupId.Regular);
			repository.Save(user);
			permissionContext = new FakePermissionContext(user);
			imagePersister = new InMemoryImagePersister();

			queries = new ArtistQueries(repository, permissionContext, new FakeEntryLinkFactory(), imagePersister, imagePersister, MemoryCache.Default, 
				new FakeUserIconFactory(), new EnumTranslations(), imagePersister);

			newArtistContract = new CreateArtistContract {
				ArtistType = ArtistType.Producer,
				Description = string.Empty,
				Names = new[] {
					new LocalizedStringContract("Tripshots", ContentLanguageSelection.English)
				},
				WebLink = new WebLinkContract("http://tripshots.net/", "Website", WebLinkCategory.Official)
			};

		}

		private (bool created, ArtistReport report) CallCreateReport(ArtistReportType reportType, int? versionNumber = null, Artist artist = null) {
			artist ??= this.artist;
			var result = queries.CreateReport(artist.Id, reportType, "39.39.39.39", "It's Miku, not Rin", versionNumber);
			var report = repository.Load<ArtistReport>(result.reportId);
			return (result.created, report);
		}

		[TestMethod]
		public async Task Create() {

			var result = await queries.Create(newArtistContract);

			Assert.IsNotNull(result, "result");
			Assert.AreEqual("Tripshots", result.Name, "Name");

			artist = repository.Load(result.Id);

			Assert.IsNotNull(artist, "Artist was saved to repository");
			Assert.AreEqual("Tripshots", artist.DefaultName, "Name");
			Assert.AreEqual(ContentLanguageSelection.English, artist.Names.SortNames.DefaultLanguage, "Default language should be English");
			Assert.AreEqual(1, artist.WebLinks.Count, "Weblinks count");

			var archivedVersion = repository.List<ArchivedArtistVersion>().FirstOrDefault();

			Assert.IsNotNull(archivedVersion, "Archived version was created");
			Assert.AreEqual(artist, archivedVersion.Artist, "Archived version artist");
			Assert.AreEqual(ArtistArchiveReason.Created, archivedVersion.Reason, "Archived version reason");

			var activityEntry = repository.List<ActivityEntry>().FirstOrDefault();

			Assert.IsNotNull(activityEntry, "Activity entry was created");
			Assert.AreEqual(artist, activityEntry.EntryBase, "Activity entry's entry");
			Assert.AreEqual(EntryEditEvent.Created, activityEntry.EditEvent, "Activity entry event type");

		}

		[TestMethod]
		[ExpectedException(typeof(NotAllowedException))]
		public async Task Create_NoPermission() {

			user.GroupId = UserGroupId.Limited;
			permissionContext.RefreshLoggedUser(repository);

			await queries.Create(newArtistContract);

		}

		[TestMethod]
		public void CreateReport() {
			
			var editor = user2;
			repository.Save(ArchivedArtistVersion.Create(artist, new ArtistDiff(), new AgentLoginData(editor), ArtistArchiveReason.PropertiesUpdated, string.Empty));
			var (created, report) = CallCreateReport(ArtistReportType.InvalidInfo);

			created.Should().BeTrue("Report was created");
			report.EntryBase.Id.Should().Be(artist.Id);
			report.User.Should().Be(user);
			report.ReportType.Should().Be(ArtistReportType.InvalidInfo);

			var notification = repository.List<UserMessage>().FirstOrDefault();
			notification.Should().NotBeNull("notification was created");
			notification.Receiver.Should().Be(editor, "notification receiver is editor");
			notification.Subject.Should().Be(string.Format(EntryReportStrings.EntryVersionReportTitle, artist.DefaultName));

		}

		[TestMethod]
		public void CreateReport_OwnershipClaim() {
			
			var editor = user2;
			repository.Save(ArchivedArtistVersion.Create(artist, new ArtistDiff(), new AgentLoginData(editor), ArtistArchiveReason.PropertiesUpdated, string.Empty));
			var (created, _) = CallCreateReport(ArtistReportType.OwnershipClaim);

			created.Should().BeTrue("Report was created");

			repository.List<UserMessage>().Should().BeEmpty("No notification created");

		}

		[TestMethod]
		public void FindDuplicates_Name() {

			var result = queries.FindDuplicates(new[] { artist.DefaultName }, string.Empty);

			Assert.IsNotNull(result, "result");
			Assert.AreEqual(1, result.Length, "Number of results");
			Assert.AreEqual(artist.Id, result[0].Id, "Matched artist");

		}

		[TestMethod]
		public void FindDuplicates_Link() {

			var result = queries.FindDuplicates(new string[0], "http://tripshots.net");

			Assert.IsNotNull(result, "result");
			Assert.AreEqual(1, result.Length, "Number of results");
			Assert.AreEqual(artist.Id, result[0].Id, "Matched artist");

		}

		[TestMethod]
		public void FindDuplicates_DifferentScheme() {

			var result = queries.FindDuplicates(new string[0], "https://tripshots.net");

			Assert.IsNotNull(result, "result");
			Assert.AreEqual(1, result.Length, "Number of results");
			Assert.AreEqual(artist.Id, result[0].Id, "Matched artist");

		}

		[TestMethod]
		public void FindDuplicates_IgnoreNullsAndEmpty() {

			var result = queries.FindDuplicates(new[] { null, string.Empty }, string.Empty);

			Assert.IsNotNull(result, "result");
			Assert.AreEqual(0, result.Length, "Number of results");

		}

		[TestMethod]
		public void FindDuplicates_Link_IgnoreDeleted() {

			artist.Deleted = true;
			var result = queries.FindDuplicates(new string[0], "http://tripshots.net");

			Assert.IsNotNull(result, "result");
			Assert.AreEqual(0, result.Length, "Number of results");

		}

		[TestMethod]
		public void FindDuplicates_Link_IgnoreInvalidLink() {

			var result = queries.FindDuplicates(new string[0], "Miku!");
			Assert.AreEqual(0, result?.Length, "Number of results");

		}

		[TestMethod]
		public async Task GetCoverPictureThumb() {
			
			var contract = await CallUpdate(ResourceHelper.TestImage());
			contract.PictureMime = "image/jpeg";

			var result = queries.GetPictureThumb(contract.Id);

			Assert.IsNotNull(result, "result");
			Assert.IsNotNull(result.Picture, "Picture");
			Assert.IsNotNull(result.Picture.Bytes, "Picture content");
			Assert.AreEqual(contract.PictureMime, result.Picture.Mime, "Picture MIME");
			Assert.AreEqual(contract.Id, result.EntryId, "EntryId");

		}

		[TestMethod]
		public void GetDetails() {

			var result = queries.GetDetails(artist.Id, "39.39.39.39");

			Assert.AreEqual("Tripshots", result.Name, "Name");

			var hit = repository.List<ArtistHit>().FirstOrDefault(a => a.Entry.Equals(artist));
			Assert.IsNotNull(hit, "Hit was created");
			Assert.AreEqual(user.Id, hit.Agent, "Hit creator");

		}

		[TestMethod]
		public void GetTagSuggestions() {

			var song = repository.Save(CreateEntry.Song());

		}

		[TestMethod]
		public async Task Revert() {

			// Arrange
			artist.Description.English = "Original";
			var oldVer = await repository.HandleTransactionAsync(ctx => queries.ArchiveAsync(ctx, artist, ArtistArchiveReason.PropertiesUpdated));
			var contract = new ArtistForEditContract(artist, ContentLanguagePreference.English, new InMemoryImagePersister());
			contract.Description.English = "Updated";
			await CallUpdate(contract);

			var entryFromRepo = repository.Load<Artist>(artist.Id);
			Assert.AreEqual("Updated", entryFromRepo.Description.English, "Description was updated");

			// Act
			var result = await queries.RevertToVersion(oldVer.Id);			

			// Assert
			Assert.AreEqual(0, result.Warnings.Length, "Number of warnings");

			entryFromRepo = repository.Load<Artist>(result.Id);
			Assert.AreEqual("Original", entryFromRepo.Description.English, "Description was restored");

			var lastVersion = entryFromRepo.ArchivedVersionsManager.GetLatestVersion();
			Assert.IsNotNull(lastVersion, "Last version is available");
			Assert.AreEqual(ArtistArchiveReason.Reverted, lastVersion.Reason, "Last version archive reason");
			Assert.IsFalse(lastVersion.Diff.Picture.IsChanged, "Picture was not changed");

		}

		/// <summary>
		/// Old version has no image, image will be removed.
		/// </summary>
		[TestMethod]
		public async Task Revert_RemoveImage() {

			var oldVer = await repository.HandleTransactionAsync(ctx => queries.ArchiveAsync(ctx, artist, ArtistArchiveReason.PropertiesUpdated));
			await CallUpdate(ResourceHelper.TestImage());

			var result = await queries.RevertToVersion(oldVer.Id);

			var entryFromRepo = repository.Load<Artist>(result.Id);
			Assert.IsTrue(PictureData.IsNullOrEmpty(entryFromRepo.Picture), "Picture data was removed");
			Assert.IsTrue(string.IsNullOrEmpty(entryFromRepo.PictureMime), "Picture MIME was removed");

			var lastVersion = entryFromRepo.ArchivedVersionsManager.GetLatestVersion();
			Assert.IsNotNull(lastVersion, "Last version is available");
			Assert.AreEqual(2, lastVersion.Version, "Last version number");
			Assert.AreEqual(ArtistArchiveReason.Reverted, lastVersion.Reason, "Last version archive reason");
			Assert.IsTrue(lastVersion.Diff.Picture.IsChanged, "Picture was changed");

		}

		/// <summary>
		/// Revert to an older version with a different image.
		/// </summary>
		[TestMethod]
		public async Task Revert_ImageChanged() {

			// Arrange
			var original = await CallUpdate(ResourceHelper.TestImage()); // First version, this is the one being restored to	
			await CallUpdate(ResourceHelper.TestImage2, "image/png"); // Second version, with a different image

			var entryFromRepo = repository.Load<Artist>(artist.Id);
			Assert.IsFalse(PictureData.IsNullOrEmpty(entryFromRepo.Picture), "Artist has picture");
			var oldPictureData = entryFromRepo.Picture.Bytes;

			var oldVer = entryFromRepo.ArchivedVersionsManager.GetVersion(original.Version); // Get the first archived version
			Assert.IsNotNull(oldVer, "Old version was found");

			// Act
			var result = await queries.RevertToVersion(oldVer.Id);

			// Assert
			entryFromRepo = repository.Load<Artist>(result.Id);
			Assert.IsTrue(!PictureData.IsNullOrEmpty(entryFromRepo.Picture), "Artist has picture");
			Assert.AreNotEqual(oldPictureData, entryFromRepo.Picture.Bytes, "Picture data was updated");
			Assert.AreEqual(MediaTypeNames.Image.Jpeg, entryFromRepo.PictureMime, "Picture MIME was updated");

			var lastVersion = entryFromRepo.ArchivedVersionsManager.GetLatestVersion();
			Assert.IsNotNull(lastVersion, "Last version is available");
			Assert.AreEqual(ArtistArchiveReason.Reverted, lastVersion.Reason, "Last version archive reason");
			Assert.IsTrue(lastVersion.Diff.Picture.IsChanged, "Picture was changed");

		}

		[TestMethod]
		[ExpectedException(typeof(NotAllowedException))]
		public async Task Revert_NotAllowed() {

			// Regular users can't revert
			user.GroupId = UserGroupId.Regular;
			permissionContext.RefreshLoggedUser(repository);

			await queries.RevertToVersion(0);

		}

		[TestMethod]
		public async Task Update_Names() {
			
			// Arrange
			var contract = new ArtistForEditContract(artist, ContentLanguagePreference.English, new InMemoryImagePersister());

			contract.Names.First().Value = "Replaced name";
			contract.UpdateNotes = "Updated artist";

			// Act
			contract = await CallUpdate(contract);

			// Assert
			Assert.AreEqual(artist.Id, contract.Id, "Update album Id as expected");

			var artistFromRepo = repository.Load(contract.Id);
			Assert.AreEqual("Replaced name", artistFromRepo.DefaultName);
			Assert.AreEqual(1, artistFromRepo.Version, "Version");

			var archivedVersion = repository.List<ArchivedArtistVersion>().FirstOrDefault();

			Assert.IsNotNull(archivedVersion, "Archived version was created");
			Assert.AreEqual(artist, archivedVersion.Artist, "Archived version album");
			Assert.AreEqual(ArtistArchiveReason.PropertiesUpdated, archivedVersion.Reason, "Archived version reason");
			Assert.AreEqual(ArtistEditableFields.Names, archivedVersion.Diff.ChangedFields.Value, "Changed fields");

			var activityEntry = repository.List<ActivityEntry>().FirstOrDefault();

			Assert.IsNotNull(activityEntry, "Activity entry was created");
			Assert.AreEqual(artist, activityEntry.EntryBase, "Activity entry's entry");
			Assert.AreEqual(EntryEditEvent.Updated, activityEntry.EditEvent, "Activity entry event type");

		}

		[TestMethod]
		public async Task Update_OriginalName_UpdateArtistStrings() {

			artist.Names.Names.Clear();
			artist.Names.Add(new ArtistName(artist, new LocalizedString("初音ミク", ContentLanguageSelection.Japanese)));
			artist.Names.Add(new ArtistName(artist, new LocalizedString("Hatsune Miku", ContentLanguageSelection.Romaji)));
			artist.TranslatedName.DefaultLanguage = ContentLanguageSelection.Japanese;
			artist.Names.UpdateSortNames();
			repository.SaveNames(artist);

			var song = repository.Save(CreateEntry.Song());
			repository.Save(song.AddArtist(artist));
			song.UpdateArtistString();

			Assert.AreEqual("初音ミク", song.ArtistString[ContentLanguagePreference.Default], "Precondition: default name");

			var contract = new ArtistForEditContract(artist, ContentLanguagePreference.English, new InMemoryImagePersister());
			contract.DefaultNameLanguage = ContentLanguageSelection.English;

			await CallUpdate(contract);

			Assert.AreEqual("Hatsune Miku", song.ArtistString[ContentLanguagePreference.Default], "Default name was updated");

		}

		[TestMethod]
		public async Task Update_Picture() {

			var contract = await CallUpdate(ResourceHelper.TestImage());

			var artistFromRepo = repository.Load(contract.Id);

			Assert.IsFalse(PictureData.IsNullOrEmpty(artist.Picture), "Picture was saved");
			Assert.AreEqual(MediaTypeNames.Image.Jpeg, artistFromRepo.PictureMime, "Picture.Mime");

			var thumbData = new EntryThumb(artistFromRepo, artistFromRepo.PictureMime, ImagePurpose.Main);
			Assert.IsFalse(imagePersister.HasImage(thumbData, ImageSize.Original), "Original file was not created"); // Original saved in Picture.Bytes
			Assert.IsTrue(imagePersister.HasImage(thumbData, ImageSize.Thumb), "Thumbnail file was saved");

			var archivedVersion = repository.List<ArchivedArtistVersion>().FirstOrDefault();

			Assert.IsNotNull(archivedVersion, "Archived version was created");
			Assert.AreEqual(ArtistEditableFields.Picture, archivedVersion.Diff.ChangedFields.Value, "Changed fields");

		}

		[TestMethod]
		public async Task Update_ArtistLinks() {

			// Arrange
			var circle = repository.Save(CreateEntry.Circle());
			var illustrator = repository.Save(CreateEntry.Artist(ArtistType.Illustrator));

			var contract = new ArtistForEditContract(vocalist, ContentLanguagePreference.English, new InMemoryImagePersister()) {
				Groups = new[] {
					new ArtistForArtistContract { Parent = new ArtistContract(circle, ContentLanguagePreference.English)},
				},
				Illustrator = new ArtistContract(illustrator, ContentLanguagePreference.English)
			};

			// Act
			await CallUpdate(contract);

			// Assert
			var artistFromRepo = repository.Load(contract.Id);

			Assert.AreEqual(2, artistFromRepo.AllGroups.Count, "Number of groups");
			Assert.IsTrue(artistFromRepo.HasGroup(circle), "Has group");
			Assert.IsTrue(artistFromRepo.HasGroup(illustrator), "Has illustrator");
			Assert.AreEqual(ArtistLinkType.Group, artistFromRepo.Groups.First(g => g.Parent.Equals(circle)).LinkType, "Artist link type for circle");
			Assert.AreEqual(ArtistLinkType.Illustrator, artistFromRepo.Groups.First(g => g.Parent.Equals(illustrator)).LinkType, "Artist link type for illustrator");

		}

		[TestMethod]
		public async Task Update_ArtistLinks_ChangeRole() {

			// Arrange
			var illustrator = repository.Save(CreateEntry.Artist(ArtistType.Illustrator));
			vocalist.AddGroup(illustrator, ArtistLinkType.Illustrator);

			// Change linked artist from illustrator to voice provider
			var contract = new ArtistForEditContract(vocalist, ContentLanguagePreference.English, new InMemoryImagePersister()) {
				Illustrator = null,
				VoiceProvider = new ArtistContract(illustrator, ContentLanguagePreference.English)
			};

			// Act
			await CallUpdate(contract);

			// Assert
			vocalist = repository.Load(contract.Id);

			Assert.AreEqual(1, vocalist.AllGroups.Count, "Number of linked artists");

			var link = vocalist.AllGroups[0];
			Assert.AreEqual(illustrator, link.Parent, "Linked artist as expected");
			Assert.AreEqual(ArtistLinkType.VoiceProvider, link.LinkType, "Link type was updated");

		}

		[TestMethod]
		public async Task Update_ArtistLinks_IgnoreInvalid() {

			// Arrange
			var circle = repository.Save(CreateEntry.Circle());
			var circle2 = repository.Save(CreateEntry.Circle());

			// Cannot add character designer for producer
			var contract = new ArtistForEditContract(artist, ContentLanguagePreference.English, new InMemoryImagePersister()) {
				AssociatedArtists = new[] {
					new ArtistForArtistContract(new ArtistForArtist(circle, artist, ArtistLinkType.CharacterDesigner), ContentLanguagePreference.English),
				},
				Groups = new[] {
					new ArtistForArtistContract(new ArtistForArtist(circle2, artist, ArtistLinkType.Group), ContentLanguagePreference.English)
				}
			};

			// Act
			await CallUpdate(contract);

			// Assert
			Assert.AreEqual(1, artist.AllGroups.Count, "Number of linked artists");
			Assert.IsFalse(artist.HasGroup(circle), "Character designer was not added");

		}

	}

}
