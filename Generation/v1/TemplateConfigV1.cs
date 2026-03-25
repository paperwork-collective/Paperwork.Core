//#define OUTPUT_TO_CONSOLE

using System;
using System.Text.Json.Serialization;

namespace Paperwork.Services.Generation.v1
{
    public class TemplateConfigV1 : TemplateConfigBase
    {
        public const string DefaultMainLayoutName = "main";
        public const string DataParameterPrefix = "{{";
        public const string DataParameterPostfix = "}}";

        [JsonPropertyName("data")]
        public List<DataContent> Data { get; set; }

        [JsonPropertyName("layouts")]
        public List<LayoutContent> Layout { get; set; }

        [JsonPropertyName("styles")]
        public List<StyleContent> Style { get; set; }

        [JsonPropertyName("mainLayout")]
        public override string MainLayoutName { get; set; }

        [JsonPropertyName("parameters")]
        public List<DataParameter> Parameters { get; set; }


        public TemplateConfigV1() : base(1, 1, DefaultMainLayoutName)
        {
            this.Data = new List<DataContent>();
            this.Layout = new List<LayoutContent>();
            this.Style = new List<StyleContent>();
        }

        public override List<TemplateItemContentBase> DataContent()
        {
            return new List<TemplateItemContentBase>(Data.ToArray());
        }

        public override List<TemplateItemContentBase> StyleContent()
        {
            return new List<TemplateItemContentBase>(Style.ToArray());
        }

        public override List<TemplateItemContentBase> LayoutContent()
        {
            return new List<TemplateItemContentBase>(Layout.ToArray()); ;
        }


        public override async Task<bool> DoLoad(LoadActionAsync loader)
        {

            bool success = true;
            var errorList = new List<Exception>();

            if (null != Layout && this.Layout.Count > 0)
            {
                foreach (var layout in this.Layout)
                {
                    var loaded = await this.EnsureConfigLoaded(layout, loader, "text/html", errorList);
                    success |= loaded;
                }
            }
            else
                success = false;

            if (null != Style && this.Style.Count > 0)
            {
                foreach (var style in this.Style)
                {
                    var loaded = await this.EnsureConfigLoaded(style, loader, "text/css", errorList);
                    success |= loaded;
                }

            }



            if (null != Data)
            {
                foreach (var data in Data)
                {
                    var loaded = await this.EnsureConfigLoaded(data, loader, "text/json", errorList);
                    success |= loaded;
                }
            }

            if(errorList.Count == 0)
            {
                return success;
            }
            else if(errorList.Count == 1)
            {
                throw new FileLoadException("Cound not load one or more of the resources. " + errorList[0].Message, errorList[0]);
            }
            else
            {
                throw new FileLoadException("Multiple errors occured during the load of the template. First error : " + errorList[0].Message, errorList[0]);
            }

        }

        private string EnsureUrlParametersReplaced(string original, List<DataParameter> parameters)
        {
            if (original.IndexOf(DataParameterPrefix) >= 0)
            {
                var updated = original;
                foreach (var p in parameters)
                {
                    var toReplace = p.ToString();
                    updated = updated.Replace(toReplace, System.Uri.EscapeDataString(p.Value));
                }

                return updated;
            }
            else
            {
                return original;
            }
        }

