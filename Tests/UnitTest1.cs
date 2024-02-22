namespace Kinderworx.Utilities.BuildUtilities.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
           string test =   Kinderworx.Utilities.BuildUtilities.BuildUtils.Test();

            Assert.True(test.Contains("s"));
        }
    }
}