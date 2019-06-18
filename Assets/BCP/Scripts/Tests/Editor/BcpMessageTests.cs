using NUnit.Framework;

public class BcpMessageTests {

    /// <summary>
    /// Tests the string to BCP message function.
    /// </summary>
    [Test]
    public void TestStringToBcpMessage()
    {
        BcpMessage message1 = BcpMessage.CreateFromRawMessage("hello");
        Assert.AreEqual(message1.Command, "hello");

        BcpMessage message2 = BcpMessage.CreateFromRawMessage("test?param1=value1&Param2=vALue2");
        Assert.AreEqual(message2.Command, "test");
        Assert.AreEqual(message2.Parameters["param1"], "value1");
        Assert.AreNotEqual(message2.Parameters["param2"], "value2");
        Assert.AreEqual(message2.Parameters["param2"], "vALue2");
        Assert.AreEqual(message2.Parameters["Param2"].Value, string.Empty);

        BcpMessage message3 = BcpMessage.CreateFromRawMessage("testing?param1=int:234&param2=float:45.34&param3");
        Assert.AreEqual(message3.Command, "testing");
        Assert.True(message3.Parameters["param1"].IsNumber);
        Assert.AreEqual(message3.Parameters["param1"].AsInt, 234);
        Assert.True(message3.Parameters["param2"].IsNumber);
        Assert.AreEqual(message3.Parameters["param2"].AsFloat, 45.34f);
        Assert.True(message3.Parameters["param3"].IsNull);
        Assert.AreEqual(message3.Parameters["param3"].Tag, BCP.SimpleJSON.JSONNodeType.NullValue);
        Assert.AreEqual(message3.Parameters["param3"].Value, "null");

        BcpMessage message4 = BcpMessage.CreateFromRawMessage("test_json?json%3D%7B%22src%22%3A%22Images%2FSun.png%22%2C%22name%22%3A%22sun1%22%2C%22hOffset%22%3A250%2C%22vOffset%22%3A250%2C%22alignment%22%3A%22center%22%7D");
        Assert.AreEqual(message4.Command, "test_json");
        Assert.True(message4.Parameters["src"].IsString);
        Assert.AreEqual(message4.Parameters["src"], "Images/Sun.png");
        Assert.True(message4.Parameters["name"].IsString);
        Assert.AreEqual(message4.Parameters["name"], "sun1");
        Assert.True(message4.Parameters["hOffset"].IsNumber);
        Assert.AreEqual(message4.Parameters["hOffset"].AsInt, 250);
        Assert.True(message4.Parameters["vOffset"].IsNumber);
        Assert.AreEqual(message4.Parameters["vOffset"].AsInt, 250);
        Assert.True(message4.Parameters["alignment"].IsString);
        Assert.AreEqual(message4.Parameters["alignment"], "center");
    }

