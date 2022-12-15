﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

using HtmlAgilityPack;
using Newtonsoft.Json;

using HWM.Parser.Interfaces;
using HWM.Parser.Entities.Creature;
using HWM.Parser.Entities.LeaderGuild;
using HWM.Parser.Mappers;

namespace HWM.Parser
{
    public class LeaderGuildParser : IParser
    {
        private string _endpoint;
        private string _jsonFolder;
        private string _imageFolder;

        private HtmlDocument GetLocalHtml(string url)
        {
            var doc = new HtmlDocument();

            using (WebClient client = new WebClient())
            {
                string html = client.DownloadString(url);

                doc.LoadHtml(html);
            }

            return doc;
        }

        private string GetFollowerRarity(string colorHex)
        {
            string tier = string.Empty;

            switch (colorHex.ToLower())
            {
                case "#fdd7a7":
                    tier = Rarity.Mythical;
                    break;
                case "#f4e5b0":
                    tier = Rarity.Legendary;
                    break;
                case "#9cb6d4":
                    tier = Rarity.VeryRare;
                    break;
                case "#bea798":
                    tier = Rarity.Rare;
                    break;
                case "#bfbfbf":
                    tier = Rarity.Standard;
                    break;
                default:
                    break;
            }

            return tier;
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

        private void DownloadFileFromUrl(string url, string fileName)
        {
            using (var client = new WebClient())
            {
                client.DownloadFile(new Uri(url), fileName);
            }
        }

        public LeaderGuildParser() { }
        
        public LeaderGuildParser(string endpoint, string jsonFolder, string imageFolder)
        {
            _endpoint = endpoint;
            _jsonFolder = jsonFolder;
            _imageFolder = imageFolder;
        }
        
        public void CollectData()
        {
            HtmlDocument htmlDoc = GetLocalHtml(_endpoint);

            HtmlNode body = htmlDoc.DocumentNode.SelectSingleNode("//body");
            HtmlNodeCollection creatureNodes = 
                body.SelectNodes("//div[@class='fcont']//div[@class='cre_mon_parent']/a");

            IList<Follower> creatureList = new List<Follower>();

            int id = 0;

            foreach (var anchor in creatureNodes)
            {
                string backgroundStyle =
                    anchor.ParentNode.ParentNode.ParentNode.Attributes["style"].Value;
                string url = anchor.Attributes["href"].Value;
                string name = url.Split('=').LastOrDefault();
                string displayName = CreatureMapper.Map(name);

                HtmlDocument creatureDoc = GetLocalHtml(url);
                HtmlNode creatureBody = creatureDoc.DocumentNode.SelectSingleNode("//body");
                HtmlNodeCollection creatureStats = 
                    creatureBody.SelectNodes("//div[@class='scroll_content_half']//div");

                string[] damageParts = creatureStats[4].InnerText.Split('-');

                var follower = new Follower()
                {
                    Id = id++,
                    Url = url,
                    Name = name,
                    DisplayName = CreatureMapper.Map(name),
                    Tier = GetFollowerRarity(backgroundStyle.Split(':').LastOrDefault()),
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
                        Movement = ConvertToNumber(creatureStats[8].InnerText)
                    },
                    Leadership = ConvertToNumber(creatureStats[9].InnerText.Replace(",", string.Empty))
                };

                creatureList.Add(follower);

                string imageUrl = anchor.ChildNodes.LastOrDefault().Attributes["src"].Value;
                string file = $"{name}.png";
                string cachedImage = Directory.GetFiles(_imageFolder, file).FirstOrDefault();

                if (cachedImage != null) 
                {
                    Console.WriteLine($"{file} is cached already");
                }
                
                else
                {
                    DownloadFileFromUrl(imageUrl, $@"{_imageFolder}\{file}");
                    Console.WriteLine($"{file} has been downloaded");
                }

                Console.WriteLine("Processed " + id + " creatures...");
                Console.WriteLine();
            }

            var json = JsonConvert.SerializeObject(creatureList, Formatting.Indented);

            File.WriteAllText($@"{_jsonFolder}\LGCreatures.json", json);
        }
    }
}
