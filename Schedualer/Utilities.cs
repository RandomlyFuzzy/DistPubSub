using System.Text.Json;

namespace Schedualer
{
    public static class Utilities
    {
        public static (int, int) EvalTime(this string Time, string Timezone)
        {
            //Time = "00:00","*:00","00:*","*"
            if (Time == "*")
            {
                return (DateTime.Now.Hour, DateTime.Now.Minute + 1);
            }
            else if (Time.Contains("*"))
            {
                string[] strings = Time.Split(":");
                if (strings[0] == "*")
                {
                    return (DateTime.Now.Hour + 1, int.Parse(strings[1]));
                }
                else
                {
                    return (int.Parse(strings[0]), DateTime.Now.Minute + 1);
                }
            }
            string[] split = Time.Split(":");
            if (split[1] == "00"|| split[1] == "0")
            {
                split[1] = "1";
            }
            return (int.Parse(split[0]), int.Parse(split[1]));

        }
        public static DateTime EvaluateNext(this string Day, string Time, string Timezone)
        {
            (int Hour, int Mins) = EvalTime(Time, Timezone);
            DateTime now = DateTime.Now;
            DateTime next = DateTime.Now;
            while (next.DayOfWeek.ToString() != Day)
            {
                next = next.AddDays(1);
            }
            next = new DateTime(next.Year, next.Month, next.Day, Hour, Mins, 0);
            if (next < now)
            {
                next = next.AddDays(7);
            }
            return next;
        }
   
        public static Entry[] JsonDeserialize(string json)
        {
            return JsonSerializer.Deserialize < Entry[]>(json);
        }
        public static string JsonSerialize(Entry[] entries)
        {
            return JsonSerializer.Serialize(entries);
        }
    }
}
