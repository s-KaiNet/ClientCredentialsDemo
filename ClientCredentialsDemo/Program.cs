using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.SharePoint.Client;
using Newtonsoft.Json.Linq;

namespace ClientCredentialsDemo
{
	class Program
	{
		static void Main()
		{
			var accessToken = GetAccessToken();
			Console.WriteLine($"Client OM: {GetWebTitleOM(accessToken)}");
			Console.WriteLine($"REST: {GetWebTitleREST(accessToken)}");
		}

		private static string GetAccessToken()
		{
			var authority = ConfigurationManager.AppSettings["AuthorizationUri"];
			var authenticationContext = new AuthenticationContext(authority, false);

			var cert = new X509Certificate2(ConfigurationManager.AppSettings["CertPath"], ConfigurationManager.AppSettings["CertPassWord"], X509KeyStorageFlags.MachineKeySet);

			var cac = new ClientAssertionCertificate(ConfigurationManager.AppSettings["ClientId"], cert);

			var authenticationResult = authenticationContext.AcquireTokenAsync(ConfigurationManager.AppSettings["SPResourceURL"], cac);

			return authenticationResult.Result.AccessToken;
		}

		private static string GetWebTitleREST(string accessToken)
		{
			using (var client = new HttpClient())
			{
				client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
				client.BaseAddress = new Uri(ConfigurationManager.AppSettings["SPResourceURL"]);
				client.DefaultRequestHeaders.Accept.Clear();
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

				var response = client.GetAsync("_api/web").Result;
				if (response.IsSuccessStatusCode)
				{
					dynamic res = JObject.Parse(response.Content.ReadAsStringAsync().Result);

					return res.Title;
				}

				throw new Exception("Error");
			}
		}

		private static string GetWebTitleOM(string accessToken)
		{
			using (var ctx = new ClientContext(ConfigurationManager.AppSettings["SPResourceURL"]))
			{
				ctx.ExecutingWebRequest += (sender, args) =>
				{
					args.WebRequestExecutor.WebRequest.Headers.Add("Authorization", "Bearer " + accessToken);
				};

				ctx.Load(ctx.Web);
				ctx.ExecuteQuery();

				return ctx.Web.Title;
			}
		}
	}
}
