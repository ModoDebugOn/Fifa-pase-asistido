using AssistedPass.Domain;

namespace AssistedPass.Application
{
    /// <summary>
    /// Changes how much the game helps with the pass: the level slider and the profile buttons.
    /// It takes effect on the next evaluation (the very next frame while aiming).
    /// </summary>
    public sealed class ChangeAssist
    {
        readonly Play play;

        public ChangeAssist(Play play) => this.play = play;

        /// <summary>0 = manual (the cone is a line), 1 = maximum help.</summary>
        public void SetLevel(float level) => play.Config.Level = level;

        /// <summary>Uses a copy of the profile, so later level changes don't modify the original.</summary>
        public void UseProfile(AssistConfig profile) => play.Config = profile.Clone();
    }
}
