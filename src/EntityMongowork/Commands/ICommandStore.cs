using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityMongowork.Commands
{
    public interface ICommandStore
    {
        bool HasNext { get; }
        void AddCommand(MongoCommand command);
        MongoCommand? GetNext();
    }
}
