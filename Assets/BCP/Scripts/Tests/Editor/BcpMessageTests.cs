using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using NSubstitute;

public class BcpMessageTests {

    /// <summary>
    /// Tests the string to BCP message function.
    /// </summary>
    [Test]
    public void TestStringToBcpMessage()
    {
        BcpMessage message1 = BcpMessageManager.StringToBcpMessage("hello");
        Assert.AreEqual(message1.Command, "hello");

        BcpMessage message2 = BcpMessageManager.StringToBcpMessage("test?param1=value1&Param2=vALue2");
        Assert.AreEqual(message2.Command, "test");
        Assert.AreEqual(message2.Parameters.Keys.Count, 2);
        Assert.AreEqual(message2.Parameters["param1"], "value1");
        Assert.Contains("param1", message2.Parameters.Keys);
        Assert.Contains("param2", message2.Parameters.Keys);
        Assert.AreNotEqual(message2.Parameters["param2"], "value2");
        Assert.AreEqual(message2.Parameters["param2"], "vALue2");
        Assert.AreEqual(message2.Parameters["Param2"], "vALue2");
    }

    [Test]
    [ExpectedException(typeof(BcpMessageException))]
    public void TestUnknownMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessageManager.StringToBcpMessage("unknown?param1=noneType");
        controller.ProcessMessage(message);
    }

    [Test]
    public void TestHelloMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessageManager.StringToBcpMessage("hello?version=1.1&controller_name=Unity%20Test%20Runner&controller_version=1.0");

        var eventRaised = false;
        BcpMessageController.OnHello += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestGoodbyeMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessageManager.StringToBcpMessage("goodbye");

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

        BcpMessage message = BcpMessageManager.StringToBcpMessage("switch?name=s_test&state=int:1");
        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    [ExpectedException(typeof(BcpMessageException))]
    public void TestSwitchMessageBadFormat1()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessageManager.StringToBcpMessage("switch");
        controller.ProcessMessage(message);
    }

    [Test]
    [ExpectedException(typeof(BcpMessageException))]
    public void TestSwitchMessageBadFormat2()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessageManager.StringToBcpMessage("switch?name=s_test");
        controller.ProcessMessage(message);
    }

    [Test]
    [ExpectedException(typeof(BcpMessageException))]
    public void TestSwitchMessageBadFormat3()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessageManager.StringToBcpMessage("switch?name=s_test&state=zzz");
        controller.ProcessMessage(message);
    }

    [Test]
    public void TestBallStartMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessageManager.StringToBcpMessage("ball_start?player_num=int:1&ball=int:1");

        var eventRaised = false;
        BcpMessageController.OnBallStart += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestBallEndMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessageManager.StringToBcpMessage("ball_end");

        var eventRaised = false;
        BcpMessageController.OnBallEnd += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestModeStartMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessageManager.StringToBcpMessage("mode_start?name=test&priority=int:1");

        var eventRaised = false;
        BcpMessageController.OnModeStart += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestModeStopMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessageManager.StringToBcpMessage("mode_stop?name=test");

        var eventRaised = false;
        BcpMessageController.OnModeStop += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestPlayerAddedMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessageManager.StringToBcpMessage("player_added?player_num=int:1");

        var eventRaised = false;
        BcpMessageController.OnPlayerAdded += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestPlayerTurnStartMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessageManager.StringToBcpMessage("player_turn_start?player_num=int:1");

        var eventRaised = false;
        BcpMessageController.OnPlayerTurnStart += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestPlayerVariableMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessageManager.StringToBcpMessage("player_variable?name=test&player_num=1&value=abc&prev_value=&change=");

        var eventRaised = false;
        BcpMessageController.OnPlayerVariable += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestPlayerScoreMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessageManager.StringToBcpMessage("player_variable?name=score&player_num=1&value=int:1234&prev_value=int:0&change=int:1234");

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
        BcpMessage message = BcpMessageManager.StringToBcpMessage("trigger?name=test");

        var eventRaised = false;
        BcpMessageController.OnTrigger += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestTimerStartedMessage()
    {
        BcpMessageController controller = new BcpMessageController(true);
        BcpMessage message = BcpMessageManager.StringToBcpMessage("trigger?name=timer_test_started&ticks=int:10&ticks_remaining=int:10");

        var eventRaised = false;
        BcpMessageController.OnTimer += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestTimerStoppedMessage()
    {
        BcpMessageController controller = new BcpMessageController(true);
        BcpMessage message = BcpMessageManager.StringToBcpMessage("trigger?name=timer_test_stopped&ticks=int:10&ticks_remaining=int:10");

        var eventRaised = false;
        BcpMessageController.OnTimer += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestTimerPausedMessage()
    {
        BcpMessageController controller = new BcpMessageController(true);
        BcpMessage message = BcpMessageManager.StringToBcpMessage("trigger?name=timer_test_paused&ticks=int:10&ticks_remaining=int:10");

        var eventRaised = false;
        BcpMessageController.OnTimer += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestTimerCompletedMessage()
    {
        BcpMessageController controller = new BcpMessageController(true);
        BcpMessage message = BcpMessageManager.StringToBcpMessage("trigger?name=timer_test_completed&ticks=int:10&ticks_remaining=int:10");

        var eventRaised = false;
        BcpMessageController.OnTimer += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestTimerTickMessage()
    {
        BcpMessageController controller = new BcpMessageController(true);
        BcpMessage message = BcpMessageManager.StringToBcpMessage("trigger?name=timer_test_tick&ticks=int:10&ticks_remaining=int:10");

        var eventRaised = false;
        BcpMessageController.OnTimer += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestTimerTimeAddedMessage()
    {
        BcpMessageController controller = new BcpMessageController(true);
        BcpMessage message = BcpMessageManager.StringToBcpMessage("trigger?name=timer_test_time_added&ticks=int:10&ticks_remaining=int:10&ticks_added=2");

        var eventRaised = false;
        BcpMessageController.OnTimer += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestTimerTimeSubtractedMessage()
    {
        BcpMessageController controller = new BcpMessageController(true);
        BcpMessage message = BcpMessageManager.StringToBcpMessage("trigger?name=timer_test_time_subtracted&ticks=int:10&ticks_remaining=int:10&ticks_subtracted=2");

        var eventRaised = false;
        BcpMessageController.OnTimer += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestTiltMessage()
    {
        BcpMessageController controller = new BcpMessageController(true);
        BcpMessage message = BcpMessageManager.StringToBcpMessage("trigger?name=tilt");

        var eventRaised = false;
        BcpMessageController.OnTilt += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestTiltWarningMessage()
    {
        BcpMessageController controller = new BcpMessageController(true);
        BcpMessage message = BcpMessageManager.StringToBcpMessage("trigger?name=tilt_warning&warnings=2&warnings_remaining=1");

        var eventRaised = false;
        BcpMessageController.OnTiltWarning += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestSlamTiltMessage()
    {
        BcpMessageController controller = new BcpMessageController(true);
        BcpMessage message = BcpMessageManager.StringToBcpMessage("trigger?name=slam_tilt");

        var eventRaised = false;
        BcpMessageController.OnSlamTilt += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestErrorMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessageManager.StringToBcpMessage("error?message=An%20error%20occurred&command=xxx");

        var eventRaised = false;
        BcpMessageController.OnError += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }

    [Test]
    public void TestResetMessage()
    {
        BcpMessageController controller = new BcpMessageController();
        BcpMessage message = BcpMessageManager.StringToBcpMessage("reset");

        var eventRaised = false;
        BcpMessageController.OnReset += (name, args) => eventRaised = true;

        controller.ProcessMessage(message);
        Assert.True(eventRaised);
    }




}
