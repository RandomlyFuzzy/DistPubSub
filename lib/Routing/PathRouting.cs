using lib.net;
using System;

namespace lib.Routing
{
    public static class PathRouting
    {
        static PathingTree<string, Action<NetClient,object>> Tree { get; set; } = new ();

        public static int AddPath(Action<NetClient, object> action,params string[] path)
        {

            return Tree.AddToPath(path, action);
        }
        public static int InvokePath(NetClient cli, object Data, params string[] path)
        {
            Action<NetClient, object>[] act = Tree.getPath(path);
            if (act != null)
            {
                foreach (var item in act)
                {
                    item?.Invoke(cli, Data);
                }
                return act.Length;
            }
            return 0;
        }

        public static void RemovePath(params string[] path)
        {
            Tree.ClearPath(path);
        }
        public static void RemovePath(int id,params string[] path)
        {
            Tree.ClearAtPath(path, id);
        }





    }
}
