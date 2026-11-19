using System;
using System.Text.Json.Serialization;

namespace Paperwork.Generation.v1
{
    /// <summary>
    /// The optional form-data section of a template: whether the answers the document was
    /// made from are carried inside the PDF itself, as an embedded file.
    /// No section, or <see cref="Include"/> false, means nothing is attached.
    /// </summary>
    /// <remarks>
    /// The file is registered in the document catalog's /EmbeddedFiles name tree, so nothing
    /// is drawn on any page - but it is listed in a reader's attachments panel, and there is
    /// no flag in the PDF format that would hide it. That is deliberate: the answers travel
    /// with the document wherever it goes, and anyone holding it can see what is there,
    /// open it, and take it out.
    /// </remarks>
    public class TemplateFormData
    {
        /// <summary>
        /// The shape of the attached JSON. Anything reading it back should check this first.
        /// </summary>
        public const string PayloadVersion = "1.0";

        /// <summary>
        /// The name every attached answers file is given. Fixed on purpose: whatever reads a
        /// document back cannot ask the template what its attachment is called, because the
        /// template may have been republished since - which is exactly when someone needs the
        /// answers back. A reader should still find the file by its contents (the "paperwork"
        /// key) rather than by this name, so a renamed copy still works.
        /// </summary>
        public const string DefaultFileName = "paperwork-user-answers.json";

        public const string DefaultDescription = "The answers this document was made from, so it can be filled in again.";

        /// <summary>
        /// Only the fields someone making the document can change - the default, and the only
        /// safe one for a published template, since everything else was set by its designer.
        /// </summary>
        public const string FieldsFilledIn = "filled";

        /// <summary>
        /// Every field on the document, the designer's own included. Only for a document built
        /// in code, where there is no form and every value is the caller's own.
        /// </summary>
        public const string FieldsAll = "all";

        /// <summary>
        /// Gets or sets whether the answers are attached to the document. False by default -
        /// a template has to ask for it.
        /// </summary>
        [JsonPropertyName("include")]
        public bool Include { get; set; }

        /// <summary>
        /// Gets or sets which field values are attached - <see cref="FieldsFilledIn"/> (the default)
        /// or <see cref="FieldsAll"/>.
        /// </summary>
        /// <remarks>
        /// A published template's fields are not all the person's to fill in: anything the designer
        /// set and nobody else can change - an internal rate, a reference, a threshold - is on the
        /// document's $fields just the same. Attaching those would put them in a file that travels
        /// with every document made, so the default is the fields someone filling the form can
        /// actually change. It is also the more useful answer: the designer's own values come back
        /// from the template when it is opened again, and a newer version of it should win.
        /// </remarks>
        [JsonPropertyName("fields")]
        public string? Fields { get; set; }

        /// <summary>
        /// True when every field is attached, the designer's own included.
        /// Anything other than <see cref="FieldsAll"/>, including nothing at all, is the safe default.
        /// </summary>
        public bool IncludesEveryField()
        {
            return string.Equals(this.Fields, FieldsAll, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets or sets the name the attached file is given. Defaults to <see cref="DefaultFileName"/>,
        /// which is what the Designer always uses - this is here for a document built in code.
        /// Any path is dropped - an attachment is a single name, not a location.
        /// </summary>
        [JsonPropertyName("fileName")]
        public string? FileName { get; set; }

        /// <summary>
        /// Gets or sets the description shown beside the file in a reader's attachments panel.
        /// Defaults to <see cref="DefaultDescription"/>.
        /// </summary>
        [JsonPropertyName("desc")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets what identifies the template the document was made from, recorded in the
        /// attached file so it can be found again. Stamped in when the template is published -
        /// Paperwork.Core never invents one.
        /// </summary>
        [JsonPropertyName("source")]
        public string? Source { get; set; }

        /// <summary>
        /// Gets or sets the version of the template, recorded alongside <see cref="Source"/>, so
        /// whatever reads the answers back knows whether the template has moved on since.
        /// </summary>
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets where the document was made - the address of the page that generated it,
        /// recorded so a reader can reopen exactly that template. <see cref="Source"/> alone is
        /// not enough: the same id is reached at a different address depending on how it was
        /// published. Set by whatever made the document, not stamped in at publish.
        /// </summary>
        [JsonPropertyName("path")]
        public string? Path { get; set; }

        /// <summary>
        /// The file name to use, with any path dropped and the default applied.
        /// </summary>
        public string ResolvedFileName()
        {
            var name = this.FileName;
            if (string.IsNullOrWhiteSpace(name))
                return DefaultFileName;

            name = name.Trim().Replace('\\', '/');
            var slash = name.LastIndexOf('/');
            if (slash >= 0)
                name = name.Substring(slash + 1);

            return string.IsNullOrWhiteSpace(name) ? DefaultFileName : name;
        }

        public string ResolvedDescription()
        {
            return string.IsNullOrWhiteSpace(this.Description) ? DefaultDescription : this.Description!;
        }
    }
}
