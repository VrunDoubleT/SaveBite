namespace SaveBite.Backend.Models.DTOs;

public sealed class AttributeValueContent
{
    public Guid AttributeId { get; set; }
    public string AttributeName { get; set; }
    public Guid ValueId { get; set; }
    public string Value { get; set; }
    public string? ImageUrl { get; set; }

    public AttributeValueContent(Guid attributeId, string attributeName, Guid valueId, string value, string? imageUrl)
    {
        AttributeId = attributeId;
        AttributeName = attributeName;
        ValueId = valueId;
        Value = value;
        ImageUrl = imageUrl;
    }
}
