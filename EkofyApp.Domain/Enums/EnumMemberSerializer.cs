using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System.Reflection;
using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public sealed class EnumMemberSerializer<T> : SerializerBase<T> where T : struct, System.Enum
{
    public override T Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var value = context.Reader.ReadString();
        foreach (var field in typeof(T).GetFields())
        {
            var attribute = field.GetCustomAttribute<EnumMemberAttribute>();
            if (attribute != null && attribute.Value == value)
            {
                return (T)field.GetValue(null);
            }
            else if (field.Name == value)
            {
                return (T)field.GetValue(null);
            }
        }
        throw new BsonSerializationException($"Cannot convert '{value}' to {typeof(T)}");
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, T value)
    {
        var field = typeof(T).GetField(value.ToString());
        var attribute = field.GetCustomAttribute<EnumMemberAttribute>();
        if (attribute != null)
        {
            context.Writer.WriteString(attribute.Value);
        }
        else
        {
            context.Writer.WriteString(value.ToString());
        }
    }
}
