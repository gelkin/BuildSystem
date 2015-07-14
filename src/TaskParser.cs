using System;
using System.Collections.Generic;
using System.Text;

namespace BuildSystem
{
    class TaskParser
    {
        private int start;
        private readonly string s;

        private const string parseErrorPrefix = "Parse error:";
        private const string taskNameExceptionMsg = "Only letters and digits are allowed in task name.";
        private const string taskDeclarationExceptionMsg = "No \':\' character in target's dependencies declaration.";

        public TaskParser(string s)
        {
            this.s = String.Copy(s);
        }

        public string GetTarget()
        {
            StringBuilder target = new StringBuilder();
            for (int i = 0; i < s.Length; ++i)
            {
                if (char.IsLetterOrDigit(s[i]))
                {
                    target.Append(s[i]);
                }
                else if (s[i] == ':')
                {
                    start = i + 1;
                    return target.ToString();
                }
                else
                {
                    throw new BuildSystemException(String.Format("{0} in string \"{1}\" at pos {2}. {3}",
                                                                 parseErrorPrefix,
                                                                 s,
                                                                 i,
                                                                 taskNameExceptionMsg));
                }
            }
            throw new BuildSystemException(String.Format("{0} in string \"{1}\". {2}",
                                                         parseErrorPrefix,
                                                         s,
                                                         taskDeclarationExceptionMsg));
        }

        public IList<string> GetDependenciesList()
        {
            IList<string> dependencies = new List<string>();
            string d;
            while ((d = NextDependency()) != null)
            {
                dependencies.Add(d);
            }

            return dependencies;
        }

        private string NextDependency()
        {
            // skip prefix spaces
            while (start < s.Length && s[start] == ' ')
            {
                ++start;
            }
            StringBuilder dependency = new StringBuilder();
            for (int i = start; i < s.Length; ++i)
            {
                if (char.IsLetterOrDigit(s[i]))
                {
                    dependency.Append(s[i]);
                }
                else if (s[i] == ' ' && dependency.Length > 0)
                {
                    start = i + 1;
                    return dependency.ToString();
                }
                else
                {
                    throw new BuildSystemException(String.Format("{0} in string \"{1}\" at pos {2}. {3}",
                                                                 parseErrorPrefix,
                                                                 s.Substring(start),
                                                                 i - start,
                                                                 taskNameExceptionMsg));
                }
            }
            if (dependency.Length > 0)
            {
                start = s.Length;
                return dependency.ToString();
            }

            return null;
        }
    }
}
