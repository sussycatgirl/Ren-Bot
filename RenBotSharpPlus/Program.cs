﻿using System;
using System.Threading.Tasks;
using DSharpPlus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using DSharpPlus.SlashCommands;
using DSharpPlus.Net;
using DSharpPlus.Lavalink;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System.Data;
using System.Text.RegularExpressions;
using System.Web;
using System.Security.Cryptography;
using System.Timers;

namespace RenBotSharp
{
    public static class Program
    {
        public static IConfiguration config;
        public static DiscordClient Discord;
        public static List<string> CleverBotContext = new List<string>();
        public static CleverBot clever = new CleverBot();
        static async Task Main(string[] args)
        {
            foreach (string i in File.ReadLines($"{Environment.CurrentDirectory}\\Talky.Ren"))
            {
                string[] pair = i.Split(' ');
                Settings.TalkyServers[ulong.Parse(pair[0])] = bool.Parse(pair[1]);
            }

            config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(path: "config.json").Build();

            Discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = config["Token"],
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All,
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug,
                MessageCacheSize = 1024
            });

            var endpoint = new ConnectionEndpoint
            {
                Hostname = "127.0.0.1", // From your server configuration.
                Port = 2333 // From your server configuration
            };

            var lavalinkConfig = new LavalinkConfiguration
            {
                Password = "youshallnotpass", // From your server configuration.
                RestEndpoint = endpoint,
                SocketEndpoint = endpoint
            };

            var lavalink = Discord.UseLavalink();
            var slash = Discord.UseSlashCommands();

#if DEBUG
            slash.RegisterCommands<BasicCommandsModule>(864223405774602260);
            slash.RegisterCommands<AudioCommandsModule>(864223405774602260);
            slash.RegisterCommands<ImageCommandsModule>(864223405774602260);
#else
            slash.RegisterCommands<BasicCommandsModule>();
            slash.RegisterCommands<AudioCommandsModule>();
            slash.RegisterCommands<ImageCommandsModule>();
#endif

            Discord.MessageDeleted += async (s, e) =>
            {
                Settings.LastDeletedMessage[e.Guild.Id] = e.Message;
            };

            Discord.MessageCreated += async (s, e) =>
            {
                if (e.Message.Author.Id != 1088269352542949436)
                {
                    if (Settings.TalkyServers[e.Guild.Id])
                    {
                        if (e.Message.ChannelId == 1009900125818208276 || e.Message.ChannelId == 891136812091838514 || e.Message.ChannelId == 817559757249314816  || e.Message.ChannelId == 1133010507712974858)
                        {
                            if (!e.Message.Author.IsBot)
                            {
                                string CleverResponse = await clever.SendCleverbotMessage(e.Message.Content, CleverBotContext.ToArray());
                                CleverBotContext.Add(e.Message.Content);
                                CleverBotContext.Add(CleverResponse);
                                await e.Message.RespondAsync(CleverResponse);
                                if (CleverBotContext.Count > 100)
                                {
                                    CleverBotContext.RemoveAt(0);
                                    CleverBotContext.RemoveAt(0);
                                }
                            }
                        }
                    }
                }
            };

