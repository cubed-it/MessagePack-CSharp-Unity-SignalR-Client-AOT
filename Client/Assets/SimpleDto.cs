using MessagePack;

namespace Assets
{
    [MessagePackObject]
    public class SimpleDto
    {
        [Key(0)] public string Value { get; set; }
    }
}
