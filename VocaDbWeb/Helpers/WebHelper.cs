using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.Web;
using NLog;
using VocaDb.Model.Domain.Globalization;
using VocaDb.Web.Code;

namespace VocaDb.Web.Helpers {

	public static class WebHelper {

		private static readonly Logger log = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// User agent strings for which hits won't be counted
		/// </summary>
		private static readonly string[] forbiddenUserAgents = {
			"Googlebot", "bingbot"
		};

		public static IEnumerable<OptionalCultureCode> GetUserLanguageCodes(HttpRequestBase request) {
			return request.UserLanguages?.Select(l => new OptionalCultureCode(l.Split(';')[0])); // en-US;q=0.8 -> en-US
		}

		/// <summary>
		/// Gets the user interface culture name from the current request, as specified by the client.
		/// The first culture, if any, that matches one of the available user interface languages is returned.
		/// </summary>
		/// <param name="request">HTTP request.</param>
		/// <returns>Culture name. Empty if not matched.</returns>
		public static string GetInterfaceCultureName(HttpRequestBase request) {

			if (request.UserLanguages == null || !request.UserLanguages.Any())
				return string.Empty;

			return GetUserLanguageCodes(request)
				.Select(l => l.GetCultureInfoSafe())
				.Where(l => l != null && InterfaceLanguage.IsValidUserInterfaceCulture(l))
				.Select(l => l.Name)
				.FirstOrDefault() ?? string.Empty;

		}

		public static string GetRealHost(HttpRequestBase request) {

			return request.UserHostAddress;

		}

		public static string GetRealHost(HttpRequestMessage request) {

			// From https://blogs.msdn.microsoft.com/hongmeig1/2012/07/09/how-to-access-the-clients-ip-address-in-web-api/
			if (HttpContext.Current != null)
				return HttpContext.Current.Request.UserHostAddress;

			object property;
			if (request.Properties.TryGetValue(typeof(RemoteEndpointMessageProperty).FullName, out property)) {
				var remoteProperty = (RemoteEndpointMessageProperty)property;
				return remoteProperty.Address;
			}

			return null;

		}

		public static bool IsLocalhost(string hostname) {

			if (string.IsNullOrEmpty(hostname))
				return false;

			var localhosts = new[] { "localhost", "127.0.0.1", "::1" };
			return localhosts.Contains(hostname);

		}

		/// <summary>
		/// Checks whether the request should be counted as a valid hit (view) 
		/// for an entry.
		/// 
		/// Bots and blank user agents are excluded.
		/// </summary>
		/// <param name="request">HTTP request. Cannot be null.</param>
		/// <returns>True if the request should be counted.</returns>
		public static bool IsValidHit(HttpRequestBase request) {

			var ua = request.UserAgent;

			if (string.IsNullOrEmpty(ua)) {
				log.Warn(ErrorLogger.RequestInfo("Blank user agent from", request));
				return false;
			}

			return !forbiddenUserAgents.Any(ua.Contains);

		}

		public static void VerifyUserAgent(HttpRequestBase request) {

			var ua = request.UserAgent;
			if (string.IsNullOrEmpty(ua)) {
				log.Warn(ErrorLogger.RequestInfo("Blank user agent from", request));
				//throw new NotAllowedException();
			}

		}

	}

}