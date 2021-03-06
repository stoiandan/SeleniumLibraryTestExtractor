﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace JenkinsExtractor
{

    /*
     *  Test case result denoted by:
     *  name, id and result (passed or not)
     */
    class TestResult
    {
        string Name { get; }
        string Id { get; }
        bool Passed { get; }
        public TestResult(string name, string id, bool passed)
        {
            Name = name;
            Id = id;
            Passed = passed;
        }

        public override string ToString()
        {
            StringBuilder stb = new StringBuilder($"Test:   {Name}\n");
            string result = Passed ? "PASS" : "FAIL";
            stb.Append($"Id: {Id} \nResult: {result}\n\n");
            //stb.Append("----------------------\n\n");

            return stb.ToString();
        }
    }

    /*
     * Represents a set of test results from a suite
     */
    class TestSuite
    {
        public List<TestResult> Results { get; } = new List<TestResult>();
        public string SuiteName { get; }
        public TestSuite(string suiteName)
        {
            SuiteName = suiteName;
        }

        public void AddResult(TestResult result)
        {
            Results.Add(result);
        }

        public override string ToString()
        {
            StringBuilder stb = new StringBuilder($"SUITE BEGIN==========={SuiteName}===========SUITE BEGIN\n\n");

            foreach (var result in Results)
            {
                stb.Append(result);
            }
            stb.Append($"SUITE END=============={SuiteName}==============SUITE END\n\n");

            return stb.ToString();
        }
    }


    /*
     * Main class used to parse the xml input
     *  and store test results
     */
    class XExtractor
    {
        public readonly List<TestSuite> Suites = new List<TestSuite>();
        readonly XDocument document;

        readonly XElement root;

        readonly string path;

        public XExtractor(string path)
        {
            this.path = path;
            document = XDocument.Load(path);
            root = document.Root;
        }

        public void Parse()
        {
            foreach (var suiteNode in document.Descendants("suite"))
            {
                if (suiteNode.Attribute("name") != null)
                {
                    var suite = new TestSuite(suiteNode.Attribute("name").Value);
                    foreach (var testNode in suiteNode.Descendants("test"))
                    {
                        if ((testNode.Attribute("id") != null) &&
                            (testNode.Attribute("name") != null) &&
                            (testNode.Element("status") != null) &&
                            (testNode.Descendants("tag") != null))
                        {
                            var statusNode = testNode.Element("status");
                            var idNode = testNode.Descendants("tag").First();

                            if (statusNode.Attribute("status") != null && idNode != null)
                            {
                                suite.AddResult(new TestResult(testNode.Attribute("name").Value,
                                                               idNode.Value,
                                                               statusNode.Attribute("status").Value.ToLower() == "pass")
                                                               );
                            }
                        }
                    }
                    Suites.Add(suite);
                }

            }

        }


        public void PrintResults()
        {
            foreach (var suite in Suites)
            {
                System.Console.WriteLine(suite);
            }
        }

        public void WriteResults(string outputPath)
        {
            using (StreamWriter writer = new StreamWriter(outputPath, true))
            {
                writer.Write($"JENKINS JOB BEGIN==========={Path.GetDirectoryName(path)}===========JENKINS JOB BEGIN");
                foreach (var suite in Suites)
                {
                    writer.Write(suite);
                }
                writer.Write($"JENKINS JOB END==========={Path.GetDirectoryName(path)}===========JENKINS JOB END");
            }
        }
    }
    class Program
    {
        /*
         *  args will pass as argument an string array of paths to XML Jenkins test results files 
         *  the output files location will be specified with "-o outputfile"
         *  POSIX complient
         */
        static void Main(string[] args)
        {

            string outputPath = null;
            try
            {
                outputPath = args.SkipWhile(x => x != "-o").Skip(1).First();
            }
            catch (Exception)
            {
                outputPath = "output.txt";
            }

            foreach (var path in args.TakeWhile(x => x != "-o").Concat(args.SkipWhile(x => x != "-o").Skip(2)))
            {
                XExtractor extractor = null;
                try
                {
                    File.Exists(path);
                    extractor = new XExtractor(path);
                }
                catch (FileLoadException)
                {
                    System.Console.WriteLine("Access rights are not granted to {0}", path);
                }
                catch (IOException)
                {
                    System.Console.WriteLine("File {0} can't be opened ", path);
                }
                System.Console.WriteLine($"JENKINS JOB BEGIN==========={Path.GetDirectoryName(path)}===========JENKINS JOB BEGIN");
                extractor.Parse();
                extractor.WriteResults(outputPath);
                extractor.PrintResults();
                System.Console.WriteLine($"JENKINS JOB END==========={Path.GetDirectoryName(path)}===========JENKINS JOB END");
            }

        }
    }
}
