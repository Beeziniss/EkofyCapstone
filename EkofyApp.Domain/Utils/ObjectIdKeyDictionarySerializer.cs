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
            ObjectId objectId = ObjectId.Parse(kvp.Key); // Chuyển đổi string key thành ObjectId
            //context.Writer.WriteObjectId(objectId); // Ghi ObjectId làm field name
            context.Writer.WriteName(objectId.ToString()); // Ghi ObjectId làm field name
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
            //string objectIdKey = context.Reader.ReadObjectId().ToString(); // ObjectId được lưu dưới dạng string
            string objectIdKey = context.Reader.ReadName(); // ObjectId được lưu dưới dạng string
            bool value = context.Reader.ReadBoolean();
            dict[objectIdKey] = value;
        }
        context.Reader.ReadEndDocument();
        return dict;
    }
}
