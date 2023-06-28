using System.Windows;
using System.Windows.Markup;
using Kalus.Controls.Console;

namespace Kalus.UnitTest
{
    public class Tests
    {

        private ConsoleView consoleView;
        [SetUp]
        public void Setup()
        {
            consoleView = new();
            
        }


        [TearDown]
        public void TearDown()
        {
            
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Test1()
        {
            consoleView.Logs.Add(new LogData("test message 1", LogLevel.Info, ClientState.ARAM));
            consoleView.Logs.Add(new LogData("test message 2", LogLevel.Warn, ClientState.NoClient));
            consoleView.Logs.Add(new LogData("test message 3", LogLevel.Error, ClientState.Draft));

            Assert.That(3, Is.EqualTo(consoleView.Logs.Count));
        }

        
    }
}