            Discord.ComponentInteractionCreated += async (s, e) =>
            {
                if (e.Id == "left_arrow_define")
                {
                    int currentpage = int.Parse(new Regex(@"\d+(?=\/)").Match(e.Message.Embeds.First().Footer.Text).Value);

                    currentpage--;

                    DataSet words = JsonConvert.DeserializeObject<DataSet>(await Settings.client.GetStringAsync($"https://api.urbandictionary.com/v0/{((string.IsNullOrEmpty(Settings.LastWord)) ? "random" : $"define?page=1&term={Settings.LastWord}")}"));

                    DataTable dataTable = words.Tables["list"];

                    DataRow result = dataTable.Rows[currentpage - 1];

                    DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                    {
                        Title = result["word"].ToString(),
                        Footer = new DiscordEmbedBuilder.EmbedFooter() { Text = $"Page {currentpage}/{dataTable.Rows.Count}  •  {DateTime.Now.ToString("t")}" },
                        Url = result["permalink"].ToString(),
                        Color = Settings.Rainbow[(currentpage - 1) % 7]
                    };

                    try
                    {
                        embed.AddField("Definition", Regex.Replace(result["definition"].ToString(), @"\[(.*?)\]", m => $"{m.Value}(<https://www.urbandictionary.com/define.php?term={HttpUtility.UrlEncode(m.Value.Replace("[", string.Empty).Replace("]", string.Empty))}>)"));

                        embed.AddField("Example", Regex.Replace(result["example"].ToString(), @"\[(.*?)\]", m => $"{m.Value}(<https://www.urbandictionary.com/define.php?term={HttpUtility.UrlEncode(m.Value.Replace("[", string.Empty).Replace("]", string.Empty))}>)"));

                        embed.AddField("Author", result["author"].ToString());

                        embed.AddField("Votes", $"Upvotes: {result["thumbs_up"]}\nDownvotes: {result["thumbs_down"]}");
                    }
                    catch
                    {
                        embed.AddField("Error :(", "Too much text");
                    }

                    var left = new DiscordButtonComponent(ButtonStyle.Primary, "left_arrow_define", "←", (currentpage <= 1));

                    var right = new DiscordButtonComponent(ButtonStyle.Primary, "right_arrow_define", "→", (dataTable.Rows.Count <= currentpage));

                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(embed).AddComponents(new DiscordComponent[]
                    {
                    left, right
                    }));
                }
                else if (e.Id == "right_arrow_define")
                {
                    int currentpage = int.Parse(new Regex(@"\d+(?=\/)").Match(e.Message.Embeds.First().Footer.Text).Value);

                    currentpage++;

                    DataSet words = JsonConvert.DeserializeObject<DataSet>(await Settings.client.GetStringAsync($"https://api.urbandictionary.com/v0/{((string.IsNullOrEmpty(Settings.LastWord)) ? "random" : $"define?page=1&term={Settings.LastWord}")}"));

                    DataTable dataTable = words.Tables["list"];

                    DataRow result = dataTable.Rows[currentpage - 1];

                    DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                    {
                        Title = result["word"].ToString(),
                        Footer = new DiscordEmbedBuilder.EmbedFooter() { Text = $"Page {currentpage}/{dataTable.Rows.Count}  •  {DateTime.Now.ToString("t")}" },
                        Url = result["permalink"].ToString(),
                        Color = Settings.Rainbow[(currentpage - 1) % 7]
                    };

                    try
                    {
                        embed.AddField("Definition", Regex.Replace(result["definition"].ToString(), @"\[(.*?)\]", m => $"{m.Value}(<https://www.urbandictionary.com/define.php?term={HttpUtility.UrlEncode(m.Value.Replace("[", string.Empty).Replace("]", string.Empty))}>)"));

                        embed.AddField("Example", Regex.Replace(result["example"].ToString(), @"\[(.*?)\]", m => $"{m.Value}(<https://www.urbandictionary.com/define.php?term={HttpUtility.UrlEncode(m.Value.Replace("[", string.Empty).Replace("]", string.Empty))}>)"));

                        embed.AddField("Author", result["author"].ToString());

                        embed.AddField("Votes", $"Upvotes: {result["thumbs_up"]}\nDownvotes: {result["thumbs_down"]}");
                    }
                    catch
                    {
                        embed.AddField("Error :(", "Too much text");
                    }

                    var left = new DiscordButtonComponent(ButtonStyle.Primary, "left_arrow_define", "←", (currentpage <= 1));

                    var right = new DiscordButtonComponent(ButtonStyle.Primary, "right_arrow_define", "→", (dataTable.Rows.Count <= currentpage));

                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(embed).AddComponents(new DiscordComponent[]
                    {
                    left, right
                    }));
                }
                else if (e.Id == "generate_new_color")
                {
                    string color = String.Format("#{0:X6}", RandomNumberGenerator.GetInt32(0, 0x1000000));

                    DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                    {
                        Title = color,
                        Color = new DiscordColor(color)
                    };

                    var newColor = new DiscordButtonComponent(ButtonStyle.Secondary, "generate_new_color", "New", false);

                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(embed).AddComponents(new DiscordComponent[] { newColor }));
                }
            };

            await Discord.ConnectAsync();
            await lavalink.ConnectAsync(lavalinkConfig); // Make sure this is after Discord.ConnectAsync().

            await Task.Delay(1000);

            await UpdateStatusAsync();
            System.Timers.Timer timer = new System.Timers.Timer(TimeSpan.FromMinutes(1));
            timer.Elapsed += async (s, e) => await UpdateStatusAsync();
            timer.Start();

            await Task.Delay(-1);
        }
        public static async Task UpdateStatusAsync()
        {
            DiscordActivity activity = new DiscordActivity();

            int type = RandomNumberGenerator.GetInt32(0, 6);
            if (type == 4) { type = 5; }
            activity.ActivityType = (ActivityType)type;

            activity.Name = Settings.Statuses[RandomNumberGenerator.GetInt32(0, Settings.Statuses.Length)].Replace("${Mem}", RandomMemberName());

            await Discord.UpdateStatusAsync(activity);
        }
        private static string RandomMemberName()
        {
            return Discord.Guilds[817559120910614570].Members.ElementAt(RandomNumberGenerator.GetInt32(0, Discord.Guilds[817559120910614570].Members.Count)).Value.DisplayName;
        }
    }
}