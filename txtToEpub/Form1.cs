using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace txtToEpub
{
    public partial class Form1 : Form
    {
        public Form1(string[] args)
        {
            InitializeComponent();
            if (args.Length > 0)
            {
                txt2equb(args);
            }
        }

        private void panel1_Click(object sender, EventArgs e)
        {
            OpenFileDialog selectDlg = new OpenFileDialog();
            selectDlg.Multiselect = true;
            selectDlg.Filter= "文本文件(*.txt)|*.txt";
            //selectDlg.ShowDialog();
            if (selectDlg.ShowDialog() == DialogResult.OK)
            {
                txt2equb(selectDlg.FileNames);
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
            panel1_Click(this, null);
        }

        private void panel1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))e.Effect = DragDropEffects.All;
            else e.Effect = DragDropEffects.None;
        }

        private void txt2equb(string[] paths)
        {
            toolStripProgressBar1.Value = 0;
            toolStripProgressBar1.Maximum = paths.Length;
            for(int i=0;i<paths.Length;i++)
            {
                txt2epub(paths[i]);
                toolStripProgressBar1.Value = i+1;
            }
        }

        private void txt2epub(string path)
        {
            if (Path.GetExtension(path) == ".txt")
            {
                string prefix = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
                if (!Directory.Exists(prefix)) Directory.CreateDirectory(prefix);

                writeContent(prefix, path);
                writeMimetype(prefix);
                writeContainer(prefix);
                writeOpf(prefix);
                writeToc(prefix);
                writeCss(prefix);
                zipEpub(prefix);

                Directory.Delete(prefix,true);
            }
        }
        #region
        private void writeMimetype(string path)
        {
            path = Path.Combine(path, "mimetype");
            using(StreamWriter f=new StreamWriter(path,false, Encoding.GetEncoding("UTF-8")))
            {
                f.Write("application/epub+zip");
            }
        }

        private void writeContainer(string path)
        {
            path = Path.Combine(path, "META-INF");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            path = Path.Combine(path, "container.xml");
            string[] container = {
                "<?xml version=\"1.0\"?>",
                "<container version=\"1.0\" xmlns=\"urn:oasis:names:tc:opendocument:xmlns:container\">",
                "<rootfiles><rootfile full-path=\"content.opf\" media-type=\"application/oebps-package+xml\"/></rootfiles>",
                "</container>"
            };
            using (StreamWriter f = new StreamWriter(path, false, Encoding.GetEncoding("UTF-8")))
            {
                f.Write(string.Join("",container));
            }
        }

        private void writeOpf(string path)
        {
            string title = Path.GetFileName(path);
            path = Path.Combine(path, "content.opf");
            string[] opf = {
                "<?xml version=\"1.0\" encoding=\"UTF-8\" ?><package version=\"2.0\" unique-identifier=\"PrimaryID\" xmlns=\"http://www.idpf.org/2007/opf\"><metadata xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:opf =\"http://www.idpf.org/2007/opf\">",
                string.Format("<dc:title>{0}</dc:title>",title),
                "<dc:language>zh-CN</dc:language></metadata>",
                "<manifest>",
                "<item href=\"content.html\" id=\"content\" media-type=\"application/xhtml+xml\"/>",
                "<item id=\"css\" href=\"stylesheet.css\" media-type=\"text/css\" />",
                "<item href=\"toc.ncx\" id=\"ncx\" media-type=\"application/x-dtbncx+xml\"/>",
                "</manifest><spine toc=\"ncx\"><itemref idref=\"content\"/></spine></package>"
            };
            using (StreamWriter f = new StreamWriter(path, false, Encoding.GetEncoding("UTF-8")))
            {
                f.Write(string.Join("", opf));
            }
        }

        string[] encodes = { "GB18030", "GBK", "UTF-8", "GB2312", "Big5", "UTF-16", "Big5SCS", "shiftjis", "eucjp" };
        private void writeContent(string prefix,string txtPath)
        {
            string path = Path.Combine(prefix,"content.html");
            string body = "";
            foreach(string encode in encodes)
            {
                try
                {
                    using (StreamReader f = new StreamReader(txtPath, Encoding.GetEncoding(encode)))
                    {
                        string line = f.ReadLine();
                        while (line != null)
                        {
                            body += checkLine(line.Replace("\n", ""));
                            line = f.ReadLine();
                        }
                    }
                    break;
                }
                catch (Exception)
                {
                    Console.WriteLine(txtPath, "can't open with", encode);
                    continue;
                }
            }

            string[] html = {
                "<?xml version='1.0' encoding='utf-8'?>",
                "<html xmlns=\"http://www.w3.org/1999/xhtml\">",
                "<head>",
                "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" />",
                "</head>",
                string.Format("<body>{0}</body>",body),
                "</html>"
            };

            using (StreamWriter f = new StreamWriter(path, false, Encoding.GetEncoding("UTF-8")))
            {
                f.Write(string.Join("",html));
            }
        }

        private void writeToc(string path)
        {
            path = Path.Combine(path, "toc.ncx");
            string[] toc = {
                "<?xml version='1.0' encoding='utf-8'?>",
                "<ncx xmlns=\"http://www.daisy.org/z3986/2005/ncx/\" version=\"2005-1\" xml:lang=\"zho\">",
                "<navMap><navPoint playOrder=\"1\"><content src=\"content.html\"/></navPoint></navMap></ncx>"
            };
            using (StreamWriter f = new StreamWriter(path, false, Encoding.GetEncoding("UTF-8")))
            {
                f.Write(string.Join("",toc));
            }
        }

        private void writeCss(string path)
        {
            path = Path.Combine(path, "stylesheet.css");
            using (StreamWriter f = new StreamWriter(path, false, Encoding.GetEncoding("UTF-8")))
            {
                f.Write("p{text-indent:2em}");
            }
        }

        string[][] convertRule = new string[][] { new string[]{ "&", "&amp;" }, new string[] { "<", "&lt;" }, new string[] { ">", "&gt;" } };
        private string checkLine(string line)
        {
            if (line.Trim() == "")
            {
                line = "<br/>";
            }
            else
            {
                foreach(string[] pair in convertRule)
                {
                    if (line.Contains(pair[0]))
                    {
                        line=line.Replace(pair[0], pair[1]);
                    }
                }
                line = "<p>" + line + "</p>\n";
            }
            return line;
        }

        private void zipEpub(string path)
        {
            new FastZip().CreateZip(path + ".epub", path, true,"");
        }
        #endregion
        //private void addToZip(ZipFile zip,string path)
        //{
        //    if (File.Exists(path)) zip.Add(path);
        //    else if (Directory.Exists(path))
        //    {
        //        //List<string> treeList = new List<string>();
        //        DirectoryInfo root = new DirectoryInfo(path);
        //        foreach (FileInfo f in root.GetFiles()) addToZip(zip, f.FullName);
        //        foreach (DirectoryInfo d in root.GetDirectories()) addToZip(zip, d.FullName);
        //    }
        //}

        private void panel1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                txt2equb((string[])e.Data.GetData(DataFormats.FileDrop));
            }
        }
    }
}
