using UnityEngine;
using UnityEditor;
using NUnit.Framework;

public class BcpVariableTests {

    /// <summary>
    /// Tests the string to BCP variable function.
    /// </summary>
    [Test]
    public void TestParameterStringToBcpVariable()
    {
        BcpVariable var1 = new BcpVariable("a string");
        Assert.AreEqual(var1.Type, BcpVariable.VariableType.String);
        Assert.AreEqual(var1.ToString(), "a string");

        BcpVariable var2 = new BcpVariable("noneType:");
        Assert.AreEqual(var2.Type, BcpVariable.VariableType.NoneType);
        Assert.AreEqual(var2.ToString(), string.Empty);

        BcpVariable var3 = new BcpVariable("int:5280");
        Assert.AreEqual(var3.Type, BcpVariable.VariableType.Int);
        Assert.AreEqual(var3.ToString(), "5280");
        Assert.AreEqual(var3.ToInt(), 5280);

        BcpVariable var4 = new BcpVariable("float:33.47");
        Assert.AreEqual(var4.Type, BcpVariable.VariableType.Float);
        Assert.AreEqual(var4.ToString(), "33.47");
        Assert.AreEqual(var4.ToFloat(), 33.47f);

        BcpVariable var5 = new BcpVariable("bool:true");
        Assert.AreEqual(var5.Type, BcpVariable.VariableType.Boolean);
        Assert.AreEqual(var5.ToString(), "true");
        Assert.AreEqual(var5.ToBoolean(), true);
    }

    /// <summary>
    /// Tests the machine variable store.
    /// </summary>
    [Test]
    public void TestMachineVariableStore()
    {
        MachineVars machineVars = new MachineVars();

        machineVars["test"] = new BcpVariable("a string");
        Assert.True(machineVars.Contains("test"));
        Assert.AreEqual(machineVars["test"].Type, BcpVariable.VariableType.String);
        Assert.AreEqual(machineVars["test"].ToString(), "a string");

        Assert.Null(machineVars["TEST"]);

        Assert.Null(machineVars["bad_test"]);

        machineVars.Add("test2", new BcpVariable("int:2048"));
        Assert.True(machineVars.Contains("test2"));
        Assert.AreEqual(machineVars["test2"].Type, BcpVariable.VariableType.Int);
        Assert.AreEqual(machineVars["test2"].ToString(), "2048");
        Assert.AreEqual(machineVars["test2"].ToInt(), 2048);

        machineVars.Add("test3", "bool:False");
        Assert.True(machineVars.Contains("test3"));
        Assert.AreEqual(machineVars["test3"].Type, BcpVariable.VariableType.Boolean);
        Assert.AreEqual(machineVars["test3"].ToString(), "false");
        Assert.AreEqual(machineVars["test3"].ToBoolean(), false);

    }



}
