﻿using System.Web.Mvc;
using VocaDb.Model;
using VocaDb.Model.DataContracts.Api;
using VocaDb.Model.Domain;
using VocaDb.Model.Domain.Users;

namespace VocaDb.Web.Helpers {

	public static class UrlHelperExtender {

		private static string EntryDetails(UrlHelper urlHelper, EntryType entryType, int id, string urlSlug) {

			switch (entryType) {
				case EntryType.DiscussionTopic:
					return urlHelper.Action("Index", "Discussion", new { clientPath = string.Format("topics/{0}", id) });

				case EntryType.ReleaseEvent:
					return urlHelper.Action("Details", "Event", new { id });

				case EntryType.Tag:
					return urlHelper.Action("DetailsById", "Tag", new { id, slug = urlSlug });

				default:
					return urlHelper.Action("Details", entryType.ToString(), new { id });
			}

		}

		public static string EntryDetails(this UrlHelper urlHelper, IEntryBase entryBase) {
			
			ParamIs.NotNull(() => entryBase);

			return EntryDetails(urlHelper, entryBase.EntryType, entryBase.Id, null);

		}

		public static string EntryDetails(this UrlHelper urlHelper, EntryForApiContract entry) {

			ParamIs.NotNull(() => entry);

			return EntryDetails(urlHelper, entry.EntryType, entry.Id, entry.UrlSlug);

		}

		public static string UserDetails(this UrlHelper urlHelper, IUser user) {

			ParamIs.NotNull(() => user);

			return urlHelper.Action("Profile", "User", new { id = user.Name });

		}

	}

}