        private async Task<bool> EnsureConfigLoaded(ContentRefBase config, LoadActionAsync loader, string mimetype, List<Exception> errors)
        {
            string value = string.Empty;
            bool success = false;

#if OUTPUT_TO_CONSOLE
            Console.WriteLine("Ensuring config loaded with " + (this.Parameters == null ? "0" : this.Parameters.Count.ToString()) + " parameters");
#endif
            try
            {
                ConfigType type;
                ConfigFormat format;

                if (!string.IsNullOrEmpty(config.Type))
                {
                    type = Enum.Parse<ConfigType>(config.Type, true);
#if OUTPUT_TO_CONSOLE
                    Console.WriteLine("Found item type of " + type);
#endif
                    switch (type)
                    {
                        case (ConfigType.Content):
                            if (!string.IsNullOrEmpty(config.Content))
                                value = config.Content;
                            else
                                value = string.Empty;
                            break;


                        case (ConfigType.Source):
                            var url = config.Source;

                            if (string.IsNullOrEmpty(url))
                                throw new ArgumentNullException("The configuration item " + (config.Name ?? "UNNAMED") + " does not have a source set when the remote type is Source");

                            if(this.Parameters != null && this.Parameters.Count > 0)
                            {
                                url = this.EnsureUrlParametersReplaced(url, this.Parameters);
                            }
#if OUTPUT_TO_CONSOLE
                            Console.WriteLine("Item type is source so loading the content from " +  url);
#endif
                            value = (await loader(url, config.Auth, mimetype)) as string;

#if OUTPUT_TO_CONSOLE
                            Console.WriteLine("Received content result for loader for " + url + " with length " + (null == value ? "0" : value.Length));
#endif

                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(config.Type), "The template config item type " + config.Type + " cound not be understood");
                    }




                }
                else if (!string.IsNullOrEmpty(config.Content))
                {
                    value = config.Content;

                }
                else if (!string.IsNullOrEmpty(config.Source))
                {
                    var url = config.Source;

                    if (string.IsNullOrEmpty(url))
                        throw new ArgumentNullException("The configuration item " + (config.Name ?? "UNNAMED") + " does not have a source set when the remote type is Source");

                    if (this.Parameters != null && this.Parameters.Count > 0)
                    {
                        url = this.EnsureUrlParametersReplaced(url, this.Parameters);
                    }
#if OUTPUT_TO_CONSOLE
                    Console.WriteLine("Item type is source so loading the content from " +  url);
#endif
                    value = (await loader(url, config.Auth, mimetype)) as string;

#if OUTPUT_TO_CONSOLE
                    Console.WriteLine("Received content result for loader for " + url + " with length " + (null == value ? "0" : value.Length));
#endif

                    
                }
                else
                {
                    value = string.Empty;
                }

                //Convert from base 64 if needed

                if (!string.IsNullOrEmpty(value))
                {
                    if (!string.IsNullOrEmpty(config.Format))
                    {
                        
                        if (config.Format == "base64" || config.Format.EndsWith(";base64"))
                        {
                            value = FromBase64ToString(value);
                        }
                        
                    }

                    config.SetContentValue(value);
                    success = true;
                }
            }
            catch (Exception ex)
            {
                success = false;
                errors.Add(new TemplateLoadException("Could not load the data for " + (config.Name ?? "Un-named") + ", see the inner exception for more details", ex));
            }

            return success;
        }
    }

    public enum ConfigType
    {
        Content,
        Source
    }

    public enum ConfigFormat
    {
        String,
        Base64
    }

    public class ContentRefBase : TemplateItemContentBase
    {
        [JsonPropertyName("name")]
        public override string Name { get; set; }

        [JsonPropertyName("source")]
        public virtual string Source { get; set; }

        [JsonPropertyName("content")]
        public virtual string Content { get; set; }

        [JsonPropertyName("type")]
        public virtual string Type { get; set; }

        [JsonPropertyName("format")]
        public virtual string Format { get; set; }

        [JsonPropertyName("auth")]
        public virtual string Auth { get; set; }

        private string _innerValue;
        private bool _loaded;

        public ContentRefBase()
        {
            this.Name = "";
            this.Source = "";
            this.Content = "";
            this.Type = "";
            this.Format = "";
            this.Auth = "";

            this._innerValue = "";
            this._loaded = false;
        }

        public void SetContentValue(string value)
        {
            this._innerValue = value;
            this._loaded = true;
        }


        public override string Value()
        {
            return this._innerValue;
        }

        public override string BaseSource()
        {
            return this.Source;
        }
    }

    public class LayoutContent : ContentRefBase
    { }

    public class StyleContent : ContentRefBase
    { }

    public class DataContent : ContentRefBase
    { }

    public class AuthToken
    {
        public string Name { get; set; }

        public string Token { get; set; }
    }

    public class DataParameter
    {
        [JsonPropertyName("id")]
        public string ID { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("desc")]
        public string Desc { get; set; }

        public bool Required { get; set; } = true;

        public DataParameter() { }

        

        public override string ToString()
        {

            if (string.IsNullOrEmpty(this.ID))
            {
                return TemplateConfigV1.DataParameterPrefix + "UNNAMED" + TemplateConfigV1.DataParameterPostfix;
            }
            else
            {
                return TemplateConfigV1.DataParameterPrefix + this.ID + TemplateConfigV1.DataParameterPostfix;
            }
        }

    }

    public class TemplateLoadException : Scryber.PDFException
    {
        public TemplateLoadException(string message, Exception inner) : base(message, inner)
        { }
    }
}

