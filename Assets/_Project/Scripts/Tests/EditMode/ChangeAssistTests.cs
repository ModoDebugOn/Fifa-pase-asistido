using System.Numerics;
using NUnit.Framework;
using AssistedPass.Application;
using AssistedPass.Domain;

namespace AssistedPass.Tests
{
    /// <summary>The level slider and the profile buttons of the demo.</summary>
    public class ChangeAssistTests
    {
        Play play;
        ChangeAssist changeAssist;

        [SetUp]
        public void SetUp()
        {
            var passer = new Player(1, Team.Home, Vector2.Zero);
            play = new Play(new[] { passer }, passer, new AssistConfig { Level = 1f });
            changeAssist = new ChangeAssist(play);
        }

        [Test]
        public void SetLevel_ChangesTheLevelInUse()
        {
            changeAssist.SetLevel(0.3f);

            Assert.That(play.Config.Level, Is.EqualTo(0.3f));
        }

        [Test]
        public void UseProfile_UsesItsValues()
        {
            var manual = new AssistConfig { Level = 0f, MaxAngle = 25f };

            changeAssist.UseProfile(manual);

            Assert.That(play.Config.Level, Is.EqualTo(0f));
            Assert.That(play.Config.MaxAngle, Is.EqualTo(25f));
        }

        [Test]
        public void AfterUsingAProfile_MovingTheSliderDoesNotModifyTheProfile()
        {
            var high = new AssistConfig { Level = 1f };
            changeAssist.UseProfile(high);

            changeAssist.SetLevel(0.2f);

            Assert.That(high.Level, Is.EqualTo(1f));
        }
    }
}
