using System.Collections.Generic;
using System.Printing;

namespace DGView.Controls.Printing
{
    public partial class PageViewModel
    {
        public class PageSize
        {
            public static string DimensionSuffix => CurrentMeasurementSystem == MeasurementSystem.US ? "″" : " cm";
            public static string DimensionSuffixName => CurrentMeasurementSystem == MeasurementSystem.US ? "inches" : "centimeters";

            public static PageSize GetPageSize(PageMediaSize mediaSize)
            {
                if (!mediaSize.PageMediaSizeName.HasValue) return null;

                if (!_validPageSizes.ContainsKey(mediaSize.PageMediaSizeName.Value))
                    _validPageSizes.Add(mediaSize.PageMediaSizeName.Value, new PageSize(mediaSize));
                
                var a1 = _validPageSizes[mediaSize.PageMediaSizeName.Value];
                return a1.IsValid ? a1 : null;
            }

            private static Dictionary<PageMediaSizeName, PageSize> _validPageSizes = new Dictionary<PageMediaSizeName, PageSize>();
            private static Dictionary<PageMediaSizeName, string> _names = new Dictionary<PageMediaSizeName, string>
            {
                {PageMediaSizeName.Unknown, "Unknown paper size"}, {PageMediaSizeName.ISOA0, "A0"},
                {PageMediaSizeName.ISOA1, "A1"}, {PageMediaSizeName.ISOA10, "A10"}, {PageMediaSizeName.ISOA2, "A2"},
                {PageMediaSizeName.ISOA3, "A3"}, {PageMediaSizeName.ISOA3Rotated, "A3 Rotated"},
                {PageMediaSizeName.ISOA3Extra, "A3 Extra"}, {PageMediaSizeName.ISOA4, "A4"},
                {PageMediaSizeName.ISOA4Rotated, "A4 Rotated"}, {PageMediaSizeName.ISOA4Extra, "A4 Extra"},
                {PageMediaSizeName.ISOA5, "A5"}, {PageMediaSizeName.ISOA5Rotated, "A5 Rotated"},
                {PageMediaSizeName.ISOA5Extra, "A5 Extra"}, {PageMediaSizeName.ISOA6, "A6"},
                {PageMediaSizeName.ISOA6Rotated, "A6 Rotated"}, {PageMediaSizeName.ISOA7, "A7"},
                {PageMediaSizeName.ISOA8, "A8"}, {PageMediaSizeName.ISOA9, "A9"}, {PageMediaSizeName.ISOB0, "B0"},
                {PageMediaSizeName.ISOB1, "B1"}, {PageMediaSizeName.ISOB10, "B10"}, {PageMediaSizeName.ISOB2, "B2"},
                {PageMediaSizeName.ISOB3, "B3"}, {PageMediaSizeName.ISOB4, "B4"},
                {PageMediaSizeName.ISOB4Envelope, "B4 Envelope"}, {PageMediaSizeName.ISOB5Envelope, "B5 Envelope"},
                {PageMediaSizeName.ISOB5Extra, "B5 Extra"}, {PageMediaSizeName.ISOB7, "B7"},
                {PageMediaSizeName.ISOB8, "B8"}, {PageMediaSizeName.ISOB9, "B9"}, {PageMediaSizeName.ISOC0, "C0"},
                {PageMediaSizeName.ISOC1, "C1"}, {PageMediaSizeName.ISOC10, "C10"}, {PageMediaSizeName.ISOC2, "C2"},
                {PageMediaSizeName.ISOC3, "C3"}, {PageMediaSizeName.ISOC3Envelope, "C3 Envelope"},
                {PageMediaSizeName.ISOC4, "C4"}, {PageMediaSizeName.ISOC4Envelope, "C4 Envelope"},
                {PageMediaSizeName.ISOC5, "C5"}, {PageMediaSizeName.ISOC5Envelope, "C5 Envelope"},
                {PageMediaSizeName.ISOC6, "C6"}, {PageMediaSizeName.ISOC6Envelope, "C6 Envelope"},
                {PageMediaSizeName.ISOC6C5Envelope, "C6C5 Envelope"}, {PageMediaSizeName.ISOC7, "C7"},
                {PageMediaSizeName.ISOC8, "C8"}, {PageMediaSizeName.ISOC9, "C9"},
                {PageMediaSizeName.ISODLEnvelope, "DL Envelope"},
                {PageMediaSizeName.ISODLEnvelopeRotated, "DL Envelope Rotated"}, {PageMediaSizeName.ISOSRA3, "SRA 3"},
                {PageMediaSizeName.JapanQuadrupleHagakiPostcard, "Quadruple Hagaki Postcard"},
                {PageMediaSizeName.JISB0, "Japanese Industrial Standard B0"},
                {PageMediaSizeName.JISB1, "Japanese Industrial Standard B1"},
                {PageMediaSizeName.JISB10, "Japanese Industrial Standard B10"},
                {PageMediaSizeName.JISB2, "Japanese Industrial Standard B2"},
                {PageMediaSizeName.JISB3, "Japanese Industrial Standard B3"},
                {PageMediaSizeName.JISB4, "Japanese Industrial Standard B4"},
                {PageMediaSizeName.JISB4Rotated, "Japanese Industrial Standard B4 Rotated"},
                {PageMediaSizeName.JISB5, "Japanese Industrial Standard B5"},
                {PageMediaSizeName.JISB5Rotated, "Japanese Industrial Standard B5 Rotated"},
                {PageMediaSizeName.JISB6, "Japanese Industrial Standard B6"},
                {PageMediaSizeName.JISB6Rotated, "Japanese Industrial Standard B6 Rotated"},
                {PageMediaSizeName.JISB7, "Japanese Industrial Standard B7"},
                {PageMediaSizeName.JISB8, "Japanese Industrial Standard B8"},
                {PageMediaSizeName.JISB9, "Japanese Industrial Standard B9"},
                {PageMediaSizeName.JapanChou3Envelope, "Chou 3 Envelope"},
                {PageMediaSizeName.JapanChou3EnvelopeRotated, "Chou 3 Envelope Rotated"},
                {PageMediaSizeName.JapanChou4Envelope, "Chou 4 Envelope"},
                {PageMediaSizeName.JapanChou4EnvelopeRotated, "Chou 4 Envelope Rotated"},
                {PageMediaSizeName.JapanHagakiPostcard, "Hagaki Postcard"},
                {PageMediaSizeName.JapanHagakiPostcardRotated, "Hagaki Postcard Rotated"},
                {PageMediaSizeName.JapanKaku2Envelope, "Kaku 2 Envelope"},
                {PageMediaSizeName.JapanKaku2EnvelopeRotated, "Kaku 2 Envelope Rotated"},
                {PageMediaSizeName.JapanKaku3Envelope, "Kaku 3 Envelope"},
                {PageMediaSizeName.JapanKaku3EnvelopeRotated, "Kaku 3 Envelope Rotated"},
                {PageMediaSizeName.JapanYou4Envelope, "You 4 Envelope"},
                {PageMediaSizeName.NorthAmerica10x11, "10 x 11"},
                {PageMediaSizeName.NorthAmerica10x14, "10 x 14"}, {PageMediaSizeName.NorthAmerica11x17, "11 x 17"},
                {PageMediaSizeName.NorthAmerica9x11, "9 x 11"},
                {PageMediaSizeName.NorthAmericaArchitectureASheet, "Architecture A Sheet"},
                {PageMediaSizeName.NorthAmericaArchitectureBSheet, "Architecture B Sheet"},
                {PageMediaSizeName.NorthAmericaArchitectureCSheet, "Architecture C Sheet"},
                {PageMediaSizeName.NorthAmericaArchitectureDSheet, "Architecture D Sheet"},
                {PageMediaSizeName.NorthAmericaArchitectureESheet, "Architecture E Sheet"},
                {PageMediaSizeName.NorthAmericaCSheet, "C Sheet"}, {PageMediaSizeName.NorthAmericaDSheet, "D Sheet"},
                {PageMediaSizeName.NorthAmericaESheet, "E Sheet"},
                {PageMediaSizeName.NorthAmericaExecutive, "Executive"},
                {PageMediaSizeName.NorthAmericaGermanLegalFanfold, "German Legal Fanfold"},
                {PageMediaSizeName.NorthAmericaGermanStandardFanfold, "German Standard Fanfold"},
                {PageMediaSizeName.NorthAmericaLegal, "Legal"},
                {PageMediaSizeName.NorthAmericaLegalExtra, "Legal Extra"},
                {PageMediaSizeName.NorthAmericaLetter, "Letter "},
                {PageMediaSizeName.NorthAmericaLetterRotated, "Letter Rotated"},
                {PageMediaSizeName.NorthAmericaLetterExtra, "Letter Extra"},
                {PageMediaSizeName.NorthAmericaLetterPlus, "Letter Plus"},
                {PageMediaSizeName.NorthAmericaMonarchEnvelope, "Monarch Envelope"},
                {PageMediaSizeName.NorthAmericaNote, "Note"},
                {PageMediaSizeName.NorthAmericaNumber10Envelope, "#10 Envelope"},
                {PageMediaSizeName.NorthAmericaNumber10EnvelopeRotated, "#10 Envelope Rotated"},
                {PageMediaSizeName.NorthAmericaNumber9Envelope, "#9 Envelope"},
                {PageMediaSizeName.NorthAmericaNumber11Envelope, "#11 Envelope"},
                {PageMediaSizeName.NorthAmericaNumber12Envelope, "#12 Envelope"},
                {PageMediaSizeName.NorthAmericaNumber14Envelope, "#14 Envelope"},
                {PageMediaSizeName.NorthAmericaPersonalEnvelope, "Personal Envelope"},
                {PageMediaSizeName.NorthAmericaQuarto, "Quarto"},
                {PageMediaSizeName.NorthAmericaStatement, "Statement"},
                {PageMediaSizeName.NorthAmericaSuperA, "Super A"}, {PageMediaSizeName.NorthAmericaSuperB, "Super B"},
                {PageMediaSizeName.NorthAmericaTabloid, "Tabloid"},
                {PageMediaSizeName.NorthAmericaTabloidExtra, "Tabloid Extra"},
                {PageMediaSizeName.OtherMetricA4Plus, "A4 Plus"}, {PageMediaSizeName.OtherMetricA3Plus, "A3 Plus"},
                {PageMediaSizeName.OtherMetricFolio, "Folio"},
                {PageMediaSizeName.OtherMetricInviteEnvelope, "Invite Envelope"},
                {PageMediaSizeName.OtherMetricItalianEnvelope, "Italian Envelope"},
                {PageMediaSizeName.PRC1Envelope, "People's Republic of China #1 Envelope"},
                {PageMediaSizeName.PRC1EnvelopeRotated, "People's Republic of China #1 Envelope Rotated"},
                {PageMediaSizeName.PRC10Envelope, "People's Republic of China #10 Envelope"},
                {PageMediaSizeName.PRC10EnvelopeRotated, "People's Republic of China #10 Envelope Rotated"},
                {PageMediaSizeName.PRC16K, "People's Republic of China 16K"},
                {PageMediaSizeName.PRC16KRotated, "People's Republic of China 16K Rotated"},
                {PageMediaSizeName.PRC2Envelope, "People's Republic of China #2 Envelope"},
                {PageMediaSizeName.PRC2EnvelopeRotated, "People's Republic of China #2 Envelope Rotated"},
                {PageMediaSizeName.PRC32K, "People's Republic of China 32K"},
                {PageMediaSizeName.PRC32KRotated, "People's Republic of China 32K Rotated"},
                {PageMediaSizeName.PRC32KBig, "People's Republic of China 32K Big"},
                {PageMediaSizeName.PRC3Envelope, "People's Republic of China #3 Envelope"},
                {PageMediaSizeName.PRC3EnvelopeRotated, "People's Republic of China #3 Envelope Rotated"},
                {PageMediaSizeName.PRC4Envelope, "People's Republic of China #4 Envelope"},
                {PageMediaSizeName.PRC4EnvelopeRotated, "People's Republic of China #4 Envelope Rotated"},
                {PageMediaSizeName.PRC5Envelope, "People's Republic of China #5 Envelope"},
                {PageMediaSizeName.PRC5EnvelopeRotated, "People's Republic of China #5 Envelope Rotated"},
                {PageMediaSizeName.PRC6Envelope, "People's Republic of China #6 Envelope"},
                {PageMediaSizeName.PRC6EnvelopeRotated, "People's Republic of China #6 Envelope Rotated"},
                {PageMediaSizeName.PRC7Envelope, "People's Republic of China #7 Envelope"},
                {PageMediaSizeName.PRC7EnvelopeRotated, "People's Republic of China #7 Envelope Rotated"},
                {PageMediaSizeName.PRC8Envelope, "People's Republic of China #8 Envelope"},
                {PageMediaSizeName.PRC8EnvelopeRotated, "People's Republic of China #8 Envelope Rotated"},
                {PageMediaSizeName.PRC9Envelope, "People's Republic of China #9 Envelope"},
                {PageMediaSizeName.PRC9EnvelopeRotated, "People's Republic of China #9 Envelope Rotated"},
                {PageMediaSizeName.Roll04Inch, "4-inch wide roll"}, {PageMediaSizeName.Roll06Inch, "6-inch wide roll"},
                {PageMediaSizeName.Roll08Inch, "8-inch wide roll"}, {PageMediaSizeName.Roll12Inch, "12-inch wide roll"},
                {PageMediaSizeName.Roll15Inch, "15-inch wide roll"},
                {PageMediaSizeName.Roll18Inch, "18-inch wide roll"},
                {PageMediaSizeName.Roll22Inch, "22-inch wide roll"},
                {PageMediaSizeName.Roll24Inch, "24-inch wide roll"},
                {PageMediaSizeName.Roll30Inch, "30-inch wide roll"},
                {PageMediaSizeName.Roll36Inch, "36-inch wide roll"},
                {PageMediaSizeName.Roll54Inch, "54-inch wide roll"},
                {PageMediaSizeName.JapanDoubleHagakiPostcard, "Double Hagaki Postcard"},
                {PageMediaSizeName.JapanDoubleHagakiPostcardRotated, "Double Hagaki Postcard Rotated"},
                {PageMediaSizeName.JapanLPhoto, "L Photo"}, {PageMediaSizeName.Japan2LPhoto, "2L Photo"},
                {PageMediaSizeName.JapanYou1Envelope, "You 1 Envelope"},
                {PageMediaSizeName.JapanYou2Envelope, "You 2 Envelope"},
                {PageMediaSizeName.JapanYou3Envelope, "You 3 Envelope"},
                {PageMediaSizeName.JapanYou4EnvelopeRotated, "You 4 Envelope Rotated"},
                {PageMediaSizeName.JapanYou6Envelope, "You 6 Envelope"},
                {PageMediaSizeName.JapanYou6EnvelopeRotated, "You 6 Envelope Rotated"},
                {PageMediaSizeName.NorthAmerica4x6, "4 x 6"}, {PageMediaSizeName.NorthAmerica4x8, "4 x 8"},
                {PageMediaSizeName.NorthAmerica5x7, "5 x 7"}, {PageMediaSizeName.NorthAmerica8x10, "8 x 10"},
                {PageMediaSizeName.NorthAmerica10x12, "10 x 12"}, {PageMediaSizeName.NorthAmerica14x17, "14 x 17"},
                {PageMediaSizeName.BusinessCard, "Business card"}, {PageMediaSizeName.CreditCard, "Credit card"}
            };

            public string SizeLabel => $"{Width}{DimensionSuffix} x {Height}{DimensionSuffix}";
            internal double _width { get; }
            internal double _height { get; }
            public string Name { get; }
            public double Width => GetDimension(_width);
            public double Height => GetDimension(_height);
            public bool IsValid { get; }
            public PageMediaSize MediaSize { get; }

            public PageSize(PageMediaSize mediaSize)
            {
                MediaSize = mediaSize;
                IsValid = mediaSize.Width.HasValue && mediaSize.Height.HasValue && mediaSize.PageMediaSizeName.HasValue && _names.ContainsKey(mediaSize.PageMediaSizeName.Value);
                _width = mediaSize.Width ?? 0.0;
                _height = mediaSize.Height ?? 0.0;
                if (_names.ContainsKey(mediaSize.PageMediaSizeName.Value))
                    Name = _names[mediaSize.PageMediaSizeName.Value];
            }

            public override string ToString() => $"{Name} ({SizeLabel})";
        }
    }
}
