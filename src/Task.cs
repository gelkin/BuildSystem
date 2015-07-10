using System.Collections.Generic;

namespace BuildSystem
{
    class Task
    {
        public IList<string> Dependencies;
        public IList<string> Actions;

        public Task(IList<string> dependencies, IList<string> actions)
        {
            Dependencies = dependencies;
            Actions = actions;
        }

        public Task(IList<string> dependencies)
        {
            Dependencies = dependencies;
        }
    }
}
