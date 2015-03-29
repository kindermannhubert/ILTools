﻿using ILTools.MsBuild;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TestRewriter
{
    class Program
    {
        static void Main(string[] args)
        {
            var executingAssemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyToRewritePath = Path.Combine(Path.GetDirectoryName(executingAssemblyLocation), @"..\..\..\TestRewriter\bin\Debug\", Path.GetFileName(executingAssemblyLocation));
            var rewrittenAssemblyPath = Path.Combine(Path.GetDirectoryName(assemblyToRewritePath), Path.GetFileNameWithoutExtension(assemblyToRewritePath) + "_Rewritten" + Path.GetExtension(assemblyToRewritePath));

            Console.WriteLine("Rewriting:\t\{assemblyToRewritePath}");
            Console.WriteLine("Output:\t\t\{rewrittenAssemblyPath}");

            var rewriteTask = new AssemblyRewrite();
            rewriteTask.AssemblyPath = assemblyToRewritePath;
            rewriteTask.Execute(rewrittenAssemblyPath);

            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}