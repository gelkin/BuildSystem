using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace BuildSystem
{
    class Program
    {
        private bool isUnix;

        private const string makefileName = "makefile";
        private const string cmdProgramName = "cmd.exe";
        private const string cmdCommandPrefix = "/c ";

        private string initialTask;
        private bool firstTaskIsInitial;

        private Dictionary<string, Task> taskToDescription = new Dictionary<string, Task>();
        private Dictionary<string, NodeTask> taskToNode = new Dictionary<string, NodeTask>();
        private NodeTask initialTaskDependencyTree;

        static void Main(string[] args)
        {   
            new Program().Run(args);
        }

        private void Run(string[] args)
        {
            if (args.Length == 0)
            {
                firstTaskIsInitial = true;
            }
            else if (args.Length == 1)
            {
                initialTask = args[0];
            }
            else
            {
                Console.WriteLine("Too much arguments.");
                return;
            }

            try
            {
                isUnix = IsUnix();
             
                SatisfyInitialTask();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private bool IsUnix()
        {
            int p = (int)Environment.OSVersion.Platform;
            return (p == 4) || (p == 6) || (p == 128);
        }

        private void SatisfyInitialTask()
        {
            if (!File.Exists(makefileName))
            {
                throw new BuildSystemException(String.Format("File \"{0}\" doesn't exist.", makefileName));
            }

            // first pass through file
            ReadDependeciesForAllTasks();
            initialTaskDependencyTree = BuildDependencyTree(initialTask);
            if (!NoCycles(initialTaskDependencyTree))
            {
                throw new BuildSystemException("Cannot determine the order of execution: circular dependency found.");
            }

            // second pass through file
            ReadActionsForTasksFromDependencyTree();
            SatisfyNodeTask(initialTaskDependencyTree);
        }

        private void ReadDependeciesForAllTasks()
        {
            using (StreamReader sr = new StreamReader(makefileName))
            {
                bool alreadyReadLine = false;
                string line = "";
                while (alreadyReadLine || (line = sr.ReadLine()) != null)
                {
                    alreadyReadLine = false;

                    TaskParser parser = new TaskParser(line);
                    string target = parser.GetTarget();

                    if (firstTaskIsInitial)
                    {
                        initialTask = target;
                        firstTaskIsInitial = false;
                    }

                    if (taskToDescription.ContainsKey(target))
                    {
                        throw new BuildSystemException(String.Format("Task \"{0}\" declaration occurs more than one time.", target));
                    }

                    Task task = new Task(parser.GetDependenciesList());
                    taskToDescription.Add(target, task);
                    while ((line = sr.ReadLine()) != null) // skip actions
                    {
                        if (line.Length == 0)
                        {
                            continue; // blank line are expected
                        }

                        if (line[0] != ' ' && line[0] != '\t')
                        {
                            alreadyReadLine = true; // found not an action
                            break;
                        }
                    }
                }

                if (initialTask == null)
                {
                    throw new BuildSystemException(String.Format("File \"{0}\" is empty.", makefileName));
                }
            }
        }

        private NodeTask BuildDependencyTree(string rootTask)
        {
            Queue<NodeTask> notProcessedNodes = new Queue<NodeTask>();
            NodeTask rootNode = new NodeTask(rootTask);
            taskToNode.Add(rootTask, rootNode);
            notProcessedNodes.Enqueue(rootNode);
            while (notProcessedNodes.Count > 0)
            {
                NodeTask currentNode = notProcessedNodes.Dequeue();
                if (!taskToDescription.ContainsKey(currentNode.Name))
                {
                    throw new BuildSystemException(String.Format("No declaration for task \"{0}\" found.", rootTask));
                }

                IList<string> dependencies = taskToDescription[currentNode.Name].Dependencies;
                IList<NodeTask> children = new List<NodeTask>(dependencies.Count);
                foreach (var child in dependencies)
                {
                    if (taskToNode.ContainsKey(child))
                    {
                        children.Add(taskToNode[child]);
                    }
                    else
                    {
                        NodeTask childNode = new NodeTask(child);
                        taskToNode.Add(child, childNode);
                        children.Add(childNode);

                        notProcessedNodes.Enqueue(childNode);
                    }
                }
                currentNode.Children = children;
            }

            return rootNode;
        }

        private bool NoCycles(NodeTask node)
        {
            Stack<NodeTask> notProcessedNodes = new Stack<NodeTask>();

            node.State = 1;
            notProcessedNodes.Push(node);

            while (notProcessedNodes.Count > 0)
            {
                bool continueFlag = false;
                
                NodeTask current = notProcessedNodes.Peek();
                foreach (var child in current.Children)
                {
                    if (child.State == 1)
                    {
                        return false;
                    }
                    if (child.State == 0)
                    {
                        child.State = 1;
                        notProcessedNodes.Push(child);
                        
                        continueFlag = true;
                        break;
                    }
                }
                if (continueFlag)
                {
                    continue;
                }

                // all children are black =)
                current.State = 2;
                notProcessedNodes.Pop();
            }

            return true;
        }

        private void ReadActionsForTasksFromDependencyTree()
        {
            using (StreamReader sr = new StreamReader(makefileName))
            {
                bool alreadyReadLine = false;
                string line = "";
                while (alreadyReadLine || (line = sr.ReadLine()) != null)
                {
                    alreadyReadLine = false;

                    TaskParser parser = new TaskParser(line);
                    string target = parser.GetTarget();
                    if (taskToNode.ContainsKey(target))
                    {
                        taskToDescription[target].Actions = ReadActionsForTask(sr, out line);
                        if (line != null)
                        {
                            alreadyReadLine = true;
                        }
                    }
                    else // skip actions
                    {
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (line.Length == 0)
                            {
                                continue;
                            }

                            if (line[0] != ' ' && line[0] != '\t')
                            {
                                alreadyReadLine = true;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private IList<string> ReadActionsForTask(StreamReader sr, out string line)
        {
            IList<string> actions = new List<string>();
            while ((line = sr.ReadLine()) != null)
            {
                if (line.Length == 0)
                {
                    continue;
                }

                if (line[0] != ' ' && line[0] != '\t')
                {
                    return actions;
                }

                int firstNonBlankCharPos = 0;
                for (int i = 0; i < line.Length; ++i)
                {
                    if (line[i] != ' ' && line[i] != '\t')
                    {
                        firstNonBlankCharPos = i;
                        break;
                    }
                }

                actions.Add(line.Substring(firstNonBlankCharPos));
            }

            return actions;
        }

        private void SatisfyNodeTask(NodeTask node)
        {
            Stack<NodeTask> notSatisfiedTasks = new Stack<NodeTask>();

            Console.WriteLine("\"{0}\" task is started.", node.Name);
            notSatisfiedTasks.Push(node);

            while (notSatisfiedTasks.Count > 0)
            {
                bool continueFlag = false;
                NodeTask current = notSatisfiedTasks.Peek();
                foreach (var child in current.Children)
                {
                    if (!child.Satisfied)
                    {
                        Console.WriteLine("\"{0}\" task is started.", child.Name);
                        notSatisfiedTasks.Push(child);

                        continueFlag = true;
                        break;
                    }
                }
                if (continueFlag)
                {
                    continue;
                }

                ExecuteActions(taskToDescription[current.Name].Actions);
                current.Satisfied = true;
                Console.WriteLine("\"{0}\" task is finished.", current.Name);
                notSatisfiedTasks.Pop();
            }
        }

        private void ExecuteActions(IList<string> actions)
        {
            foreach (var action in actions)
            {
                if (!ExecuteActionInCMD(action))
                {
                    throw new BuildSystemException(String.Format("Error: executing of \"{0}\" ended up with non-zero return value.", action));
                }
            }
        }

        private bool ExecuteActionInCMD(string action)
        {
            Process process = new Process();
            process.StartInfo = InitStartInfo(action);
            string stdOutput;
            string stdError;
            try
            {
                process.Start();
                stdOutput = process.StandardOutput.ReadToEnd();
                stdError = process.StandardError.ReadToEnd();
                process.WaitForExit();
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("OS error while executing \"{0}\": {1}.", action, e.Message), e);
            }

            Console.Write(stdOutput);
            Console.Write(stdError);
            return (process.ExitCode == 0);
        }

        private ProcessStartInfo InitStartInfo(string action)
        {
            string command = null;
            string args = null;
            if (isUnix)
            {
                for (int i = 0; i < action.Length; ++i)
                {
                    if (action[i] == ' ')
                    {
                        command = action.Substring(0, i);
                        args = action.Substring(i);
                        break;
                    }
                }
                if (command == null)
                {
                    command = action;
                    args = "";
                }
            }
            else
            {
                command = cmdProgramName;
                args = cmdCommandPrefix + action;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo(command, args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            return startInfo;
        }
    }
}
