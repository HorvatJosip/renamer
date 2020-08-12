using System;

namespace Renamer
{
    public class Message
    {
        private bool _addedLine;

        public static Message NewLine { get; } = new Message(Environment.NewLine);

        public string Content { get; private set; }

        public MessageType Type { get; }

        public Message(string content) : this(content, MessageType.Information) { }

        public Message(string content, MessageType type)
        {
            Content = content;
            Type = type;
        }

        public string AddLine(string line)
        { 
            if(_addedLine == false)
            {
                Content += Environment.NewLine;

                _addedLine = true;
            }

            Content += line + Environment.NewLine;

            return Content;
        }

        public override string ToString() => Content;
    }
}
