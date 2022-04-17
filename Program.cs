using ApiLogViewer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LogViewer
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            Timer timer = null;
            bool isPlaying = false;
            bool isNotTheEndOfTheLog = true;
            Queue<Message> messages = new Queue<Message>();
            TimeSpan timeSpan = TimeSpan.Zero;
            Task task = Task.Run(() =>
            {
                do
                {
                    if (Console.KeyAvailable)
                    {
                        ConsoleKey key = Console.ReadKey(intercept: true).Key;
                        if (key == ConsoleKey.Spacebar)
                        {
                            isPlaying = !isPlaying;
                        }
                        else if (key == ConsoleKey.Escape)
                        {
                            timer.Dispose();
                            break;
                        }
                        else if (key == ConsoleKey.LeftArrow
                                 && timeSpan > TimeSpan.Zero)
                        {
                            timeSpan -= TimeSpan.FromSeconds(1);
                        }
                        else if (key == ConsoleKey.RightArrow
                                 && timeSpan < messages.Max(
                                     m => m.EndTypingTime))
                        {
                            timeSpan += TimeSpan.FromSeconds(1);
                        }
                    }
                    string playingState = isPlaying && isNotTheEndOfTheLog
                    ? "Is playing"
                    : "Is stopped";
                    Console.Title = AssemblyName.GetAssemblyName(
                        Assembly.GetExecutingAssembly().Location)
                        + " - "
                        + timeSpan.ToString()
                        + " - "
                        + playingState
                        + " - Spacebar to change the playback state"
                        + " - Escape to quit";
                }
                while (isNotTheEndOfTheLog);
            });
            using (System.Windows.Forms.OpenFileDialog openLogDialog =
                new System.Windows.Forms.OpenFileDialog()
                {
                    Title = "Select the file with the log",
                })
            {
                bool isSelectedFile = openLogDialog.ShowDialog()
                       == System.Windows.Forms.DialogResult.OK;
                if (isSelectedFile)
                {
                    string[] lines = null;
                    try
                    {
                        lines = File.ReadAllLines(openLogDialog.FileName);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Cannot parse lines. "
                            + "File may not exist "
                            + "or is prohibited to read "
                            + "due to access rights");
                        Console.ReadKey(true);
                        return;
                    }
                    StringBuilder currentText = new StringBuilder();
                    Message currentMessage = null;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        try
                        {
                            if (string.IsNullOrWhiteSpace(lines[i]))
                            {
                                if (currentMessage != null)
                                {
                                    currentMessage.Text = currentText
                                        .ToString();
                                    messages.Enqueue(currentMessage);
                                    currentMessage = null;
                                }
                                continue;
                            }
                            else if (currentMessage == null)
                            {
                                currentMessage = new Message();
                                currentText.Clear();
                                string[] timeDataProbably = lines[i]
                                    .Split(',');
                                if (TimeSpan.TryParse(
                                    timeDataProbably[0].Substring(0, 8),
                                    out TimeSpan startTypingTime)
                                    && TimeSpan.TryParse(timeDataProbably[1]
                                    .Substring(0, 8),
                                    out TimeSpan endTypingTime))
                                {
                                    currentMessage.StartTypingTime =
                                        startTypingTime;
                                    currentMessage.EndTypingTime =
                                        endTypingTime;
                                    currentMessage.NickName =
                                        lines[i + 1].Split(':')[0];
                                    currentText.AppendLine(
                                        lines[i + 1].Split(new string[]
                                        {
                                            ": "
                                        },
                                        2,
                                        StringSplitOptions
                                        .RemoveEmptyEntries)[1]);
                                    i++;
                                    continue;
                                }
                            }
                            currentText.AppendLine(lines[i]);
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Cannot parse time or text. "
                                              + "Check format of the log");
                            Console.ReadKey(true);
                            return;
                        }
                    }
                    isPlaying = true;
                    timer = new Timer((object state) =>
                    {
                        if (timeSpan >= messages.Max(m => m.EndTypingTime))
                        {
                            isNotTheEndOfTheLog = false;
                            timer.Dispose();
                            return;
                        }
                        if (!isPlaying)
                        {
                            return;
                        }
                        timeSpan = timeSpan.Add(
                            TimeSpan.FromSeconds(1));
                        Message startMessage = messages.FirstOrDefault(
                            m => m.StartTypingTime == timeSpan);
                        if (startMessage != null)
                        {
                            Console.WriteLine(
                                startMessage.NickName + " is typing...");
                        }
                        else
                        {
                            Message endMessage = messages.FirstOrDefault(
                                m => m.EndTypingTime == timeSpan);
                            if (endMessage != null)
                            {
                                Console.WriteLine(endMessage.EndTypingTime
                                    + ": "
                                    + endMessage.Text);
                            }
                        }
                    }, null, 0, 1000);
                }
            }
            PrintDecorationLine();
            Console.WriteLine("Start of the log"
                .ToUpper());
            PrintDecorationLine();
            task.Wait();
            isPlaying = false;
            PrintDecorationLine();
            Console.WriteLine("End of the log"
                .ToUpper());
            PrintDecorationLine();
            Console.WriteLine("Press any key to quit"
                .ToUpper());
            Console.ReadKey(false);
        }

        private static void PrintDecorationLine()
        {
            Console.WriteLine(
                string.Join(string.Empty,
                    Enumerable.Repeat("=", Console.BufferWidth - 1)));
        }
    }
}
