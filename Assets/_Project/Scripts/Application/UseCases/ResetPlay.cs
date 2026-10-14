namespace AssistedPass.Application
{
    /// <summary>Gives the ball back to the initial holder to repeat the play.</summary>
    public sealed class ResetPlay
    {
        readonly Play play;
        readonly IBall ball;

        public ResetPlay(Play play, IBall ball)
        {
            this.play = play;
            this.ball = ball;
        }

        public void Execute()
        {
            play.Holder = play.InitialHolder;
            play.Phase = PlayPhase.InPossession;
            play.LastResult = null;
            ball.GiveTo(play.InitialHolder);
        }
    }
}
