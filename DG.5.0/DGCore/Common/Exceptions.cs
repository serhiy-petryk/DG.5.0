using System;
using System.Text.Json;

namespace DGCore.Common
{
    public class LoadJsonConfigException : Exception
    {
        public string FileName { get; }
        public long? LineNumber { get; }
        public long? Position { get; }
        public override string Message { get; }
        public LoadJsonConfigException(string filename, Exception ex)
        {
            FileName = filename;
            if (ex is JsonException jsonException)
            {
                LineNumber = jsonException.LineNumber;
                Position = jsonException.BytePositionInLine;
            }
            Message = ex.Message;
        }
    }
}
