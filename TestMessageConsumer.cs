using MassTransit;

namespace MassTransitStop
{
    public class TestMessageConsumer : IConsumer<TestMessage>
    {
        public Task Consume(ConsumeContext<TestMessage> context)
        {
            Console.WriteLine((Program.IsServer ? "Server Received Message: " : "Client Received Message: ") + context.Message.Key.ToString());
            return Task.CompletedTask;
        }
    }
}
