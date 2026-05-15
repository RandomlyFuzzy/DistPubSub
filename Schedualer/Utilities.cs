using System.Text.Json;

namespace Schedualer
{
    public static class Utilities
    {
        public static (int, int) EvalTime(this string time, string timezone)
        {
            if (time == "*")
            {
                return (DateTime.Now.Hour, DateTime.Now.Minute + 1);
            }
            else if (time.Contains("*"))
            {
                string[] parts = time.Split(":");
                if (parts[0] == "*")
                {
                    return (DateTime.Now.Hour + 1, int.Parse(parts[1]));
                }
                else
                {
                    return (int.Parse(parts[0]), DateTime.Now.Minute + 1);
                }
            }
            string[] split = time.Split(":");
            if (split[1] == "00" || split[1] == "0")
            {
                split[1] = "1";
            }
            return (int.Parse(split[0]), int.Parse(split[1]));
        }

        public static DateTime EvaluateNext(this string day, string time, string timezone)
        {
            (int hour, int mins) = EvalTime(time, timezone);
            DateTime now = DateTime.Now;
            DateTime next = DateTime.Now;
            while (next.DayOfWeek.ToString() != day)
            {
                next = next.AddDays(1);
            }
            next = new DateTime(next.Year, next.Month, next.Day, hour, mins, 0);
            if (next < now)
            {
                next = next.AddDays(7);
            }
            return next;
        }

        public static Entry[] JsonDeserialize(string json)
        {
            return JsonSerializer.Deserialize<Entry[]>(json);
        }

        public static string JsonSerialize(Entry[] entries)
        {
            return JsonSerializer.Serialize(entries);
        }
    }
}
