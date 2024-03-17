using QIQI.EProjectFile;
using QIQI.EProjectFile.Sections;

namespace FormRipper
{
    internal class Program
    {

        static string ToFileName(string uuid)
        {
            switch (uuid)
            {
                case "d09f2340818511d396f6aaf844c7e325":
                    return "krnln";
                case "27bb20fdd3e145e4bee3db39ddd6e64c":
                    return "iext";
                case "A512548E76954B6E92C21055517615B0":
                    return "spec";
                case "52F260023059454187AF826A3C07AF2A":
                    return "shell";
                case "{A068799B-7551-46b9-8CA8-EEF8357AFEA4}":
                    return "commobj";
                case "4BB4003860154917BC7D8230BF4FA58A":
                    return "dp1";
                case "F7FC1AE45C5C4758AF03EF19F18A395D":
                    return "eAPI";
                case "DA19AC3ADD2F4121AAD84AC5FBCAFC71":
                    return "shellEx";
            }
            return "";
        }

        static void GenerateECode(string template, string save, List<FormInfo> forms, List<LibraryRefInfo> libs)
        {
            EplDocument doc = new EplDocument();

            // load template
            doc.Load(new MemoryStream(File.ReadAllBytes(template)));

            for (int i = 0; i < doc.Sections.Count; i++)
            {
                if (doc.Sections[i].SectionKey == 67138329)
                {
                    ResourceSection res = (ResourceSection)doc.Sections[i];

                    for (int j = 0; j < forms.Count; j++)
                    {
                        if (forms[j].Name == "")
                            forms[j].Name = "匿名窗口_" + j;

                        res.Forms.Add(forms[j]);
                    }

                    doc.Sections.RemoveAt(i);
                    doc.Sections.Add(res);
                    break;
                }
            }

            for (int i = 0; i < doc.Sections.Count; i++)
            {
                if (doc.Sections[i].SectionKey == 50361113)
                {
                    CodeSection Code = (CodeSection)doc.Sections[i];
                    for (int j = 0; j < libs.Count; j++)
                    {
                        libs[j].FileName = ToFileName(libs[j].GuidString);
                    }
                    Code.Libraries = libs.ToArray();
                    doc.Sections.RemoveAt(i);
                    doc.Sections.Add(Code);
                    break;
                }
            }

            doc.Save(new FileStream(save, FileMode.OpenOrCreate, FileAccess.ReadWrite));
        }

        static void Main(string[] args)
        {
            EApp app = new EApp(@"IDE.exe");
            File.WriteAllBytes(@"Const.bin", app.ReadEConst());
            var forms = app.ReadEForm();
            var libs = app.ReadELib();
            var dll = app.ReadEDll();

            GenerateECode(@"2.e", @"output.e", forms, libs);

            Console.WriteLine();
        }
    }
}
