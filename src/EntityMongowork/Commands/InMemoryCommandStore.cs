using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityMongowork.Commands
{
    public class InMemoryCommandStore : ICommandStore
    {
        private readonly Queue<MongoCommand> _commands = new Queue<MongoCommand>();

        public bool HasNext => _commands.Any();

        public void AddCommand(MongoCommand command)
        {
            _commands.Enqueue(command);
        }

        public MongoCommand? GetNext()
        {
            if (!HasNext) return null;

            return _commands.Dequeue();
        }
    }
}
