
//*************************************************************************************
//   !!! Generated by the fmp-cli 1.85.0.  DO NOT EDIT!
//*************************************************************************************

public abstract class HealthyUnitTestBase : IClassFixture<TestFixture>
{
    /// <summary>
    /// 测试上下文
    /// </summary>
    protected TestFixture fixture_ { get; set; }

    public HealthyUnitTestBase(TestFixture _testFixture)
    {
        fixture_ = _testFixture;
    }


    [Fact]
    public abstract Task EchoTest();

}
