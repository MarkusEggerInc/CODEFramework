using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Xml;
using CODE.Framework.Core.Newtonsoft;
using CODE.Framework.Core.Utilities;

namespace CODE.Framework.Services.Server
{
    class NewtonsoftJsonDispatchFormatter : IDispatchMessageFormatter
    {
        readonly OperationDescription _operation;
        readonly Dictionary<string, int> _parameterNames;
        public NewtonsoftJsonDispatchFormatter(OperationDescription operation, bool isRequest)
        {
            _operation = operation;
            if (!isRequest) return;
            var operationParameterCount = operation.Messages[0].Body.Parts.Count;
            if (operationParameterCount <= 1) return;
            _parameterNames = new Dictionary<string, int>();
            for (var i = 0; i < operationParameterCount; i++)
                _parameterNames.Add(operation.Messages[0].Body.Parts[i].Name, i);
        }

        public void DeserializeRequest(Message message, object[] parameters)
        {
            object bodyFormatProperty;
            if (!message.Properties.TryGetValue(WebBodyFormatMessageProperty.Name, out bodyFormatProperty)) throw new InvalidOperationException("Incoming messages must have a body format of Raw. Is a ContentTypeMapper set on the WebHttpBinding?");
            var webBodyFormatMessageProperty = bodyFormatProperty as WebBodyFormatMessageProperty;
            if (webBodyFormatMessageProperty == null || webBodyFormatMessageProperty.Format != WebContentFormat.Raw) throw new InvalidOperationException("Incoming messages must have a body format of Raw. Is a ContentTypeMapper set on the WebHttpBinding?");

            var bodyReader = message.GetReaderAtBodyContents();
            bodyReader.ReadStartElement("Binary");
            var rawBody = bodyReader.ReadContentAsBase64();
            using (var ms = new MemoryStream(rawBody))
            using (var sr = new StreamReader(ms))
            {
                var serializer = new JsonSerializer();
                if (parameters.Length == 1)
                    // single parameter, assuming bare
                    parameters[0] = serializer.Deserialize(sr, _operation.Messages[0].Body.Parts[0].Type);
                else
                {
                    // multiple parameter, needs to be wrapped
                    var reader = new JsonTextReader(sr);
                    reader.Read();
                    if (reader.TokenType != JsonToken.StartObject) throw new InvalidOperationException("Input needs to be wrapped in an object");
                    reader.Read();
                    while (reader.TokenType == JsonToken.PropertyName)
                    {
                        var parameterName = reader.Value as string;
                        reader.Read();
                        if (parameterName != null && _parameterNames.ContainsKey(parameterName))
                        {
                            var parameterIndex = _parameterNames[parameterName];
                            parameters[parameterIndex] = serializer.Deserialize(reader, _operation.Messages[0].Body.Parts[parameterIndex].Type);
                        }
                        else
                            reader.Skip();

                        reader.Read();
                    }

                    reader.Close();
                }

                sr.Close();
                ms.Close();
            }
        }

        public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            var json = JsonHelper.SerializeToRestJson(result);
            var replyMessage = Message.CreateMessage(messageVersion, _operation.Messages[1].Action, GetBytes(json));
            replyMessage.Properties.Add(WebBodyFormatMessageProperty.Name, new WebBodyFormatMessageProperty(WebContentFormat.Raw));
            var responseProperty = new HttpResponseMessageProperty();
            responseProperty.Headers[HttpResponseHeader.ContentType] = "application/json";
            replyMessage.Properties.Add(HttpResponseMessageProperty.Name, responseProperty);
            return replyMessage;
        }

        private static byte[] GetBytes(string str)
        {
            var bytes = new byte[str.Length * sizeof(char)];
            Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }
    }

    class RawBodyWriter : BodyWriter
    {
        readonly byte[] _content;
        public RawBodyWriter(byte[] content) : base(true)
        {
            _content = content;
        }

        protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
        {
            writer.WriteStartElement("Binary");
            writer.WriteBase64(_content, 0, _content.Length);
            writer.WriteEndElement();
        }
    }
}