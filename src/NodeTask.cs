using System.Collections.Generic;

namespace BuildSystem
{
    class NodeTask
    {
        private readonly string name;
        public string Name
        {
            get { return name; }
        }
        public IList<NodeTask> Children;
        public int State = 0; // flag for Program.NoCycles(...) method
        public bool Satisfied = false; // flag for Program.SatisfyNodeTask(...) method
        
        public NodeTask(string name)
        {
            this.name = name;
        }

        public NodeTask(string name, IList<NodeTask> children)
        {
            this.name = name;
            Children = children;
        }
    }
}
