using lib.net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            //bool parallel = false;
            //string pat = String.Join("/", path);
            //if (useParallel.ContainsKey(pat))
            //{
            //    parallel = useParallel[pat];
            //}
            //else
            //{
            //    useParallel.Add(pat, parallel);
            //}

            Action<NetClient, object>[] act = Tree.getPath(path);
            if (act != null)
            {
                //if (act.Length<5)
                //{
                    ////get cpu usage and decide to use parallel or not the next time
                    //double cpu = GetCpuUsage();
                    foreach (var item in act)
                    {
                        item?.Invoke(cli, Data);
                    }
                    //double cpu2 = GetCpuUsage();
                    //if ((cpu2/cpu > 2||cpu>80) &&cpu2>0.1&&cpu>0.1)
                    //{
                    //    useParallel[pat] = true;
                    //}
                //}
                //else
                //{
                //    Parallel.ForEach(act, (a) => a.Invoke(cli,Data));
                //}
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
