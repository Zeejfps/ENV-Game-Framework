namespace Pong;

public sealed class BallPaddleCollisionSystem
{
    public void Update(Ball ball, Paddle bottomPaddle, Paddle topPaddle)
    {
        if (ball.CurrPosition.X > bottomPaddle.CurrPosition.X - bottomPaddle.Size &&
            ball.CurrPosition.X < bottomPaddle.CurrPosition.X + bottomPaddle.Size &&
            ball.CurrPosition.Y <= bottomPaddle.CurrPosition.Y + 2 &&
            ball.CurrPosition.Y >= bottomPaddle.CurrPosition.Y - 2)
        {
            ball.Velocity = ball.Velocity with { Y = -ball.Velocity.Y };
        }
        else 
        if (ball.CurrPosition.X > topPaddle.CurrPosition.X - topPaddle.Size &&
            ball.CurrPosition.X < topPaddle.CurrPosition.X + topPaddle.Size &&
            ball.CurrPosition.Y >= topPaddle.CurrPosition.Y - 2)
        {
            ball.Velocity = ball.Velocity with { Y = -ball.Velocity.Y };
        }
    }
}