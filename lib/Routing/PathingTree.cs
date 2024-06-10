using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib.Routing
{
    public class PathingTree<Path,Object>
    {
        Path Node = default;
        List<Object> Leafs = new List<Object>();
        Dictionary<Path, PathingTree<Path,Object>> Branches = new Dictionary<Path, PathingTree<Path,Object>>();


        public PathingTree()
        {
        }

        PathingTree(Path node)
        {
            Node = node;
        }

        int AddToLeafs(Object leaf)
        {
            Leafs.Add(leaf);
            return Leafs.Count - 1;
        }

        public void ClearPath(Path[] path)
        {
            if (path.Length == 0)
            {
                Leafs.Clear();
                return;
            }
            if (Branches.ContainsKey(path[0]))
            {
                Branches[path[0]].ClearPath(path.Skip(1).ToArray());
            }
        }
        public void ClearAtPath(Path[] path,int id)
        {
            if (path.Length == 0)
            {
                Leafs[id] = default(Object);
                return;
            }
            if (Branches.ContainsKey(path[0]))
            {
                Branches[path[0]].ClearPath(path.Skip(1).ToArray());
            }
        }

        public int AddToPath(Path[] path, Object action)
        {
            if(path.Length == 0)
            {
                return AddToLeafs(action);
            }
            if (Branches.ContainsKey(path[0]))
            {
                return Branches[path[0]].AddToPath(path.Skip(1).ToArray(), action);
            }
            else
            {
                PathingTree<Path,Object> tree = new PathingTree<Path, Object>(path[0]);
                int ret = tree.AddToPath(path.Skip(1).ToArray(), action);
                Branches.Add(path[0], tree);
                return ret;
            }
        }


        public Object[] getPath(Path[] path)
        {
            if (path.Length == 0)
            {
                return Leafs.Where(a=>!a.Equals(default(Object))).ToArray();
            }
            if (Branches.ContainsKey(path[0]))
            {
                return Branches[path[0]].getPath(path.Skip(1).ToArray());
            }
            return null;
        }
    }
}
