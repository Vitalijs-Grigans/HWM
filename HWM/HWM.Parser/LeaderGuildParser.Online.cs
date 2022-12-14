using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;

using HtmlAgilityPack;

using HWM.Parser.Helpers;
using HWM.Parser.Interfaces;
using HWM.Parser.Entities.Creature;
using HWM.Parser.Entities.LeaderGuild;
using HWM.Parser.Extensions;

namespace HWM.Parser
{
    [SupportedOSPlatform("windows")]
    public partial class LeaderGuildParser : IParser
    {
        private string _endpoint;
        private string _jsonFolder;
        private string _imageFolder;

        private static Rarity GetFollowerRarity(string colorHex)
        {
            Rarity rarity = default(Rarity);

            switch (colorHex.ToLower())
            {
                case "#fdd7a7":
                    rarity = Rarity.Mythical;
                    break;
                case "#f4e5b0":
                    rarity = Rarity.Legendary;
                    break;
                case "#9cb6d4":
                    rarity = Rarity.VeryRare;
                    break;
                case "#bea798":
                    rarity = Rarity.Rare;
                    break;
                case "#bfbfbf":
                    rarity = Rarity.Standard;
                    break;
                default:
                    rarity = Rarity.None;
                    break;
            }

            return rarity;
        }

        private int ConvertToNumber(string input, bool nullable = false) 
        {
            int returnVal;

            if (nullable)
            {
                int.TryParse(input, out returnVal);
            }

            else 
            {
                returnVal = int.Parse(input);
            }

            return returnVal;
        }
        
        public LeaderGuildParser(IDictionary<string, string> config)
        {
            _endpoint = config["LeaderGuildEndpoint"];
            _jsonFolder = config["ParseResultsFolder"];
            _imageFolder = config["CreatureImageFolder"];
        }
        
        public async Task CollectDataAsync()
        {
            HtmlDocument htmlDoc = await ExternalServices.Instance.GetHtmlAsync(_endpoint);
            HtmlNode body = htmlDoc.DocumentNode.SelectSingleNode("//body");
            HtmlNodeCollection creatureNodes = 
                body.SelectNodes("//div[@class='fcont']//div[@class='cre_mon_parent']/a");
            IList<Follower> creatureList = new List<Follower>();

            int id = 0;

            foreach (var anchor in creatureNodes)
            {
                Console.Clear();
                
                string backgroundStyle =
                    anchor.ParentNode.ParentNode.ParentNode.Attributes["style"].Value;
                string url = anchor.Attributes["href"].Value;
                string name = url.Split('=').LastOrDefault();
                string displayName = anchor.Attributes["title"].Value;
                string imageUrl = anchor.ChildNodes.LastOrDefault().Attributes["src"].Value;
                string file = $"{name}.png";
                string cachedImage = Directory.GetFiles(_imageFolder, file).FirstOrDefault();
                HtmlDocument creatureDoc = await ExternalServices.Instance.GetHtmlAsync(url);
                HtmlNode creatureBody = creatureDoc.DocumentNode.SelectSingleNode("//body");
                HtmlNodeCollection creatureStats = 
                    creatureBody.SelectNodes("//div[@class='scroll_content_half']//div");
                HtmlNodeCollection creatureSkills =
                    creatureBody.SelectNodes("//div[@class='army_info_skills']/span");
                string[] damageParts = creatureStats[4].InnerText.Split('-');
                Rarity tier = GetFollowerRarity(backgroundStyle.Split(':').LastOrDefault());

                var follower = new Follower()
                {
                    Id = id++,
                    Url = url,
                    Name = name,
                    DisplayName = displayName,
                    Characteristics = new CreatureStats()
                    {
                        Attack = ConvertToNumber(creatureStats[0].InnerText),
                        Shots = ConvertToNumber(creatureStats[1].InnerText, nullable: true),
                        Defence = ConvertToNumber(creatureStats[2].InnerText),
                        Mana = ConvertToNumber(creatureStats[3].InnerText, nullable: true),
                        MinDamage = ConvertToNumber(damageParts.FirstOrDefault()),
                        MaxDamage = ConvertToNumber(damageParts.LastOrDefault()),
                        Range = ConvertToNumber(creatureStats[5].InnerText, nullable: true),
                        HitPoints = ConvertToNumber(creatureStats[6].InnerText),
                        Initiative = ConvertToNumber(creatureStats[7].InnerText),
                        Movement = ConvertToNumber(creatureStats[8].InnerText),
                        Abilities = creatureSkills?.Count ?? 0,
                    },
                    Tier = tier,
                    DisplayTier = tier.GetDisplayName(),
                    Leadership = ConvertToNumber(creatureStats[9].InnerText.Replace(",", string.Empty)),
                };

                creatureList.Add(follower);

                if (cachedImage == null)
                {
                    await ExternalServices.Instance.DownloadImageAsync(imageUrl, $@"{_imageFolder}\{file}");
                }

                Console.WriteLine($"Processed {id} out of {creatureNodes.Count}");
            }

            await ExternalServices.Instance.SaveJsonAsync(creatureList, $@"{_jsonFolder}\LGCreatures.json");
        }
    }
}
