using Prosa.Log4View.Interfaces;
using Prosa.Log4View.Level;
using Prosa.Log4View.Message;
using Prosa.Log4View.Parser;
using Prosa.Log4View.Receiver;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Prosa.Log4View.Log4jReceiver
{
    public class Log4jXmlParser : Parser.MessageParser.MessageParser
    {
        private readonly NameTable _nameTable = new NameTable();
        private readonly XmlReaderSettings _xmlReaderSettings = new XmlReaderSettings();
        private readonly XmlParserContext _context;
        private readonly XmlNamespaceManager _namespaceMgr;
        private int _lineNumber;

        public Log4jXmlParser(ILogReceiver receiver, string logSource)
          : base(receiver, logSource)
        {
            this._namespaceMgr = new XmlNamespaceManager(this._nameTable);
            this._context = new XmlParserContext(this._nameTable, this._namespaceMgr, null, XmlSpace.None);
            this._xmlReaderSettings.ConformanceLevel = ConformanceLevel.Fragment;
            this._xmlReaderSettings.IgnoreWhitespace = true;
            this._xmlReaderSettings.IgnoreComments = true;
            this._xmlReaderSettings.IgnoreProcessingInstructions = true;
            this._xmlReaderSettings.CheckCharacters = false;
            this._namespaceMgr.AddNamespace("log4j", "ns");
            if (this.Receiver.LoggingFramework == null)
                return;
            foreach (string xmlNamespace in this.Receiver.LoggingFramework.XmlNamespaces)
                this._namespaceMgr.AddNamespace(xmlNamespace, "ns");
        }

        public override MessageBlock Parse(
          InputBuffer buffer,
          DateTimeOffset? readFrom = null,
          DateTimeOffset? readUntil = null,
          int? maxMessageCount = null,
          CancellationToken ct = default(CancellationToken))
        {
            return this.ParseWithUnprocessed(buffer, null, readFrom, readUntil, maxMessageCount, ct);
        }

        public MessageBlock ParseWithUnprocessed(
          InputBuffer buffer,
          UnprocessedString unprocessedData,
          DateTimeOffset? readFrom = null,
          DateTimeOffset? readUntil = null,
          int? maxMessageCount = null,
          CancellationToken ct = default(CancellationToken))
        {
            MessageBlock messageBlock = new MessageBlock();
            if (unprocessedData == null)
                unprocessedData = new UnprocessedString();
            this._lineNumber = 0;
            bool flag = false;
            int num1 = Math.Max(500, buffer.LineCount / 10);
            foreach (string line in buffer.Lines)
            {
                string str1 = line.Trim(new char[1]);
                if (!string.IsNullOrEmpty(str1))
                {
                    int num2 = 0;
                    int length1 = unprocessedData.Length;
                    unprocessedData.Append(str1);
                    int startIndex = num2;
                    int num3 = 0;
                    do
                    {
                        if (num3 > -1)
                            num3 = str1.IndexOf("event>", startIndex, StringComparison.OrdinalIgnoreCase);
                        string str2 = flag ? "]]>" : "<![CDATA[";
                        int num4 = str1.IndexOf(str2, startIndex, StringComparison.OrdinalIgnoreCase);
                        if (num4 >= 0 && (num3 <= 0 || num4 < num3))
                        {
                            flag = !flag;
                            startIndex = num4 + str2.Length;
                        }
                        else
                            startIndex = num3 < 0 ? -1 : num3 + "event>".Length;
                    }
                    while ((num3 < 0 || num3 > startIndex) && (startIndex >= 0 && startIndex < str1.Length));
                    int length2 = length1 + num3 + "event>".Length - num2;
                    while (!flag && num3 >= 0 && length2 > 0)
                    {
                        LogMessage xmlElement = this.ParseXmlElement(unprocessedData.Substring(num2, length2), buffer.Id);
                        if (xmlElement != null)
                        {
                            if (readUntil.HasValue)
                            {
                                DateTimeOffset? nullable = readUntil;
                                DateTimeOffset adjustedTime = xmlElement.AdjustedTime;
                                if ((nullable.HasValue ? (nullable.GetValueOrDefault() < adjustedTime ? 1 : 0) : 0) != 0)
                                {
                                    this.NotifyProgress(1.0);
                                    return messageBlock;
                                }
                            }
                            if (!readFrom.HasValue || xmlElement.AdjustedTime >= readFrom.Value)
                            {
                                messageBlock.Add(xmlElement);
                                if (maxMessageCount.HasValue)
                                    return messageBlock;
                            }
                        }
                        num2 += length2;
                        if (num2 == unprocessedData.Length)
                        {
                            num3 = -1;
                            length2 = 0;
                        }
                        else
                        {
                            num3 = unprocessedData.IndexOfValueInLines("event>", num2, StringComparison.OrdinalIgnoreCase);
                            length2 = num3 - num2;
                        }
                    }
                    unprocessedData.Shorten(num2);
                }
                if (ct.IsCancellationRequested)
                    return messageBlock;
                if (++this._lineNumber % num1 == 0)
                    this.NotifyProgress(this._lineNumber * 1.0 / buffer.LineCount);
            }
            this.NotifyProgress(1.0);
            return messageBlock;
        }

        private LogMessage ParseXmlElement(string orgLine, int bufferId)
        {
            using (XmlReader reader = XmlReader.Create(new StringReader(orgLine), this._xmlReaderSettings, this._context))
            {
                LogMessage logMessage = null;
                try
                {
                    while (reader.Read())
                    {
                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                if (reader.LocalName.Equals("event", StringComparison.OrdinalIgnoreCase))
                                {
                                    ConstructorInfo c = typeof(LogMessage).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] {typeof(ILogReceiver), typeof(int), typeof(string)}, null);
                                    logMessage = (LogMessage)c.Invoke(new object[] { this.Receiver, bufferId, this.LogSource });
                                    
                                    this.ParseMessageAttributes(reader, logMessage);
                                    continue;
                                }
                                if (logMessage != null)
                                {
                                    this.ParseMessageDetails(reader, logMessage);
                                    continue;
                                }
                                continue;
                            case XmlNodeType.EndElement:
                                if (reader.Depth == 0)
                                    return logMessage == null || !(logMessage.Source != "ignore") ? null : logMessage;
                                continue;
                            default:
                                continue;
                        }
                    }
                }
                catch (XmlException ex)
                {
                    this.Logger.Debug("XmlException parsing Xml stream", ex);
                    if (!ex.Message.Contains("Unexpected end of file has occurred"))
                    {
                        this.Receiver.ReportReceiveError(this.LogSource, this._lineNumber, orgLine, null);
                    }
                    int num = ex.Message.IndexOf("' is an undeclared namespace", StringComparison.Ordinal);
                    if (num != -1)
                    {
                        this._namespaceMgr.AddNamespace(ex.Message.Substring(1, num - 1).Trim(), "ignore");
                        this._namespaceMgr.PushScope();
                    }
                }
            }
            return null;
        }

        private void ParseMessageDetails(XmlReader reader, LogMessage logMessage)
        {
            string localName = reader.LocalName.ToLowerInvariant();
            switch (localName)
            {
                case "message":
                    logMessage.Message = this.PoolString(reader.ReadString().Replace("&#xD;&#xA;", Environment.NewLine));
                    break;
                case "exception":
                case "throwable":
                    logMessage.Exception = this.PoolString(reader.ReadString().Replace("&#xD;&#xA;", Environment.NewLine));
                    break;
                case "thrown":
                    this.ParseTrownAttributes(reader, logMessage);
                    break;
                case "extendedstacktraceitem":
                    this.ParseExtendedStackTraceItemAttributes(reader, logMessage);
                    break;
                case "ndc":
                    logMessage.NDC = this.PoolString(reader.ReadString().Replace("&#xD;&#xA;", Environment.NewLine));
                    break;
                case "data":
                    this.GetDataProperties(reader, logMessage);
                    break;
                case "instant":
                    this.ParseInstantAttributes(reader, logMessage);
                    break;
                case "locationinfo":
                    this.ParseLocationAttributes(reader, logMessage);
                    break;
            }
        }

        private void ParseInstantAttributes(XmlReader reader, LogMessage logMessage)
        {
            while (reader.MoveToNextAttribute())
            {
                string name = reader.Name.ToLowerInvariant();
                string str = reader.Value;

                switch (name)
                {
                    case "epochsecond":
                        logMessage.Time = fromXmlString(str, logMessage.Receiver.TimeZone, true);
                        break;
                    default:
                        break;
                }
            }
        }

        private void ParseExtendedStackTraceItemAttributes(XmlReader reader, LogMessage logMessage)
        {
            string stackClass = "";
            string stackMethod = "";
            string stackFile = "";
            string stackLine = "";
            string stackLocation = "";
            string stackVersion = "";

            while (reader.MoveToNextAttribute())
            {
                string name = reader.Name.ToLowerInvariant();
                string str = reader.Value;

                switch (name)
                {
                    case "class":
                        stackClass = str;
                        break;
                    case "method":
                        stackMethod = str;
                        break;
                    case "file":
                        stackFile = str;
                        break;
                    case "line":
                        stackLine = str;
                        break;
                    case "location":
                        stackLocation = str;
                        break;
                    case "version":
                        stackVersion = str;
                        break;
                    default:
                        break;
                }
            }
            logMessage.StackTrace = $"{logMessage.StackTrace} at {stackClass}.{stackMethod}({stackFile}:{stackLine})[{stackLocation}:{stackVersion}]{Environment.NewLine}";
            logMessage.Exception = $"{logMessage.Exception} at {stackClass}.{stackMethod}({stackFile}:{stackLine})[{stackLocation}:{stackVersion}]{Environment.NewLine}";

        }

        private void ParseTrownAttributes(XmlReader reader, LogMessage logMessage)
        {
            while (reader.MoveToNextAttribute())
            {
                string name = reader.Name.ToLowerInvariant();
                string str = reader.Value;
                switch (name)
                {
                    case "localizedmessage":
                        logMessage.Message = str;
                        break;
                    case "name":
                        logMessage.Exception = $"{str}: {logMessage.Message}{Environment.NewLine}";
                        break;
                    default:
                        break;
                }
            }
        }

        private void ParseMessageAttributes(XmlReader reader, LogMessage logMessage)
        {
            while (reader.MoveToNextAttribute())
            {
                string name = reader.Name.ToLowerInvariant();
                string str = reader.Value;
                switch (name)
                {
                    case "domain":
                        logMessage.Domain = this.PoolString(str);
                        continue;
                    case "identity":
                        logMessage.Identity = this.PoolString(str);
                        continue;
                    case "level":
                        logMessage.LogLevel = Levels.Get(str);
                        continue;
                    case "logger":
                    case "loggername":
                        logMessage.Logger = this.PoolString(str);
                        continue;
                    case "thread":
                    case "threadid":
                        logMessage.Thread = this.PoolString(str);
                        int result;
                        if (int.TryParse(str, out result))
                        {
                            logMessage.ThreadId = result;
                            continue;
                        }
                        continue;
                    case "timestamp":
                        logMessage.Time = fromXmlString(str, logMessage.Receiver.TimeZone);
                        continue;
                    case "username":
                        logMessage.User = this.PoolString(str);
                        continue;
                }

                logMessage.SetField(reader.Name, this.PoolString(str.Replace("&#xD;&#xA;", Environment.NewLine)));
            }
        }

        private static DateTimeOffset fromXmlString(String xmlTime, TimeZoneInfo timeZone, bool isSeconds = false)
        {
            try
            {
                if (xmlTime.Contains("-"))
                {
                    DateTimeOffset parsedTime;
                    if (DateTimeOffset.TryParseExact(xmlTime, "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeLocal, out parsedTime))
                    {
                        return AdjustTimeZone(parsedTime, timeZone);
                    }
                }
                long javaTimeStamp;
                if (long.TryParse(xmlTime, out javaTimeStamp))
                {
                    DateTimeOffset javaTimestampOffset = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeZoneInfo.Utc.BaseUtcOffset);

                    return AdjustTimeZone(isSeconds ? javaTimestampOffset.AddSeconds(javaTimeStamp) : javaTimestampOffset.AddMilliseconds(javaTimeStamp), timeZone);
                }
            }
            catch { }
            return DateTimeOffset.MinValue;
        }

        private static DateTimeOffset AdjustTimeZone(DateTimeOffset parsedTime, TimeZoneInfo timeZone)
        {
            if (timeZone == null)
            {
                return parsedTime;
            }
            TimeSpan utcOffset = timeZone.GetUtcOffset(parsedTime.DateTime);
            return new DateTimeOffset(parsedTime.Ticks, utcOffset);
        }

        private void ParseLocationAttributes(XmlReader reader, LogMessage logMessage)
        {
            while (reader.MoveToNextAttribute())
            {
                string str = this.PoolString(reader.Value);
                string name = reader.Name.ToLowerInvariant();
                switch (name)
                {
                    case "class":
                        logMessage.Class = str;
                        continue;
                    case "method":
                        logMessage.Method = str;
                        continue;
                    case "file":
                        logMessage.File = str;
                        continue;
                    case "line":
                        logMessage.Line = str;
                        continue;
                    default:
                        logMessage.SetField(reader.Name, str);
                        continue;
                }
            }
            reader.MoveToElement();
        }

        private void GetDataProperties(XmlReader reader, LogMessage logMessage)
        {
            if (reader.MoveToAttribute("name"))
            {
                string fieldName = reader.Value.ToLowerInvariant();
                if (reader.MoveToAttribute("value") && reader.Value != "(null)")
                {
                    string str = reader.Value;
                    switch (fieldName)
                    {
                        case "hostname":
                        case "log4jmachinename":
                            logMessage.Host = this.PoolString(str);
                            break;
                        case "log4japp":
                            logMessage.Domain = this.PoolString(str);
                            break;
                        case "ndc":
                            logMessage.NDC = this.PoolString(str.Replace("&#xD;&#xA;", Environment.NewLine));
                            break;
                        case "mdc":
                            logMessage.MDC = this.PoolString(str.Replace("&#xD;&#xA;", Environment.NewLine));
                            break;
                        default:
                            logMessage.SetField(reader.Value, this.PoolString(str.Replace("&#xD;&#xA;", Environment.NewLine)));
                            break;
                    }
                }
            }
            reader.MoveToElement();
        }
    }
}
