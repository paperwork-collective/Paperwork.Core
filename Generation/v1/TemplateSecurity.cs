using System;
using System.Text.Json.Serialization;

namespace Paperwork.Generation.v1
{
    /// <summary>
    /// The optional security section of a template - what a reader may still do with the document,
    /// how it is encrypted, and whether a password is needed to open it.
    /// No section means no restrictions and no encryption.
    /// </summary>
    public class TemplateSecurity
    {
        public const string AutoOwnerPassword = "auto";
        public const string Encryption128Bit = "128bit";

        public const string AllowPrinting = "printing";
        public const string AllowCopying = "copying";
        public const string AllowAnnotations = "annotations";
        public const string AllowForms = "forms";
        public const string AllowModifications = "modifications";
        public const string AllowAccessibility = "accessibility";

        /// <summary>
        /// All the words that can be used in <see cref="Allow"/>
        /// </summary>
        public static readonly string[] AllowWords = new[]
        {
            AllowPrinting, AllowCopying, AllowAnnotations, AllowForms, AllowModifications, AllowAccessibility
        };

        /// <summary>
        /// Gets or sets what the reader may still do - any of printing, copying, annotations, forms, modifications, accessibility.
        /// Anything left out is restricted.
        /// </summary>
        [JsonPropertyName("allow")]
        public List<string> Allow { get; set; }

        /// <summary>
        /// Gets or sets the owner password. Only 'auto' is supported - a new random password for each document,
        /// that is never stored or shown, so the restrictions cannot be lifted.
        /// </summary>
        [JsonPropertyName("ownerPassword")]
        public string OwnerPassword { get; set; }

        /// <summary>
        /// Gets or sets the encryption. Only '128bit' is supported.
        /// </summary>
        [JsonPropertyName("encryption")]
        public string Encryption { get; set; }

        /// <summary>
        /// Gets or sets whether the person making the document must give a password that is needed to open it.
        /// </summary>
        [JsonPropertyName("requireUserPassword")]
        public bool RequireUserPassword { get; set; }

        public TemplateSecurity()
        {
            this.Allow = new List<string>();
            this.OwnerPassword = AutoOwnerPassword;
            this.Encryption = Encryption128Bit;
        }
    }
}
