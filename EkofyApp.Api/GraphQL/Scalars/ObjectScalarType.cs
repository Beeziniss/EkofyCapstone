using HotChocolate.Language;
using System.Collections;
using System.Globalization;

namespace EkofyApp.Api.GraphQL.Scalars;

public sealed class ObjectScalarType : ScalarType
{
    public ObjectScalarType() : base("Object")
    {
        Description = "Generic object scalar type.";
    }

    public override Type RuntimeType => typeof(object);

    // Cho phép nhiều literal (object/list/primitive/null)
    public override bool IsInstanceOfType(IValueNode literal) =>
        literal is ObjectValueNode or ListValueNode or NullValueNode
        or StringValueNode or BooleanValueNode or IntValueNode or FloatValueNode;

    // AST -> CLR
    public override object? ParseLiteral(IValueNode valueSyntax) =>
        FromValueNode(valueSyntax);

    // CLR -> AST
    public override IValueNode ParseValue(object? runtimeValue) =>
        ToValueNode(runtimeValue);

    public override IValueNode ParseResult(object? resultValue) =>
        ToValueNode(resultValue);

    public override object? Serialize(object? runtimeValue) => runtimeValue;
    public override object? Deserialize(object? resultValue) => resultValue;

    // -------- helpers --------
    private static IValueNode ToValueNode(object? value)
    {
        if (value is null) return NullValueNode.Default;
        if (value is IValueNode vn) return vn;
        if (value is string s) return new StringValueNode(s);
        if (value is bool b) return new BooleanValueNode(b);
        if (value is sbyte or byte or short or ushort or int or uint or long or ulong)
            return new IntValueNode((Int32)value);
        if (value is float or double or decimal)
            return new FloatValueNode((float)value);

        if (value is IDictionary dict)
        {
            var fields = new List<ObjectFieldNode>();
            foreach (DictionaryEntry e in dict)
                fields.Add(new ObjectFieldNode(e.Key?.ToString() ?? "", ToValueNode(e.Value)));
            return new ObjectValueNode(fields);
        }

        if (value is IEnumerable en && value is not string)
        {
            var items = new List<IValueNode>();
            foreach (var item in en) items.Add(ToValueNode(item));
            return new ListValueNode(items);
        }

        return new StringValueNode(value.ToString() ?? string.Empty);
    }

    private static object? FromValueNode(IValueNode node) => node switch
    {
        NullValueNode => null,
        StringValueNode s => s.Value,
        BooleanValueNode b => b.Value,
        IntValueNode i => long.TryParse(i.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l) ? l : i.Value,
        FloatValueNode f => double.TryParse(f.Value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var d) ? d : f.Value,
        ListValueNode l => FromList(l),
        ObjectValueNode o => FromObject(o),
        _ => null
    };

    private static object FromList(ListValueNode list)
    {
        var result = new List<object?>(list.Items.Count);
        foreach (var item in list.Items) result.Add(FromValueNode(item));
        return result;
    }

    private static object FromObject(ObjectValueNode obj)
    {
        var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var f in obj.Fields) dict[f.Name.Value] = FromValueNode(f.Value);
        return dict;
    }
    public override bool TrySerialize(object? runtimeValue, out object? resultValue)
    {
        resultValue = Serialize(runtimeValue);
        return true;
    }

    public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
    {
        runtimeValue = Deserialize(resultValue);
        return true;
    }
}
