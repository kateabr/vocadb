using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using VocaDb.Model.Domain.ExtLinks;

namespace VocaDb.Model.DataContracts {

	[DataContract(Namespace = Schemas.VocaDb)]
	public class WebLinkForApiContract : IWebLink {

		public WebLinkForApiContract() {}

		public WebLinkForApiContract(WebLink webLink, WebLinkOptionalFields fields = WebLinkOptionalFields.None) {
			
			ParamIs.NotNull(() => webLink);

			Category = webLink.Category;
			Description = webLink.Description;

			if (fields.HasFlag(WebLinkOptionalFields.DescriptionOrUrl)) {
				DescriptionOrUrl = webLink.DescriptionOrUrl;
			}

			Id = webLink.Id;
			Url = webLink.Url;

		}

		[DataMember]
		[JsonConverter(typeof(StringEnumConverter))]
		public WebLinkCategory Category { get; set; }

		[DataMember]
		public string Description { get; set; }

		[DataMember(EmitDefaultValue = false)]
		public string DescriptionOrUrl { get; set; }

		[DataMember]
		public int Id { get; set; }

		[DataMember]
		public string Url { get; set; }

	}
	
	[Flags]
	public enum WebLinkOptionalFields {

		None = 0,
		DescriptionOrUrl = 1

	}

}
