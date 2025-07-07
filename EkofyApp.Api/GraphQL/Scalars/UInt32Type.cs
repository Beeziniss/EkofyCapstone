using HotChocolate.Language;

namespace EkofyApp.Api.GraphQL.Scalars;

public class UInt32Type : ScalarType<uint, IntValueNode>
{
    public UInt32Type() : base("UInt32") { }

    protected override uint ParseLiteral(IntValueNode valueSyntax)
    {
        int val = valueSyntax.ToInt32();
        if (val < 0)
        {
            throw new SerializationException("UInt32 cannot be negative.", this);
        }
        return (uint)val;
    }

    protected override IntValueNode ParseValue(uint runtimeValue)
    {
        return new IntValueNode(runtimeValue);
    }

    public override IValueNode ParseResult(object? resultValue)
    {
        if (resultValue is uint u)
        {
            return new IntValueNode(u);
        }
        if (resultValue is int i && i >= 0)
        {
            return new IntValueNode(i);
        }

        throw new SerializationException("Cannot parse result as UInt32", this);
    }

    public override object Deserialize(object? resultValue)
    {
        if (resultValue is uint u)
            return u;
        if (resultValue is int i && i >= 0)
            return (uint)i;
        if (resultValue is string s && uint.TryParse(s, out var parsed))
            return parsed;

        throw new SerializationException("Invalid UInt32 value", this);
    }
}
