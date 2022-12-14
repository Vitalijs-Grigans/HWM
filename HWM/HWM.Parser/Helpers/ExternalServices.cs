using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

using HtmlAgilityPack;
using Newtonsoft.Json;

using HWM.Parser.Entities.LeaderGuild;


namespace HWM.Parser.Helpers
{
    [SupportedOSPlatform("windows")]
    public sealed class ExternalServices
    {
        private static readonly Lazy<ExternalServices> requestHelper =
            new Lazy<ExternalServices>(() => new ExternalServices());

        private ExternalServices() { }

        public static ExternalServices Instance 
        { 
            get 
            { 
                return requestHelper.Value; 
            } 
        }

        public async Task<HtmlDocument> GetHtmlAsync(string url)
        {
            var doc = new HtmlDocument();
            var client = new HttpClient();
            byte[] bytes = await client.GetByteArrayAsync(url);
            Encoding encoding = Encoding.GetEncoding("windows-1251");
            string html = encoding.GetString(bytes, 0, bytes.Length);

            doc.LoadHtml(html);

            return doc;
        }

        public async Task DownloadImageAsync(string url, string fileName)
        {
            var client = new HttpClient();
            var response = await client.GetAsync(url);

            response.EnsureSuccessStatusCode();

            byte[] bytes = await response.Content.ReadAsByteArrayAsync();
            Image image = Image.FromStream(new MemoryStream(bytes));

            image.Save(fileName);
        }

        public async Task SaveJsonAsync(IEnumerable<Follower> data, string path)
        {
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);

            await File.WriteAllTextAsync(path, json);
        }

        public async Task<IEnumerable<Follower>> LoadJsonAsync(string path)
        {
            string json = await File.ReadAllTextAsync(path);

            return JsonConvert.DeserializeObject<IEnumerable<Follower>>(json);
        }
    }
}
