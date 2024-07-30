using System.Diagnostics;

namespace Schedualer
{
    public readonly struct Entry
    {
        public string Name { get; init; } = "Task";
        public string ScheduelType { get; init; } = "Relative";//Relative or Absolute
        //Relative interval
        public string[] Interval { get; init; } = ["00:00"];
        //Absolute time
        public string[] Days { get; init; } = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];//or "Weekday", "Weekend","Everyday"
        public string[] Times { get; init; } = ["00:00", "00:00"];
        public string Timezone { get; init; } = "UTC";
        public string[] Command { get; init; } = ["test.cmd"];
        public bool Debug { get; init; } = true;
        public Entry() { }

        public IEnumerable<DateTime> GetTime()
        {
            var Enum = ScheduelType.ToLower() switch
            {
                "relative" => GetTimeRelative(),
                "absolute" => GetTimeAbsolute(),
                _ => throw new NotImplementedException()
            };

            foreach (var item in Enum)
            {
                yield return item;
            }
        }
        IEnumerable<DateTime> GetTimeRelative()
        {
            List<DateTime> list = new List<DateTime>();
            foreach (string time in Interval)
            {
                (int Hour, int Mins) = time.EvalTime(time);
                DateTime start = DateTime.Now;
                list.Add(start.AddHours(Hour).AddMinutes(Mins).AddSeconds(-start.Second));
            }

            list.Sort();
            if (list.Any(a => a <= DateTime.Now))
            {
                throw new Exception("Time is in the past");
            }

            foreach (var item in list)
            {
                yield return item;
            }
        }
        IEnumerable<DateTime> GetTimeAbsolute()
        {
            while (true)
            {
                List<DateTime> list = new List<DateTime>();
                foreach (var Day in Days)
                {
                    foreach (string time in Times)
                    {
                        (int Hour, int Mins) = time.EvalTime(time);
                        list.Add(Day.EvaluateNext(time, Timezone));
                    }
                }
                list.Sort();
                if (list.Any(a=>a < DateTime.Now))
                {
                    throw new Exception("Time is in the past");
                }
                yield return list[0];
            }
        }
        public IEnumerator<Task<DateTime>> AsyncGetTime()
        {
            var Enum = ScheduelType.ToLower() switch
            {
                "relative" => GetTimeRelative(),
                "absolute" => GetTimeAbsolute(),
                _ => throw new NotImplementedException()
            };

            foreach (var item in Enum)
            {
                TimeSpan span = (DateTime)item - DateTime.Now;
                //Console.WriteLine(span.TotalSeconds);
                //await until the time is reached
                Task<DateTime> task =new Task<DateTime>(() => {Task.Delay(span).GetAwaiter().GetResult(); return item;});
                task.Start();
                yield return task;//.ContinueWith(a => item); ;


            }

        }

        public override string ToString()
        {
            return Name+" : " + string.Join(" ", Command);
        }

        public async Task Execute()
        {
            string cmdText = Name+" : "+string.Join(" ", Command);
            ConsoleColor color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Executing "+cmdText);
            Console.ForegroundColor = color;
            bool Redirection = Debug;
            Process p = Process.Start(new ProcessStartInfo()
            {
                FileName = Command[0],
                Arguments = string.Join(" ", Command.Skip(1)),
                UseShellExecute = !Redirection,
                RedirectStandardOutput = Redirection,
                RedirectStandardError = Redirection,
            });

            if(Redirection)
            {
                if (p == null)
                {
                    throw new Exception($"Process {Name} failed to start");
                }
                List<Task> tasks = new List<Task>();

                object locker = new object();
                tasks.Add(
                new Task(() =>
                    {
                        lock (locker)
                        {
                            ConsoleColor color = Console.ForegroundColor;
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine(cmdText);
                            Console.ForegroundColor = color;

                            while (!p.HasExited)
                            {
                                if (p.StandardOutput.Peek() != -1)
                                {
                                    Thread.Sleep(100);
                                }
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("sout\t: " + p.StandardOutput.ReadLine());
                                Console.ForegroundColor = color;
                            }
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine(cmdText + " : EXITED");
                            Console.ForegroundColor = color;
                            Console.ForegroundColor = color;
                        }
                    }
                ));
                tasks.Last().Start();

                tasks.Add(
                new Task(() =>
                    {
                        lock (locker)
                        {
                            ConsoleColor color = Console.ForegroundColor;
                            while (!p.HasExited)
                            {
                                if (p.StandardError.Peek() == -1)
                                {
                                    Thread.Sleep(100);
                                }
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("err\t: " + p.StandardError.ReadLine());
                                Console.ForegroundColor = color;
                            }
                            Console.ForegroundColor = color;
                        }
                    }
                ));
                tasks.Last().Start();
                //await Task.WhenAll(tasks);
            }
        }
    }
}
