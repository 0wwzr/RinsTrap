using RinsTrap.Models.Attributes;

namespace RinsTrap.Enums
{
    public enum GuiRecolorType
    {
        [EnumSort(Order = 1)]
        [EnumName(StaticName = "None")]
        None = 0,

        [EnumSort(Order = 2)]
        [EnumName(StaticName = "Purple")]
        Purple = 1,

        [EnumSort(Order = 3)]
        [EnumName(StaticName = "Rainbow")]
        Rainbow = 2,

        [EnumSort(Order = 4)]
        [EnumName(StaticName = "Synthwave")]
        Synthwave = 3,

        [EnumSort(Order = 5)]
        [EnumName(StaticName = "Yellow")]
        Yellow = 4,

        [EnumSort(Order = 6)]
        [EnumName(StaticName = "Deep Blue")]
        DeepBlue = 5,

        [EnumSort(Order = 7)]
        [EnumName(StaticName = "Red")]
        Red = 6,

        [EnumSort(Order = 8)]
        [EnumName(StaticName = "Custom")]
        Custom = 7
    }
}
