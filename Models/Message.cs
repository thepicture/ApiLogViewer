using System;

namespace ApiLogViewer.Models
{
    public class Message
    {
        public TimeSpan StartTypingTime { get; set; }
        public TimeSpan EndTypingTime { get; set; }
        public string NickName { get; set; }
        public string Text { get; set; }
    }
}
