using NSoup.Nodes;
using NSoup.Parse;
using NSoup.Select;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
namespace NSoup.Tests
{
    public class Http
    {
        static Http()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
        private readonly HttpClient httpClient;
        private HttpClientHandler httpClientHandler;
        public Http(Uri proxyAddrss = null, string proxyUserName = null, string proxyPassword = null)
        {


            if (proxyAddrss != null)
            {
                //启用代理
                httpClientHandler = new HttpClientHandler()
                {
                    UseProxy = true,
                    UseCookies = true,
                    UseDefaultCredentials = false,
                    //Proxy = new WebProxy(proxyAddrss, true)
                    //{
                    //    Credentials = !string.IsNullOrWhiteSpace(proxyUserName) ? new NetworkCredential(proxyUserName, proxyPassword) : null
                    //}
                };

            }
            else
            {
                httpClientHandler = new HttpClientHandler() { UseCookies = true };
            }
            httpClient = new HttpClient(httpClientHandler);
            httpClient.Timeout = new TimeSpan(1, 0, 0);
            //client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
            httpClient.DefaultRequestHeaders.Add("Connection", "Keep-Alive");

        }

        public async Task<string> GetHtml(string url, Dictionary<string, string> headers = null)
        {
            httpClient.DefaultRequestHeaders.Host = (new Uri(url).Host);
            if (headers != null)
            {
                foreach (var key in headers.Keys)
                {
                    httpClient.DefaultRequestHeaders.Add(key, headers[key]);
                }
            }

            using (var result = await httpClient.GetAsync(url))
            {
                return await result.Content.ReadAsStringAsync();
            }
        }
    }
    public class Test
    {
        [Fact]
        public void TestRun()
        {
            Http http = new Http();
            var html = http.GetHtml("https://dealer.autohome.com.cn/1120/info.html").Result;
            Document doc = Parser.Parse(html, "http://dealer.autohome.com.cn/");
            Elements elems = doc.Select(".dealeron-cont .show-ul li img");
        }
    }
}
