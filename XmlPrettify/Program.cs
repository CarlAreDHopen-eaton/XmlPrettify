using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using CommandLine;
using CommandLine.Text;

namespace XmlPrettify
{
   class Program
   {
      static void Main(string[] args)
      {
         ParserResult<CommandLineOptions> result = Parser.Default.ParseArguments<CommandLineOptions>(args);
         result.WithParsed(options =>
             {
                if (options.Help)
                {
                   Console.WriteLine(HelpText.AutoBuild(result).ToString());
                   return;
                }

                if (string.IsNullOrEmpty(options.InputFile))
                {
                   Console.WriteLine("Please provide the path to the XML file using the -i/--input option.");
                   return;
                }

                Run(options);
             });
      }

      private static void Run(CommandLineOptions options)
      {
         string inputFile = options.InputFile;
         string outputFile = options.OutputFile ?? Path.GetFileNameWithoutExtension(inputFile) + "_formatted.xml";
         int indentSize = options.IndentSize ?? 3;
         List<string> attributeFilter = new List<string>();

         if (!string.IsNullOrEmpty(options.FilterAttributes))
         {
            string[] filter = options.FilterAttributes.Split(',');
            if (filter .Length > 0)
            {
               attributeFilter.AddRange(filter);
            }
         }
         try
         {
            XmlDocument doc = new XmlDocument();
            doc.Load(inputFile);

            ProcessAttributes(doc.DocumentElement, options.SortAttributes, attributeFilter);

            XmlWriterSettings settings = new XmlWriterSettings
            {
               Indent = true,
               IndentChars = new string(' ', indentSize)
            };

            using (XmlWriter writer = XmlWriter.Create(outputFile, settings))
            {
               doc.Save(writer);
            }

            Console.WriteLine($"Formatted XML saved to: {outputFile}");
         }
         catch (Exception ex)
         {
            Console.WriteLine("An error occurred while processing the XML file:");
            Console.WriteLine(ex.Message);
         }
      }

      private static void ProcessAttributes(XmlElement element, bool bSort, List<string> attributeFilter)
      {
         // No need to process if no sort and no filter is defined.
         if (bSort == false && attributeFilter.Count == 0)
            return;

         List<XmlAttribute> attributeList = new List<XmlAttribute>();
         foreach (XmlAttribute attribute in element.Attributes)
         {
            attributeList.Add(attribute);
         }

         if (bSort)
            attributeList.Sort((attr1, attr2) => string.Compare(attr1.Name, attr2.Name, StringComparison.Ordinal));

         element.Attributes.RemoveAll();

         foreach (XmlAttribute attribute in attributeList)
         {
            if (!attributeFilter.Contains(attribute.Name))
               element.SetAttribute(attribute.Name, attribute.Value);
         }

         foreach (XmlNode childNode in element.ChildNodes)
         {
            if (childNode is XmlElement childElement)
            {
               ProcessAttributes(childElement, bSort, attributeFilter);
            }
         }
      }
   }

   public class CommandLineOptions
   {
      [Option('i', "input", Required = true, HelpText = "Path to the XML input file.")]
      public string InputFile { get; set; }

      [Option('o', "output", HelpText = "Path to the output file. If not specified, the input file name with _formatted suffix will be used.")]
      public string OutputFile { get; set; }

      [Option("indent", HelpText = "Indentation size. Default is 3 spaces.")]
      public int? IndentSize { get; set; }

      [Option('h', "help", HelpText = "Display this help text.")]
      public bool Help { get; set; }

      [Option('f', "filter", Required = false, HelpText = "Comma separated list of attributes to filter out..")]
      public string FilterAttributes { get; set; }

      [Option("sort", HelpText = "Sort attributes alphabetically in the nodes.")]
      public bool SortAttributes { get; set; }
   }
}
