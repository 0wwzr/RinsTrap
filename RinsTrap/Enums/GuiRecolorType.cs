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
        [EnumName(StaticName = "Ocean Blue")]
        OceanBlue = 2,

        [EnumSort(Order = 4)]
        [EnumName(StaticName = "Emerald Green")]
        EmeraldGreen = 3,

        [EnumSort(Order = 5)]
        [EnumName(StaticName = "Sunset Orange")]
        SunsetOrange = 4,

        [EnumSort(Order = 6)]
        [EnumName(StaticName = "Rose Pink")]
        RosePink = 5,

        [EnumSort(Order = 7)]
        [EnumName(StaticName = "Midnight")]
        Midnight = 6,

        [EnumSort(Order = 8)]
        [EnumName(StaticName = "Rainbow")]
        Rainbow = 7,

        [EnumSort(Order = 9)]
        [EnumName(StaticName = "Synthwave")]
        Synthwave = 8,

        [EnumSort(Order = 10)]
        [EnumName(StaticName = "Yellow")]
        Yellow = 9,

        [EnumSort(Order = 11)]
        [EnumName(StaticName = "Deep Blue")]
        DeepBlue = 10,

        [EnumSort(Order = 12)]
        [EnumName(StaticName = "Red")]
        Red = 11,

        [EnumSort(Order = 13)]
        [EnumName(StaticName = "Custom")]
        Custom = 12
    }
}
