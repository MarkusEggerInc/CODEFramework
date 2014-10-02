using System.Windows;
using CODE.Framework.Core.Utilities;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for Disclaimer.xaml
    /// </summary>
    public partial class Disclaimer : Window
    {
        public Disclaimer()
        {
            //var e1 = EnumHelper.GetEnumInformation<TestValues>();
            //var e2 = EnumHelper.GetEnumInformation<TestValues>();

            var source = new 
                { 
                    X01 = TestValues.Two,
                    X02 = OtherTestValues.Two,
                    X03 = TestValues.Two,
                    X04 = TestValues.Two,
                    X05 = TestValues.Two,
                    X06 = TestValues.Two,
                    X07 = TestValues.Two,
                    X08 = TestValues.Two,
                    X09 = TestValues.Two,
                    X10 = TestValues.Two,
                    X11 = TestValues.Two,
                    X12 = TestValues.Two,
                    X13 = TestValues.Two,
                    X14 = (System.Byte)2,
                    X15 = (System.SByte)2,
                    X16 = (System.Int16)2,
                    X17 = (System.UInt16)2,
                    X18 = (System.Int32)2,
                    X19 = (System.UInt32)2,
                    X20 = (System.Int64)2,
                    X21 = (System.UInt64)2,
                    X22 = (System.Single)2,
                    X23 = (System.Double)2,
                    X24 = (System.Decimal)2
                };

            var destination = new DestinationClass
                { 
                    X01 = OtherTestValues.Three,
                    X02 = TestValues.Three,
                    X03 = (System.Byte)3,
                    X04 = (System.SByte)3,
                    X05 = (System.Int16)3,
                    X06 = (System.UInt16)3,
                    X07 = (System.Int32)3,
                    X08 = (System.UInt32)3,
                    X09 = (System.Int64)3,
                    X10 = (System.UInt64)3,
                    X11 = (System.Single)3,
                    X12 = (System.Double)3,
                    X13 = (System.Decimal)3,
                    X14 = TestValues.Three,
                    X15 = TestValues.Three,
                    X16 = TestValues.Three,
                    X17 = TestValues.Three,
                    X18 = TestValues.Three,
                    X19 = TestValues.Three,
                    X20 = TestValues.Three,
                    X21 = TestValues.Three,
                    X22 = TestValues.Three,
                    X23 = TestValues.Three,
                    X24 = TestValues.Three,
                };

            Mapper.Map(source, destination);

            InitializeComponent();
        }
    }

    public enum TestValues
    {
        Zero,
        One,
        Two,
        Three,
        Four
    }

    public enum OtherTestValues
    {
        Zero,
        One,
        Two,
        Three,
        Four
    }

    public class DestinationClass
    {
        public OtherTestValues X01 { get; set; }
        public TestValues X02 { get; set; }
        public System.Byte X03 { get; set; }
        public System.SByte X04 { get; set; }
        public System.Int16 X05 { get; set; }
        public System.UInt16 X06 { get; set; }
        public System.Int32 X07 { get; set; }
        public System.UInt32 X08 { get; set; }
        public System.Int64 X09 { get; set; }
        public System.UInt64 X10 { get; set; }
        public System.Single X11 { get; set; }
        public System.Double X12 { get; set; }
        public System.Decimal X13 { get; set; }
        public TestValues X14 { get; set; }
        public TestValues X15 { get; set; }
        public TestValues X16 { get; set; }
        public TestValues X17 { get; set; }
        public TestValues X18 { get; set; }
        public TestValues X19 { get; set; }
        public TestValues X20 { get; set; }
        public TestValues X21 { get; set; }
        public TestValues X22 { get; set; }
        public TestValues X23 { get; set; }
        public TestValues X24 { get; set; }
    }
}
