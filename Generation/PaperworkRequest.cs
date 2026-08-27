using System;
namespace Paperwork.Generation
{
    public class PaperworkRequest
    {
        /// <summary>
        /// Gets or sets the TemplateDefinition content as a string
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the Major version of the template definition schema - Default is 1
        /// </summary>
        public int MajorVersion { get; set; }

        /// <summary>
        /// Gets or sets the Minor version of the Template definition schema - Default is 1
        /// </summary>
        public int MinorVersion { get; set; }

        /// <summary>
        /// Gets or sets the Format of the Template config (Base64, Content, or Remote)
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// Gets or sets the url for the source of the Template Config
        /// </summary>
        public string BaseSource { get; set; }

        /// <summary>
        /// Gets or sets the format for the output required (only PDF is currently supported)
        /// </summary>
        public string Output { get; set; }


        /// <summary>
        /// Gets or sets the list of authenticated services
        /// </summary>
        public List<PaperworkRequestAuthToken> AuthTokens {get;set;}

        public List<PaperworkRequestField> Fields { get; set; }

        /// <summary>
        /// Gets or sets the rendering output options for this request
        /// </summary>
        public PaperworkRequestRenderOptions RenderOptions { get; set; }

        /// <summary>
		/// Gets or sets the start time offset in milliseconds sing the UNIX Epoch start time (Jan 1st 1970 UTC).
		/// </summary>
		public long EpochStartOffset { get; set; }

        /// <summary>
        /// Creates a new Generation request
        /// </summary>
        public PaperworkRequest()
        {
            this.MajorVersion = 1;
            this.MinorVersion = 1;
            this.EpochStartOffset = -1;
            this.Content = string.Empty;
            this.Format = "Content";
            this.Output = "application/pdf";
            this.BaseSource = string.Empty;
            this.RenderOptions = new PaperworkRequestRenderOptions();
            this.AuthTokens = new List<PaperworkRequestAuthToken>();
            this.Fields = new List<PaperworkRequestField>();
        }
    }

    public class PaperworkRequestAuthToken
    {
        public string Name { get; set; }

        public string Token { get; set; }

        public PaperworkRequestAuthToken()
        {
            this.Name = string.Empty;
            this.Token = string.Empty;
        }
    }

    /// <summary>
    /// Represents a single field that can be used within the template
    /// </summary>
    public class PaperworkRequestField
    {
        public string Id { get; set; }

        public string Type { get; set; }

        /// <summary>
        /// The field's value. Widened from string to object so non-scalar values
        /// (e.g. a List Group's array-of-entry-objects) can round-trip through
        /// JSON deserialization without throwing - System.Text.Json deserializes
        /// an object-typed property into a boxed JsonElement, which Scryber's
        /// binding engine already consumes directly elsewhere (see the JSON data
        /// params handling in PDFGeneratorV1).
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// True if this field has a genuinely empty value - a null reference, an
        /// empty/whitespace string, or a JsonElement representing null/undefined/an
        /// empty string. Non-scalar values (arrays, objects) are never "empty" here,
        /// even if they contain zero items - that's a valid List Group with no
        /// entries yet, not a missing value.
        /// </summary>
        public bool HasEmptyValue()
        {
            if (Value == null) return true;
            if (Value is string s) return string.IsNullOrEmpty(s);
            if (Value is System.Text.Json.JsonElement el)
            {
                switch (el.ValueKind)
                {
                    case System.Text.Json.JsonValueKind.Null:
                    case System.Text.Json.JsonValueKind.Undefined:
                        return true;
                    case System.Text.Json.JsonValueKind.String:
                        return string.IsNullOrEmpty(el.GetString());
                    default:
                        return false;
                }
            }
            return false;
        }

    }

    public class PaperworkRequestRenderOptions
    {

        public bool UseAsync { get; set; }

        public PaperworkRequestLogOption LogLevel { get; set; }

        //public PaperworkRequestCacheOption Cache { get; set; }
        
        public PaperworkOverlayGrid Overlay { get; set; }

        public PaperworkRequestRenderOptions()
        {
            this.LogLevel = PaperworkRequestLogOption.Off;
            //this.Cache = PaperworkRequestCacheOption.Static;
        }
    }

    public class PaperworkOverlayGrid
    {
        public bool Show { get; set; } = false;

        public int Spacing { get; set; } = DefaultSpacing;

        public int MajorCount { get; set; } = DefaultMajor;

        public string Color { get; set; } = DefaultColor;

        public double Opacity { get; set; } = DefaultOpacity;

        public double LineThickness { get; set; } = DefaultThickness;

        public static string DefaultColor = "#00FFFF";
        public static double DefaultOpacity = 0.5;
        public static int DefaultSpacing = 10;
        public static int DefaultMajor = 5;
        public static double DefaultThickness = 0.5;

    }

    public enum PaperworkRequestLogOption
    {
        Off = 0,
        Errors = 1,
        Warnings = 2,
        Messages = 3,
        Verbose = 4
    }

    public enum PaperworkRequestCacheOption
    {
        None = 0,
        Static = 1
    }
}