    [Test]
    public void TestUnknownMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessage.CreateFromRawMessage("unknown?param1=noneType");
        Assert.Throws<BcpMessageException>(() => controller.ProcessMessage(message, false));
    }

    [Test]
    public void TestHelloMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessage.CreateFromRawMessage("hello?version=1.1&controller_name=Unity%20Test%20Runner&controller_version=1.0");

        var eventRaised = false;
        BcpMessageController.OnHello += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestGoodbyeMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessage.CreateFromRawMessage("goodbye");

        var eventRaised = false;
        BcpMessageController.OnGoodbye += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestSwitchMessage()
    {
        BcpMessageController controller = new BcpMessageController();

        var eventRaised = false;
        BcpMessageController.OnSwitch += (name, args) => eventRaised = true;

        BcpMessage message = BcpMessage.CreateFromRawMessage("switch?name=s_test&state=int:1");
        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestSwitchMessageBadFormat1()
    {
        var eventRaised = false;
        BcpMessageController.OnSwitch += (name, args) => eventRaised = true;

        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessage.CreateFromRawMessage("switch");
        Assert.False(eventRaised);
        Assert.Throws<BcpMessageException>(() => controller.ProcessMessage(message));
    }

    [Test]
    public void TestSwitchMessageBadFormat2()
    {
        var eventRaised = false;
        BcpMessageController.OnSwitch += (name, args) => eventRaised = true;

        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessage.CreateFromRawMessage("switch?name=s_test");
        Assert.False(eventRaised);
        Assert.Throws<BcpMessageException>(() => controller.ProcessMessage(message));
    }

    [Test]
    public void TestSwitchMessageBadFormat3()
    {
        var eventRaised = false;
        BcpMessageController.OnSwitch += (name, args) => eventRaised = true;

        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessage.CreateFromRawMessage("switch?name=s_test&state=zzz");
        Assert.False(eventRaised);
        Assert.Throws<BcpMessageException>(() => controller.ProcessMessage(message));
    }

    [Test]
    public void TestBallStartMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessage.CreateFromRawMessage("ball_start?player_num=int:1&ball=int:1");

        var eventRaised = false;
        BcpMessageController.OnBallStart += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestBallEndMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessage.CreateFromRawMessage("ball_end");

        var eventRaised = false;
        BcpMessageController.OnBallEnd += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestModeStartMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessage.CreateFromRawMessage("mode_start?name=test&priority=int:1");

        var eventRaised = false;
        BcpMessageController.OnModeStart += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestModeStopMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessage.CreateFromRawMessage("mode_stop?name=test");

        var eventRaised = false;
        BcpMessageController.OnModeStop += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestPlayerAddedMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessage.CreateFromRawMessage("player_added?player_num=int:1");

        var eventRaised = false;
        BcpMessageController.OnPlayerAdded += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestPlayerTurnStartMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessage.CreateFromRawMessage("player_turn_start?player_num=int:1");

        var eventRaised = false;
        BcpMessageController.OnPlayerTurnStart += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestPlayerVariableMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessage.CreateFromRawMessage("player_variable?name=test&player_num=1&value=abc&prev_value=&change=");

        var eventRaised = false;
        BcpMessageController.OnPlayerVariable += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestPlayerScoreMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessage.CreateFromRawMessage("player_variable?name=score&player_num=1&value=int:1234&prev_value=int:0&change=int:1234");

        var variableEventRaised = false;
        var scoreEventRaised = false;
        BcpMessageController.OnPlayerVariable += (name, args) => variableEventRaised = true;
        BcpMessageController.OnPlayerScore += (name, args) => scoreEventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(variableEventRaised);
        Assert.True(scoreEventRaised);
    }

    [Test]
    public void TestTriggerMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessage.CreateFromRawMessage("trigger?name=test");

        var eventRaised = false;
        BcpMessageController.OnTrigger += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestTimerStartedMessage()
    {
        BcpMessageController controller = new BcpMessageController(true);
        BcpMessage message = BcpMessage.CreateFromRawMessage("trigger?name=timer_test_started&ticks=int:10&ticks_remaining=int:10");

        var eventRaised = false;
        BcpMessageController.OnTimer += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestTimerStoppedMessage()
    {
        BcpMessageController controller = new BcpMessageController(true);
        BcpMessage message = BcpMessage.CreateFromRawMessage("trigger?name=timer_test_stopped&ticks=int:10&ticks_remaining=int:10");

        var eventRaised = false;
        BcpMessageController.OnTimer += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestTimerPausedMessage()
    {
        BcpMessageController controller = new BcpMessageController(true);
        BcpMessage message = BcpMessage.CreateFromRawMessage("trigger?name=timer_test_paused&ticks=int:10&ticks_remaining=int:10");

        var eventRaised = false;
        BcpMessageController.OnTimer += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestTimerCompletedMessage()
    {
        BcpMessageController controller = new BcpMessageController(true);
        BcpMessage message = BcpMessage.CreateFromRawMessage("trigger?name=timer_test_completed&ticks=int:10&ticks_remaining=int:10");

        var eventRaised = false;
        BcpMessageController.OnTimer += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestTimerTickMessage()
    {
        BcpMessageController controller = new BcpMessageController(true);
        BcpMessage message = BcpMessage.CreateFromRawMessage("trigger?name=timer_test_tick&ticks=int:10&ticks_remaining=int:10");

        var eventRaised = false;
        BcpMessageController.OnTimer += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestTimerTimeAddedMessage()
    {
        BcpMessageController controller = new BcpMessageController(true);
        BcpMessage message = BcpMessage.CreateFromRawMessage("trigger?name=timer_test_time_added&ticks=int:10&ticks_remaining=int:10&ticks_added=2");

        var eventRaised = false;
        BcpMessageController.OnTimer += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestTimerTimeSubtractedMessage()
    {
        BcpMessageController controller = new BcpMessageController(true);
        BcpMessage message = BcpMessage.CreateFromRawMessage("trigger?name=timer_test_time_subtracted&ticks=int:10&ticks_remaining=int:10&ticks_subtracted=2");

        var eventRaised = false;
        BcpMessageController.OnTimer += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestTiltMessage()
    {
        BcpMessageController controller = new BcpMessageController(true);
        BcpMessage message = BcpMessage.CreateFromRawMessage("trigger?name=tilt");

        var eventRaised = false;
        BcpMessageController.OnTilt += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestTiltWarningMessage()
    {
        BcpMessageController controller = new BcpMessageController(true);
        BcpMessage message = BcpMessage.CreateFromRawMessage("trigger?name=tilt_warning&warnings=2&warnings_remaining=1");

        var eventRaised = false;
        BcpMessageController.OnTiltWarning += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestSlamTiltMessage()
    {
        BcpMessageController controller = new BcpMessageController(true);
        BcpMessage message = BcpMessage.CreateFromRawMessage("trigger?name=slam_tilt");

        var eventRaised = false;
        BcpMessageController.OnSlamTilt += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestErrorMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessage.CreateFromRawMessage("error?message=An%20error%20occurred&command=xxx");

        var eventRaised = false;
        BcpMessageController.OnError += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestResetMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessage.CreateFromRawMessage("reset");

        var eventRaised = false;
        BcpMessageController.OnReset += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }




}
