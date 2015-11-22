﻿using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using VocaDb.Model.Domain;
using VocaDb.Model.Domain.Images;
using VocaDb.Model.Domain.Tags;

namespace VocaDb.Model.DataContracts.Tags {

	[DataContract(Namespace = Schemas.VocaDb)]
	public class TagForApiContract {

		public TagForApiContract() { }

		public TagForApiContract(Tag tag, 
			IEntryImagePersisterOld thumbPersister,
			bool ssl,			
			TagOptionalFields optionalFields) {
			
			CategoryName = tag.CategoryName;
			Id = tag.Id;
			Name = tag.Name;
			Status = tag.Status;
			UrlSlug = tag.Name;
			Version = tag.Version;

			if (optionalFields.HasFlag(TagOptionalFields.AliasedTo) && tag.AliasedTo != null) {
				AliasedTo = new TagBaseContract(tag.AliasedTo);
			}

			if (optionalFields.HasFlag(TagOptionalFields.Description)) {
				Description = tag.Description;
			}

			if (optionalFields.HasFlag(TagOptionalFields.MainPicture) && tag.Thumb != null) {
				MainPicture = new EntryThumbForApiContract(tag.Thumb, thumbPersister, ssl);
			}

			if (optionalFields.HasFlag(TagOptionalFields.Parent) && tag.Parent != null) {
				Parent = new TagBaseContract(tag.Parent);
			}

		}

		[DataMember]
		public TagBaseContract AliasedTo { get; set; }

		[DataMember]
		public string CategoryName { get; set; }

		[DataMember(EmitDefaultValue = false)]
		public string Description { get; set; }

		[DataMember]
		public int Id { get; set; }

		[DataMember(EmitDefaultValue = false)]
		public EntryThumbForApiContract MainPicture { get; set; }

		[DataMember]
		public string Name { get; set; }

		[DataMember]
		public TagBaseContract Parent { get; set; }

		[DataMember]
		[JsonConverter(typeof(StringEnumConverter))]
		public EntryStatus Status { get; set; }

		[DataMember]
		public string UrlSlug { get; set; }

		[DataMember]
		public int Version { get; set; }

	}

	[Flags]
	public enum TagOptionalFields {

		None		= 0,
		AliasedTo	= 1,
		Description = 2,
		MainPicture = 4,
		Parent		= 8

	}

}
