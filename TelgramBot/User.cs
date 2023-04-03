using System.Data.SQLite;
namespace TelegramBotExperiments;
public class User
{
    public long ChatId { get; set; }
    public string ZodiacSign { get; set; }
    public TimeSpan DailyMessageTime { get; set; }
    public string TimeZone { get; set; }

    public static void CreateTable(SQLiteConnection connection)
    {
        using var command = new SQLiteCommand(
            "CREATE TABLE IF NOT EXISTS Users (ChatId INTEGER PRIMARY KEY, ZodiacSign TEXT, DailyMessageTime TEXT, TimeZone TEXT)",
            connection
        );
        command.ExecuteNonQuery();
    }

    public void Save(SQLiteConnection connection)
    {
        using var command = new SQLiteCommand(
            "INSERT OR REPLACE INTO Users (ChatId, ZodiacSign, DailyMessageTime, TimeZone) VALUES (@chatId, @zodiacSign, @dailyMessageTime, @timeZone)",
            connection
        );
        command.Parameters.AddWithValue("@chatId", ChatId);
        command.Parameters.AddWithValue("@zodiacSign", ZodiacSign);
        command.Parameters.AddWithValue("@dailyMessageTime", DailyMessageTime.ToString("hh\\:mm"));
        command.Parameters.AddWithValue("@timeZone", TimeZone);
        command.ExecuteNonQuery();
    }

    public static User Load(SQLiteConnection connection, long chatId)
    {
        using var command = new SQLiteCommand(
            "SELECT ZodiacSign, DailyMessageTime, TimeZone FROM Users WHERE ChatId = @chatId",
            connection
        );
        command.Parameters.AddWithValue("@chatId", chatId);
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            var user = new User { ChatId = chatId, ZodiacSign = reader.GetString(0) };
            TimeSpan dailyMessageTime;
            if (TimeSpan.TryParseExact(reader.GetString(1), "hh\\:mm", null, out dailyMessageTime))
            {
                user.DailyMessageTime = dailyMessageTime;
            }
            user.TimeZone = reader.GetString(2);
            return user;
        }
        else
        {
            return null;
        }
    }

    public static List<User> LoadAll(SQLiteConnection connection)
    {
        var users = new List<User>();
        using var command = new SQLiteCommand("SELECT * FROM Users", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var user = new User
            {
                ChatId = Convert.ToInt64(reader["ChatId"]),
                ZodiacSign = Convert.ToString(reader["ZodiacSign"]),
                DailyMessageTime = TimeSpan.Parse(Convert.ToString(reader["DailyMessageTime"])),
                TimeZone = Convert.ToString(reader["TimeZone"])
            };
            users.Add(user);
        }
        return users;
    }
}
