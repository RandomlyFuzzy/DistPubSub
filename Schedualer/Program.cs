using Microsoft.VisualBasic;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;

namespace Schedualer
{
    public class Program
    {
        static void Main(string[] args)
        {
            string path = "./Config.json";
            if (args.Length == 0)
            {
                Console.WriteLine("No arguments provided Defaulting to ./Config.json");
            }
            else
            {
                path = args[0];
            }

            if (!System.IO.File.Exists(path))
            {
                Console.WriteLine("File does not exist creating Default at " + path);
                System.IO.File.WriteAllText(path, Utilities.JsonSerialize(new Entry[] { new Entry() }));

                string script = @"@echo off 
                                    echo hello world         
                                ";
                System.IO.File.WriteAllText("test.cmd", script);
                Console.WriteLine("Default file created Exiting ... ");
            }

            string json = System.IO.File.ReadAllText(path);
            Entry[] entries = Utilities.JsonDeserialize(json);


            MainThread(entries).GetAwaiter().GetResult();
        }
        static async Task MainThread(Entry[] entries)
        {
            List<Thread> threads = new List<Thread>();
            foreach (var entry in entries)
            {
                //threads.Add(new Thread(async () =>
                //{
                try
                {
                    Console.WriteLine(entry.ToString());
                    var time = entry.AsyncGetTime();
                    while(time.MoveNext())
                    {
                        DateTime dt = await time.Current;
                        Console.WriteLine(dt);
                        await entry.Execute();
                    }
                }catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                Console.WriteLine("Thread Exited");
                //}));
                //threads.Last().Start();
            }
            Thread.Sleep(1000);
            while(!threads.All(a=>!a.IsAlive))
            {
                Thread.Sleep(1000);
            }
            Console.Read();
        }


        
    }
}
