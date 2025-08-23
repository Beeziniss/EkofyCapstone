using HotChocolate.Language;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace EkofyApp.Api.GraphQL.Scalars;

public sealed class EntitlementValueScalar : ScalarType<object>
{
    public EntitlementValueScalar() : base("EntitlementValue")
    {
        Description = "Polymorphic scalar for String, Int, Long, Double, Decimal, Boolean, DateTime, Object, Array.";
    }

    // GraphQL AST -> .NET
    public override object? ParseLiteral(IValueNode valueSyntax)
    {
        switch (valueSyntax)
        {
            case NullValueNode: return null;
            case StringValueNode sv: return sv.Value;
            case BooleanValueNode bv: return bv.Value;

            case IntValueNode iv:
                {
                    if (long.TryParse(iv.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
                        return (l <= int.MaxValue && l >= int.MinValue) ? (object)(int)l : l;
                }
                throw new SerializationException("Invalid integer value.", this);

            case FloatValueNode fv:
                {
                    if (decimal.TryParse(fv.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var dec))
                        return dec;
                }
                {
                    if (double.TryParse(fv.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var dbl))
                        return dbl;
                }
                throw new SerializationException("Invalid floating-point value.", this);

            case ListValueNode list:
                {
                    var arr = new List<object?>();
                    foreach (var item in list.Items)
                    {
                        arr.Add(ParseLiteral(item));
                    }
                    return arr;
                }

            case ObjectValueNode ov:
                {
                    var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
                    foreach (var f in ov.Fields) dict[f.Name.Value] = ParseLiteral(f.Value);
                    return dict;
                }
        }

        throw new SerializationException($"Cannot parse literal {valueSyntax.GetType().Name}.", this);
    }

    // .NET -> GraphQL AST
    public override IValueNode ParseValue(object? runtimeValue)
    {
        if (runtimeValue is null) return NullValueNode.Default;

        switch (runtimeValue)
        {
            case string s: return new StringValueNode(s);
            case bool b: return new BooleanValueNode(b);
            case int i: return new IntValueNode(i);
            case long l: return new IntValueNode(l);
            case double d: return new FloatValueNode(d);
            case decimal m: return new FloatValueNode(m);
            case float f: return new FloatValueNode(f);
            case JsonElement je: return ParseValue(JsonToObject(je));

            case IEnumerable en when runtimeValue is not string:
                {
                    List<IValueNode> items = [];
                    foreach (object? it in en)
                    {
                        items.Add(ParseValue(it));
                    }

                    return new ListValueNode(items);
                }

            case IDictionary<string, object?> dict:
                {
                    List<ObjectFieldNode> fields = [];
                    foreach (KeyValuePair<string, object?> kv in dict)
                    {
                        fields.Add(new ObjectFieldNode(kv.Key, ParseValue(kv.Value)));
                    }

                    return new ObjectValueNode(fields);
                }

            default:
                {
                    // POCO -> object value
                    PropertyInfo[] props = runtimeValue.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
                    List<ObjectFieldNode> fields = [];
                    foreach (PropertyInfo p in props)
                    {
                        fields.Add(new ObjectFieldNode(p.Name, ParseValue(p.GetValue(runtimeValue))));
                    }

                    return new ObjectValueNode(fields);
                }
        }
    }

    public override IValueNode ParseResult(object? resultValue) => ParseValue(resultValue);

    public override bool TrySerialize(object? runtimeValue, out object? resultValue)
    {
        try
        {
            resultValue = MakeJsonFriendly(runtimeValue); return true;
        }
        catch
        {
            resultValue = null; return false;
        }
    }

    public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
    {
        runtimeValue = resultValue is JsonElement je ? JsonToObject(je) : resultValue;
        return true;
    }

    public override bool IsInstanceOfType(IValueNode literal) =>
        literal is NullValueNode or StringValueNode or BooleanValueNode
        or IntValueNode or FloatValueNode
        or ListValueNode or ObjectValueNode;

    public override bool IsInstanceOfType(object? value) =>
        value is null or string or bool or int or long or double or decimal or float
        or IEnumerable or IDictionary<string, object?> or JsonElement;

    private static object? MakeJsonFriendly(object? v)
    {
        if (v is null)
        {
            return null;
        }

        if (v is string or bool or int or long or double or decimal or float)
        {
            return v;
        }

        if (v is IEnumerable en && v is not string)
        {
            List<object?> list = [];
            foreach (object? item in en)
            {
                list.Add(MakeJsonFriendly(item));
            }

            return list;
        }

        if (v is IDictionary<string, object?> dict)
        {
            Dictionary<string, object?> map = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object?> kv in dict)
            {
                map[kv.Key] = MakeJsonFriendly(kv.Value);
            }

            return map;
        }

        PropertyInfo[] props = v.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        Dictionary<string, object?> d = new(StringComparer.Ordinal);
        foreach (PropertyInfo p in props)
        {
            d[p.Name] = MakeJsonFriendly(p.GetValue(v));
        }

        return d;
    }

    private static object? JsonToObject(JsonElement je)
    {
        return je.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => je.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => je.TryGetInt64(out var l) ? l :
                                     je.TryGetDecimal(out var dec) ? dec :
                                     je.TryGetDouble(out var d) ? d : je.ToString(),
            JsonValueKind.Array => ToList(je),
            JsonValueKind.Object => ToDict(je),
            _ => null
        };

        static List<object?> ToList(JsonElement a)
        {
            List<object?> list = [];
            foreach (JsonElement el in a.EnumerateArray())
            {
                list.Add(JsonToObject(el));
            }

            return list;
        }

        static Dictionary<string, object?> ToDict(JsonElement o)
        {
            Dictionary<string, object?> d = new(StringComparer.Ordinal);
            foreach (JsonProperty prop in o.EnumerateObject())
            {
                d[prop.Name] = JsonToObject(prop.Value);
            }

            return d;
        }
    }
}
