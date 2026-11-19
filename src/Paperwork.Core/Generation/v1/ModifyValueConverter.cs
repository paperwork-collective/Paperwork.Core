using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Paperwork.Generation.v1
{
    /// <summary>
    /// Reads a field's "modify" however it was written. The Designer writes it as one
    /// comma-separated string ("Template,Viewer" - see fieldEditorMgr.js, which splits it
    /// the same way), but every reader on the JavaScript side uses indexOf, which works on
    /// a string and an array alike, so both shapes exist and neither side ever noticed.
    /// Declaring it as a list alone made every existing template fail to parse at all.
    /// </summary>
    public class ModifyValueConverter : JsonConverter<List<string>?>
    {
        public override List<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return null;

                case JsonTokenType.String:
                    return Split(reader.GetString());

                case JsonTokenType.StartArray:
                    var all = new List<string>();
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            var one = reader.GetString();
                            if (!string.IsNullOrWhiteSpace(one))
                                all.Add(one!.Trim());
                        }
                        else
                        {
                            reader.Skip();
                        }
                    }
                    return all;

                default:
                    // Anything else is not a "modify" anyone wrote on purpose. Reading it as
                    // nothing keeps the template loadable, and nothing is treated as
                    // designer-only, which is the safe way to be wrong.
                    reader.Skip();
                    return null;
            }
        }

        private static List<string> Split(string? value)
        {
            var all = new List<string>();
            if (string.IsNullOrWhiteSpace(value))
                return all;

            foreach (var one in value!.Split(','))
            {
                var trimmed = one.Trim();
                if (trimmed.Length > 0)
                    all.Add(trimmed);
            }
            return all;
        }

        /// <summary>
        /// Written back out the way the Designer writes it, so a template that goes through
        /// Paperwork.Core comes out the same shape it went in.
        /// </summary>
        public override void Write(Utf8JsonWriter writer, List<string>? value, JsonSerializerOptions options)
        {
            if (null == value)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(string.Join(",", value));
        }
    }
}
