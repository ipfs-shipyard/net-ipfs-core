using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ipfs.Engine.Tests;

[TestClass]
public class RandomWalkTest
{
    [TestMethod]
    public async Task CanStartAndStop()
    {
        var walk = new RandomWalk();
        await walk.StartAsync();
        await walk.StopAsync();

        await walk.StartAsync();
        await walk.StopAsync();
    }

    [TestMethod]
    public void CannotStartTwice()
    {
        var walk = new RandomWalk();
        walk.StartAsync().Wait();
        ExceptionAssert.Throws<Exception>(() => { walk.StartAsync().Wait(); });
    }

    [TestMethod]
    public async Task CanStopMultipletimes()
    {
        var walk = new RandomWalk();
        await walk.StartAsync();
        await walk.StopAsync();
        await walk.StopAsync();
        await walk.StartAsync();
        await walk.StopAsync();
    }
}