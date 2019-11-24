using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Common.SampleConstants;
using IdentityModel.Client;
using IdentityModel.OidcClient;
using Newtonsoft.Json.Linq;

namespace Client
{
    public static class Sample
    {
        public static async Task Run()
        {
            Console.WriteLine("+-----------------------+");
            Console.WriteLine("|  Sign in with OIDC    |");
            Console.WriteLine("+-----------------------+");
            Console.WriteLine("");
            Console.WriteLine("Press any key to sign in...");
            Console.ReadKey();

            string token = await SignIn();
            
            Console.WriteLine("");
            Console.WriteLine("sign in done, going to call api with token, press any key to continue...");
            Console.ReadKey();

            await CallApi(token);
            
            Console.WriteLine("");
            Console.WriteLine("done");
        }

        private static async Task CallApi(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new Exception("token is missing");
            }
            
            var apiClient = new HttpClient();
            apiClient.SetBearerToken(token);

            HttpResponseMessage response = await apiClient.GetAsync($"{SampleUrls.Api}/identity");
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.StatusCode);
            }
            else
            {
                string content = await response.Content.ReadAsStringAsync();
                Console.WriteLine(JArray.Parse(content));
            }
        }

        private static async Task<string> SignIn()
        {
            // create a redirect URI using an available port on the loopback address.
            const string redirectUri = SampleUrls.ConsoleAppCallback;
            Console.WriteLine("redirect URI: " + redirectUri);

            // create an HttpListener to listen for requests on that redirect URI.
            var oidcRedirectListener = new HttpListener();
            oidcRedirectListener.Prefixes.Add(redirectUri);

            Console.WriteLine("Listening..");
            oidcRedirectListener.Start();

            try
            {
                var options = new OidcClientOptions
                              {
                                  Authority = SampleUrls.SecurityTokenService,
                                  ClientId = Clients.ConsoleApp,
                                  ClientSecret = Secrets.ConsoleApp,
                                  Scope = $"openid profile {SampleScopes.TestApi}",
                                  RedirectUri = redirectUri
                              };
                
                var client = new OidcClient(options);
                AuthorizeState state = await client.PrepareLoginAsync();

                Console.WriteLine($"Start URL: {state.StartUrl}");

                // open system browser to start authentication
                OpenBrowser(state.StartUrl);

                // wait for the authorization response.
                HttpListenerContext oidcRedirectListenerContext = await oidcRedirectListener.GetContextAsync();

                string formData = GetRequestPostData(oidcRedirectListenerContext.Request);

                // Brings the Console to Focus.
                //BringConsoleToFront(); // todo: this failed in macos, seems to be windows-only implementation, investigate, commenting out for now

                // sends an HTTP response to the browser.
                HttpListenerResponse response = oidcRedirectListenerContext.Response;
                string responseString = $"<html><head><meta http-equiv='refresh' content='10;url={SampleUrls.SecurityTokenService}'></head><body>Please return to the app.</body></html>";
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                Stream responseOutput = response.OutputStream;
                await responseOutput.WriteAsync(buffer, 0, buffer.Length);
                responseOutput.Close();

                Console.WriteLine($"Form Data: {formData}");

                Console.WriteLine();
                Console.WriteLine("going to process response, press any key to continue...");
                Console.ReadKey();

                LoginResult result = await client.ProcessResponseAsync(formData, state);

                if (result.IsError)
                {
                    Console.WriteLine("\n\nError:\n{0}", result.Error);
                    return null;
                }

                Console.WriteLine("\n\nClaims:");
                
                foreach (Claim claim in result.User.Claims)
                {
                    Console.WriteLine("  [{0}]: [{1}]", claim.Type, claim.Value);
                }

                Console.WriteLine();
                Console.WriteLine("Access token:\n\n{0}\n", result.AccessToken);

                if (!string.IsNullOrWhiteSpace(result.RefreshToken))
                {
                    Console.WriteLine("Refresh token:\n\n{0}\n", result.RefreshToken);
                }

                return result.AccessToken;
            }
            finally
            {
                oidcRedirectListener.Stop();
            }
        }

        private static void BringConsoleToFront()
        {
            SetForegroundWindow(GetConsoleWindow());
        }
        
        // Hack to bring the Console window to front.
        // ref: http://stackoverflow.com/a/12066376
        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private static string GetRequestPostData(HttpListenerRequest request)
        {
            if (!request.HasEntityBody)
            {
                // compensating for situations where data is sent in query string instead of request body
                return request.Url.Query;
            }

            using (Stream body = request.InputStream)
            {
                using (var reader = new StreamReader(body, request.ContentEncoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }
        
        /// <summary>
        /// https://brockallen.com/2016/09/24/process-start-for-urls-on-net-core/
        /// </summary>
        /// <param name="url"></param>
        private static void OpenBrowser(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
