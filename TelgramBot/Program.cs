using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Args;
using System.Threading.Tasks;
using System.Xml;
using HtmlAgilityPack;
using Telegram.Bot.Types.Enums;
using Newtonsoft.Json;
using RestSharp;
using Newtonsoft.Json.Linq;
using System.Globalization;
using OpenAI_API.Chat;
using System.Data.SQLite;
using System.Linq;
using System.Timers;
using Telegram.Bot.Types.InputFiles;
using System.Text;
using System.Net.Http;
using System.Net;
using TimeZoneConverter;
using GeoTimeZone;  

namespace TelegramBotExperiments
{


    public class Program 
    {
        
        static ITelegramBotClient bot = new TelegramBotClient("6266024489:AAEqa-TkCn0U2Z65mn1cid0To60OEl6AFrU");
        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var message = update.Message;
            //// Некоторые действия


            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            ////await botClient.SendTextMessageAsync( message.Chat, text: "Знаешь кто самая топовая чикса ин зе ворлд", cancellationToken: cancellationToken);;

            if (message.Text == "/start")
            {
                var replyKeyboardMarkup = new ReplyKeyboardMarkup(new[]
                {
            new[]
            {
                new KeyboardButton("Получить гороскоп"),
                new KeyboardButton("Подписаться на гороскоп"),
            }
           
            
        });
                replyKeyboardMarkup.ResizeKeyboard = true;
                replyKeyboardMarkup.OneTimeKeyboard = true;

                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Выберите  вариант:",
                    replyMarkup: replyKeyboardMarkup
                );
            }
            else if (message.Text == "Получить гороскоп")
            {
                
                await SetHorscopTime.AskZodiacSign(botClient, update.Message.Chat.Id, update);
                
            }
            else if (message.Text == "Подписаться на гороскоп")
            {
                // обработка выбора варианта 2
                await SetHorscopTime.SaveZodiac(botClient, update.Message.Chat.Id, update, cancellationToken);
                //await SetDailyMessageTime(botClient, update.Message.Chat.Id, update);
            }
            
        }

        public static async Task<string> GetHoroscope(string zodiacSign)
        {
            string openaiApiKey = "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";

            var client = new RestClient("https://api.openai.com/v1/completions");
            var request = new RestSharp.RestRequest("", Method.Post);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Authorization", $"Bearer {openaiApiKey}");

            var requestJson = new
            {
                prompt = $"Какой гороскоп для {zodiacSign} сегодня?",
                model = "text-davinci-003",
                max_tokens = 300,
                temperature = 0.7,
                top_p = 1,
                n = 1,
                stop = "."


            };
            string requestBody = JsonConvert.SerializeObject(requestJson);
            request.AddJsonBody(requestBody);
            var x = 0;
            var response = await client.ExecuteAsync(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var content = response.Content;
                var json = JObject.Parse(content);
                var horoscope = json["choices"][0]["text"].ToString();
                return horoscope;
            }
            else
            {
                return null;
            }
        }


        private static async Task StartDailyHoroscopeTimer(ITelegramBotClient botClient)
        {
            while (true)
            {
                using var connection = new SQLiteConnection("Data Source=users.db");
                connection.Open();

                User.CreateTable(connection);

                var users = User.LoadAll(connection);

                foreach (var user in users)
                {
                    if (user.ChatId == 0 || user.ZodiacSign == null || user.DailyMessageTime == null || string.IsNullOrEmpty(user.TimeZone))
                    {
                        continue;
                    }
                    var now = DateTime.UtcNow; // текущее время в UTC
                    var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZone); // часовой пояс пользователя

                    var userLocalTime = TimeZoneInfo.ConvertTimeFromUtc(now, userTimeZone).TimeOfDay; // локальное время пользователя

                    var dailyMessageTime = user.DailyMessageTime;

                    if (userLocalTime.Hours == dailyMessageTime.Hours && userLocalTime.Minutes == dailyMessageTime.Minutes)
                    {
                        var horoscope = await GetHoroscope(user.ZodiacSign);
                        await botClient.SendTextMessageAsync(user.ChatId, horoscope);
                    }




                }
                var yyyy = 0;
                await Task.Delay(TimeSpan.FromMinutes(1));
                Console.WriteLine("Работает..");
            }
        }




        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Некоторые действия
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }

        
        static void Main(string[] args)
        {
            Console.WriteLine("Запущен бот " + bot.GetMeAsync().Result.FirstName);
            Task.Run(() => StartDailyHoroscopeTimer(bot));
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }, // receive all update types
            };
            bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );
            
            Console.ReadLine();
            
        }
    }
}
