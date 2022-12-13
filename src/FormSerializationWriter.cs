using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions.Serialization;

namespace Microsoft.Kiota.Serialization.Form;
/// <summary>Represents a serialization writer that can be used to write a form url encoded string.</summary>
public class FormSerializationWriter : ISerializationWriter
{
    private int depth;
    private readonly StringBuilder _builder = new();
    /// <inheritdoc/>
    public Action<IParsable>? OnBeforeObjectSerialization { get; set; }
    /// <inheritdoc/>
    public Action<IParsable>? OnAfterObjectSerialization { get; set; }
    /// <inheritdoc/>
    public Action<IParsable, ISerializationWriter>? OnStartObjectSerialization { get; set; }
    /// <inheritdoc/>
    public void Dispose() {
        GC.SuppressFinalize(this);
    }
    /// <inheritdoc/>
    public Stream GetSerializedContent() => new MemoryStream(Encoding.UTF8.GetBytes(_builder.ToString()));
    /// <inheritdoc/>
    public void WriteAdditionalData(IDictionary<string, object> value) {
        if(value == null) return;
        foreach(var kvp in value.Select(static x => (key: x.Key, value: GetNormalizedStringRepresentation(x.Value))))
            WriteStringValue(kvp.key, kvp.value);
    }
    private static string GetNormalizedStringRepresentation(object value) {
        return value switch {
            null => "null",
            bool b => b.ToString().ToLowerInvariant(),
            DateTimeOffset dto => dto.ToString("o"),
            IParsable => throw new InvalidOperationException("Form serialization does not support nested objects."),
            _ => value.ToString(),            
        };
    }
    /// <inheritdoc/>
    public void WriteBoolValue(string key, bool? value) {
        if(value.HasValue) 
            WriteStringValue(key, value.Value.ToString().ToLowerInvariant());
    }
    /// <inheritdoc/>
    public void WriteByteArrayValue(string key, byte[] value) {
        if(value != null)//empty array is meaningful
            WriteStringValue(key, value.Any() ? Convert.ToBase64String(value) : string.Empty);
    }
    /// <inheritdoc/>
    public void WriteByteValue(string key, byte? value) {
        if(value.HasValue) 
            WriteIntValue(key, Convert.ToInt32(value.Value));
    }
    /// <inheritdoc/>
    public void WriteCollectionOfObjectValues<T>(string key, IEnumerable<T> values) where T : IParsable => throw new InvalidOperationException("Form serialization does not support collections.");
    /// <inheritdoc/>
    public void WriteCollectionOfPrimitiveValues<T>(string key, IEnumerable<T> values) => throw new InvalidOperationException("Form serialization does not support collections.");
    /// <inheritdoc/>
    public void WriteDateTimeOffsetValue(string key, DateTimeOffset? value) {
        if(value.HasValue) 
            WriteStringValue(key, value.Value.ToString("o"));
    }
    /// <inheritdoc/>
    public void WriteDateValue(string key, Date? value) {
        if(value.HasValue) 
            WriteStringValue(key, value.Value.ToString());
    }
    /// <inheritdoc/>
    public void WriteDecimalValue(string key, decimal? value) {
        if(value.HasValue) 
            WriteStringValue(key, value.Value.ToString(CultureInfo.InvariantCulture));
    }
    /// <inheritdoc/>
    public void WriteDoubleValue(string key, double? value) {
        if(value.HasValue) 
            WriteStringValue(key, value.Value.ToString(CultureInfo.InvariantCulture));
    }
    /// <inheritdoc/>
    public void WriteFloatValue(string key, float? value) {
        if(value.HasValue) 
            WriteStringValue(key, value.Value.ToString(CultureInfo.InvariantCulture));
    }
    /// <inheritdoc/>
    public void WriteGuidValue(string key, Guid? value) {
        if(value.HasValue) 
            WriteStringValue(key, value.Value.ToString("D"));
    }
    /// <inheritdoc/>
    public void WriteIntValue(string key, int? value) {
        if(value.HasValue) 
            WriteStringValue(key, value.Value.ToString(CultureInfo.InvariantCulture));
    }
    /// <inheritdoc/>
    public void WriteLongValue(string key, long? value) {
        if(value.HasValue) 
            WriteStringValue(key, value.Value.ToString(CultureInfo.InvariantCulture));
    }
    /// <inheritdoc/>
    public void WriteNullValue(string key) {
        WriteStringValue(key, "null");
    }
    /// <inheritdoc/>
    public void WriteObjectValue<T>(string key, T value, params IParsable[] additionalValuesToMerge) where T : IParsable
    {
        if(depth > 0) throw new InvalidOperationException("Form serialization does not support nested objects.");
        depth++;
        if(value == null && !additionalValuesToMerge.Any(static x => x != null)) return;
        if(value != null) {
            OnBeforeObjectSerialization?.Invoke(value);
            OnStartObjectSerialization?.Invoke(value, this);
            value.Serialize(this);
        }
        foreach(var additionalValueToMerge in additionalValuesToMerge)
        {
            OnBeforeObjectSerialization?.Invoke(additionalValueToMerge);
            OnStartObjectSerialization?.Invoke(additionalValueToMerge, this);
            additionalValueToMerge.Serialize(this);
            OnAfterObjectSerialization?.Invoke(additionalValueToMerge);
        }
        if(value != null)
            OnAfterObjectSerialization?.Invoke(value);
    }
    /// <inheritdoc/>
    public void WriteSbyteValue(string key, sbyte? value) {
        if(value.HasValue) 
            WriteIntValue(key, Convert.ToInt32(value.Value));
    }
    /// <inheritdoc/>
    public void WriteStringValue(string key, string value) {
        if(value == null) return;
        if(_builder.Length > 0) _builder.Append('&');
        _builder.Append(key).Append('=').Append(Uri.EscapeDataString(value));
    }
    /// <inheritdoc/>
    public void WriteTimeSpanValue(string key, TimeSpan? value) {
        if(value.HasValue) 
            WriteStringValue(key, XmlConvert.ToString(value.Value));
    }
    /// <inheritdoc/>
    public void WriteTimeValue(string key, Time? value) {
        if(value.HasValue) 
            WriteStringValue(key, value.Value.ToString());
    }
    void ISerializationWriter.WriteCollectionOfEnumValues<T>(string key, IEnumerable<T?> values) {
        if(values == null || !values.Any()) return;
        WriteStringValue(key, string.Join(",", values.Where(static x => x.HasValue)
            .Select(static x => x!.Value.ToString().ToFirstCharacterLowerCase())));
    }
    void ISerializationWriter.WriteEnumValue<T>(string key, T? value) {
        if(value.HasValue)
        {
            if(typeof(T).GetCustomAttributes<FlagsAttribute>().Any())
                WriteStringValue(key, string.Join(",", Enum.GetValues(typeof(T))
                                        .Cast<T>()
                                        .Where(x => value.Value.HasFlag(x))
                                        .Select(static x => Enum.GetName(typeof(T),x))
                                        .Select(static x => x.ToFirstCharacterLowerCase())));
            else WriteStringValue(key, value.Value.ToString().ToFirstCharacterLowerCase());
        }
    }
}
