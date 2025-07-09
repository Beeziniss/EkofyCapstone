using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace EkofyApp.Domain.Utils;
/// <summary>
/// Class này tự serialize và deserialize một Dictionary với ObjectId làm khóa.
/// Nhưng ObjectId sẽ được lưu trữ dưới dạng chuỗi trong MongoDB.
/// Điều này là cần thiết vì MongoDB không hỗ trợ trực tiếp ObjectId làm khóa trong Dictionary.
/// Và class này chỉ để chơi hehe
/// </summary>
public sealed class ObjectIdKeyDictionarySerializer : SerializerBase<Dictionary<string, bool>>
{
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Dictionary<string, bool> value)
    {
        context.Writer.WriteStartDocument();
        foreach (KeyValuePair<string, bool> kvp in value)
        {
            ObjectId objectId = ObjectId.Parse(kvp.Key); // Convert string key to ObjectId
            //context.Writer.WriteObjectId(objectId); // Write ObjectId as field name
            context.Writer.WriteName(objectId.ToString()); // Write ObjectId as field name
            context.Writer.WriteBoolean(kvp.Value);
        }
        context.Writer.WriteEndDocument();
    }

    public override Dictionary<string, bool> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        Dictionary<string, bool> dict = [];
        context.Reader.ReadStartDocument();
        while (context.Reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            //string objectIdKey = context.Reader.ReadObjectId().ToString(); // ObjectId stored as string
            string objectIdKey = context.Reader.ReadName(); // ObjectId stored as string
            bool value = context.Reader.ReadBoolean();
            dict[objectIdKey] = value;
        }
        context.Reader.ReadEndDocument();
        return dict;
    }
}
