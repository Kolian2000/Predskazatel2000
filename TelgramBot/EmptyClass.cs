using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;



namespace TelegramBotExperiments
{
    public class SetHorscopTime
    {
        public static async Task AskZodiacSign(ITelegramBotClient botClient, long chatId, Update update)
        {
            await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Введите ваш знак зодиака:");

            // ожидаем ответа пользователя
            string zodiacSign = null;
            while (string.IsNullOrWhiteSpace(zodiacSign))
            {
                var updates = await botClient.GetUpdatesAsync(offset: update.Id + 1);
                var message = updates.FirstOrDefault()?.Message;
                if (message != null && !string.IsNullOrWhiteSpace(message.Text))
                {
                    zodiacSign = message.Text.Trim();
                }
            }

            await HandleZodiacSign(botClient, chatId, zodiacSign);
        }
        public static async Task SaveZodiac(ITelegramBotClient botClient, long chatId, Update update, CancellationToken cancellationToken)
        {
            await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Введите ваш знак зодиака:");
            var response = "";

            // Получаем обновления, начиная со следующего после текущего
            var updates = await botClient.GetUpdatesAsync(update.Id + 1);

            // Ожидаем, пока пользователь не введет сообщение
            while (string.IsNullOrWhiteSpace(response))
            {
                foreach (var u in updates)
                {
                    // Проверяем, что сообщение пришло от нужного чата и от пользователя, который начал этот диалог
                    if (u.Message != null && u.Message.Chat.Id == chatId && u.Message.From.Id == update.Message.From.Id)
                    {
                        response = u.Message.Text;
                        break;
                    }
                }

                // Если сообщение не было получено, ждем 1 секунду и запрашиваем обновления снова
                if (string.IsNullOrWhiteSpace(response))
                {
                    await Task.Delay(1000);
                    updates = await botClient.GetUpdatesAsync(update.Id + 1);
                }
            }

            using var connection = new SQLiteConnection("Data Source=users.db");
            connection.Open();
            User.CreateTable(connection);
            var user = User.Load(connection, chatId) ?? new User { ChatId = chatId };
            user.ZodiacSign = response;
            user.Save(connection);

            await botClient.SendTextMessageAsync(chatId, "Знак зодиака сохранен.");
            await SetDailyTimeZone(botClient, update.Message.Chat.Id);
            await SetDailyMessageTime(botClient, update.Message.Chat.Id);
        }




        public static async Task HandleZodiacSign(ITelegramBotClient botClient, long chatId, string zodiacSign)
        {
            // Список знаков зодиака
            var zodiacSigns = new string[] { "овен", "телец", "близнецы", "рак", "лев", "дева", "весы", "скорпион", "стрелец", "козерог", "водолей", "рыбы" };

            // Проверяем, что введенный знак зодиака есть в списке
            if (zodiacSigns.Contains(zodiacSign.ToLower()))
            {
                // Отправляем сообщение с гороскопом для введенного знака зодиака
                var horoscope = await Program.GetHoroscope(zodiacSign);
                await botClient.SendTextMessageAsync(chatId, $"Гороскоп для {zodiacSign}: {horoscope}");
            }
            else
            {
                // Если введенный знак зодиака не найден в списке, отправляем сообщение об ошибке
                await botClient.SendTextMessageAsync(chatId, $"Знак зодиака {zodiacSign} не найден.");
            }
        }
        public static async Task SetDailyTimeZone(ITelegramBotClient botClient, long chatId)
        {
            try
            {
                var buttonTitles = new List<string>
                {
                    "Europe/Moscow",
                    "Europe/Kaliningrad",
                    "Europe/Samara",
                    "Asia/Yekaterinburg",
                    "Asia/Omsk",
                    "Asia/Krasnoyarsk",
                    "Asia/Irkutsk",
                    "Asia/Yakutsk",
                    "Asia/Vladivostok",
                    "Asia/Magadan",
                    "Asia/Kamchatka",
                    "Подписаться на гороскоп"
                };


                var replyKeyboardMarkup = new ReplyKeyboardMarkup(new[]
                {
                    new[]
                    {
                        new KeyboardButton("Europe/Moscow"),
                        new KeyboardButton("Europe/Kaliningrad"),
                        new KeyboardButton("Europe/Samara")
                    },
                    new[]
                    {
                        new KeyboardButton("Asia/Yekaterinburg"),
                        new KeyboardButton("Asia/Omsk"),
                        new KeyboardButton("Asia/Krasnoyarsk")
                    },
                    new[]
                    {
                        new KeyboardButton("Asia/Irkutsk"),
                        new KeyboardButton("Asia/Yakutsk"),
                        new KeyboardButton("Asia/Vladivostok")
                    },
                    new[]
                    {
                        new KeyboardButton("Asia/Magadan"),
                        new KeyboardButton("Asia/Kamchatka"),

                    }
                });


                replyKeyboardMarkup.ResizeKeyboard = true;
                replyKeyboardMarkup.OneTimeKeyboard = true;


                var message = await botClient.SendTextMessageAsync(chatId, "Выберите ваш часовой пояс:", replyMarkup: replyKeyboardMarkup);


                var timeout = DateTime.UtcNow.AddSeconds(30);

                while (DateTime.UtcNow < timeout)
                {
                    var updates = await botClient.GetUpdatesAsync(offset: message.MessageId);

                    var lastMessage = updates.Select(u => u.Message).LastOrDefault(m => m?.Chat.Id == chatId);

                    if (lastMessage != null && buttonTitles.Contains(lastMessage.Text))
                    {
                        using var connection = new SQLiteConnection("Data Source=users.db");
                        connection.Open();

                        User.CreateTable(connection);

                        var user = User.Load(connection, chatId) ?? new User { ChatId = chatId };



                        user.TimeZone = lastMessage.Text;
                        user.Save(connection);

                        await botClient.SendTextMessageAsync(chatId, $"Ваш часовой пояс установлен на {lastMessage.Text}", replyMarkup: new ReplyKeyboardRemove());

                        return;
                    }

                    await Task.Delay(1000);
                }

                await botClient.SendTextMessageAsync(chatId, "Истекло время ожидания ответа.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }
        }

        public static async Task SetDailyMessageTime(ITelegramBotClient botClient, long chatId)
        {
            try
            {

                var message = await botClient.SendTextMessageAsync(chatId, "Введите время, когда вы хотите получать ежедневные гороскопы в формате чч:мм");


                var timeout = DateTime.Now.AddSeconds(30);

                while (DateTime.Now < timeout)
                {
                    var updates = await botClient.GetUpdatesAsync(offset: message.MessageId);

                    var lastMessage = updates.Select(u => u.Message).LastOrDefault(m => m?.Chat.Id == chatId);

                    if (lastMessage != null && TimeSpan.TryParseExact(lastMessage.Text, new[] { "h\\:m", "hh\\:mm" }, CultureInfo.InvariantCulture, out var dailyMessageTime))
                    {
                        using var connection = new SQLiteConnection("Data Source=users.db");
                        connection.Open();

                        User.CreateTable(connection);

                        var user = User.Load(connection, chatId) ?? new User { ChatId = chatId };

                        user.DailyMessageTime = dailyMessageTime;
                        user.Save(connection);

                        await botClient.SendTextMessageAsync(chatId, $"Ежедневный гороскоп установлен на {dailyMessageTime:hh\\:mm}");
                        return;
                    }

                    await Task.Delay(1000);
                }
                //if(string.IsNullOrEmpty(timezonena))
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }
        }
    }
}
