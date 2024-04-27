using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace FESConfigFile
{
    public class FCG_Document
    {
        public const byte SyntaxVersion = 1;
        public const byte StableVersion = 2;
        public const byte PatchVersion = 0;
        public static string version => SyntaxVersion.ToString() + "." + StableVersion.ToString() + "." + PatchVersion.ToString();

        string Filepath = "";
        StringBuilder warning = new StringBuilder();
        int warningindex = 0;
        public bool TabToFourSpace = false;
        public FCG_Node RootNode;
        public bool Open(string Filepath)
        {
            warning.Clear();
            warningindex = 0;
            this.Filepath = Filepath;
            string filetxt = File.ReadAllText(Filepath);
            var ft = filetxt.AsSpan();
            int i = 0;
            var vinfo = new List<char>();
            //Read and Split the Text:ft
            while(i < ft.Length && ft[i] != '<')
            {
                vinfo.Add(ft[i]);
                i++;
            }
            int index = vinfo.IndexOf('V');
            if (byte.Parse(vinfo[index+1].ToString()) != SyntaxVersion && byte.Parse(vinfo[index+2].ToString()) > StableVersion)
            {
                AddWarning(0, 0, "Version are not compatible");
                return false;
            }

            //<
            if(SplitNode(ref ft,ref i,out var rtn))
            {
                RootNode = rtn;
                RootNode.ParentNode = RootNode;
                return true;
            }
            else
            {
                AddWarning(warningindex,i,"RootNode is null");
                return false;
            }
        }
        bool SplitNode(ref ReadOnlySpan<char> ft,ref int i,out FCG_Node outnode)
        {
            warningindex++;
            var node = new FCG_Node("", "");
            var sb = new StringBuilder();
            //<
            if (i < ft.Length && Tot(ft[i]) == 2)
            {
                i++;
                //Remove space
                while(i < ft.Length && Tot(ft[i]) != 0)
                {
                    i++;
                }
                //node.name
                while(i < ft.Length && Tot(ft[i]) == 0)
                {
                    sb.Append(ft[i]);
                    i++;
                }
                node.Name = sb.ToString();
                // > or \s
                while(i < ft.Length && Tot(ft[i]) != 3)
                {
                    //\s
                    while (i < ft.Length && Tot(ft[i]) == 1)
                    {
                        i++;
                    }
                    if (SplitAttribute(ref ft, ref i, out var n, out var v))
                    {
                        node.SetAttribute(n, v);
                    }
                }
                //>
                if (i < ft.Length && Tot(ft[i]) == 3)
                {
                    i++;
                    sb.Clear(); 
                    //\s or letter
                    while (i < ft.Length && Tot(ft[i]) == 1)
                    {
                        i++;
                    }
                    //node.value
                    while (i < ft.Length && Tot(ft[i]) != 2)
                    {
                        sb.Append(ft[i]);
                        i++;
                    }
                    node.Value = sb.ToString().Trim( new char[4] {'\t',' ','\r','\n'});
                    //<
                    while(i < ft.Length && Tot(ft[i]) == 2)
                    {
                        sb.Clear();
                        if(ft[i + 1] == '/')
                        {
                            //node end
                            i += 2;
                            while (i < ft.Length && Tot(ft[i]) != 3)
                            {
                                if (Tot(ft[i]) != 1)
                                {
                                    sb.Append(ft[i]);
                                }
                                i++;
                            }
                            //finally check
                            if(sb.ToString() == node.Name)
                            {
                                warningindex--;
                                outnode = node;
                                return true;
                            }
                            else
                            {
                                warningindex--;
                                outnode = null;
                                return false;
                            }
                        }
                        else
                        {
                            //new node
                            if(SplitNode(ref ft,ref i,out var cdnode))
                            {
                                node.AddNode(cdnode);
                            }
                        }
                        while(i < ft.Length && Tot(ft[i]) != 2)
                        {
                            i++;
                        }
                    }
                    warningindex--;
                    outnode = null;
                    return false;
                }
                else
                {
                    warningindex--;
                    outnode = null;
                    return false;
                }
            }
            else
            {
                warningindex--;
                outnode = null;
                return false;
            }
        }
        bool SplitAttribute(ref ReadOnlySpan<char> ft,ref int i,out string atbname, out string atbvalue)
        {
            StringBuilder sb = new StringBuilder();
            //Atb.name
            while (i < ft.Length && Tot(ft[i]) == 0)
            {
                sb.Append(ft[i]);
                i++;
            }
            atbname = sb.ToString();
            //\s or =
            while (i < ft.Length && Tot(ft[i]) == 1)
            {
                i++;
            }
            //=
            if (Tot(ft[i]) == 4)
            {
                i++;
                sb.Clear();
                //\s
                while (i < ft.Length && Tot(ft[i]) == 1)
                {
                    i++;
                }
                //Atb.value
                if (i < ft.Length && Tot(ft[i]) == 5)
                {
                    i++;
                    while (i < ft.Length && Tot(ft[i]) != 5)
                    {
                        sb.Append(ft[i]);
                        i++;
                    }
                    i++;
                }
                else
                {
                    while (i < ft.Length && Tot(ft[i]) == 0)
                    {
                        sb.Append(ft[i]);
                        i++;
                    }
                }
                atbvalue = sb.ToString();
                if (atbname != "" && atbvalue != "" && i < ft.Length)
                {
                    return true;
                }
                else
                {
                    atbname = null;
                    atbvalue = null;
                    return false;
                }
            }
            else
            {
                atbname = null;
                atbvalue = null;
                return false;
            }
        }
        public void Save()
        {
            if(Filepath != "")
            {
                File.WriteAllText(Filepath,this.ToString());
            }
        }
        public void Save(string filepath)
        {
            Filepath = filepath;
            File.WriteAllText(Filepath,this.ToString());
        }
        public void Close()
        {
            Filepath = "";
            warning.Clear();
            warningindex = 0;
            RootNode.Dispose();
            RootNode = null;
        }
        public override string ToString()
        {
            var sb = new StringBuilder("V").Append(SyntaxVersion).Append(StableVersion);
            return RootNode.ToString(0, TabToFourSpace).Insert(0,sb).ToString();
        }
        void AddWarning(int wi,int i,string ws)
        {
            string s = "Warning(char:" + i.ToString() + ",node:" + wi.ToString() + ")" + ws + Environment.NewLine;
            warning.Append(s);
        }
        public string GetWarning()
        {
            return warning.ToString();
        }
        //Sort by character
        //0:letter
        //1:\t\n\s\r
        //2:<
        //3:>
        //4:=
        //5:\"
        /// <summary>
        /// 0字符 1空白 2开头 3结尾 4等号 5引号
        /// </summary>
        /// <param name="cr"></param>
        /// <returns></returns>
        int Tot(char cr)
        {
            if(cr == ' ' || cr == '\n' || cr == '\t' || cr == '\r')
            {
                return 1;
            }
            else if(cr == '<')
            {
                return 2;
            }
            else if(cr == '>')
            {
                return 3;
            }
            else if(cr == '=')
            {
                return 4;
            }
            else if(cr == '\"')
            {
                return 5;
            }
            else
            {
                return 0;
            }
        }
        /// <summary>
        /// 通过路径来获取指定节点或属性的值，路径中节点通过/分割，属性通过:分割。
        /// 例如：
        /// <code>Root/path/node:atb</code>
        /// </summary>
        /// <param name="path">获取的路径</param>
        /// <param name="value">获取的值结果</param>
        /// <returns>返回获取是否成功</returns>
        public bool PathGetValue(string path,out string value)
        {
            var ps = path.AsSpan();
            var sb = new StringBuilder();
            List<string> pathnodes = new List<string>();
            string att = "";
            bool hasatt = false;
            int i = 0;

            while(i < ps.Length)
            {
                char s = ps[i];
                if(s == ':' && !hasatt)
                {
                    string temp = sb.ToString();
                    if(temp != "")
                    {
                        pathnodes.Add(sb.ToString());
                        sb.Clear();
                    }
                    hasatt = true;
                }
                else if(s == '/' && !hasatt)
                {
                    string temp = sb.ToString();
                    if (temp != "")
                    {
                        pathnodes.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                else
                {
                    if(s != ' ')
                    {
                        sb.Append(s);
                    }
                }
                i++;
            }
            if (hasatt && sb.Length != 0)
            {
                att = sb.ToString();
                sb.Clear();
            }
            else
            {
                pathnodes.Add(sb.ToString());
                sb.Clear();
            }
            //Search
            i = 0;
            FCG_Node now = RootNode;
            if(pathnodes.Count > 1 && pathnodes[i] == now.Name)
            {
                i++;
                while(i < pathnodes.Count)
                {
                    if (now.TryGetChildNode(pathnodes[i], out var next))
                    {
                        now = next;
                    }
                    else
                    {
                        value = "null";
                        return false;
                    }
                    i++;
                }
                if (hasatt)
                {
                    if (now.GetAttribute(att, out value))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    value = now.Value;
                    return true;
                }
            }
            else
            {
                value = "null";
                return false;
            }
        }
    }
    
    public class FCG_Node : IDisposable
    {
        string value;
        string name;
        public FCG_Node ParentNode;
        Dictionary<string, FCG_Node> ChildNodes = new Dictionary<string, FCG_Node>();
        Dictionary<string,string> Attributes = new Dictionary<string, string>();

        public FCG_Node(string name,string value)
        {
            this.name = name.Replace(" ","");
            this.value = value.Trim('\"');
        }
        public string Name
        {
            get => name;
            set => name = value.Replace(" ", "");
        }
        public string Value
        {
            get => value;
            set => this.value = value.Trim('\"');
        }
        public bool AddNode(FCG_Node node)
        {
            if(!ChildNodes.ContainsKey(node.Name))
            {
                ChildNodes.Add(node.Name, node);
                node.ParentNode = this;
                return true;
            }
            return false;
        }
        public FCG_Node this[string key]
        {
            set => ChildNodes[key] = value;
            get => ChildNodes[key];
        }
        public bool TryGetChildNode(string name,out FCG_Node node)
        {
            if (ChildNodes.ContainsKey(name))
            {
                node = ChildNodes[name];
                return true;
            }
            else
            {
                node = null;
                return false;
            }
        }
        public FCG_Node RemoveNode(string name)
        {
            if (ChildNodes.ContainsKey(name))
            {
                ChildNodes[name].ParentNode = null;
                ChildNodes.Remove(name);
            }
            return this;
        }
        public FCG_Node DeleteNode(string name)
        {
            if (ChildNodes.ContainsKey(name))
            {
                ChildNodes[name].Dispose();
                ChildNodes.Remove(name);
            }
            return this;
        }
        public FCG_Node SetAttribute(string key, string value)
        {
            if (Attributes.ContainsKey(key))
            {
                Attributes[key] = value;
            }
            else
            {
                Attributes.Add(key, value);
            }
            return this;
        }
        public bool GetAttribute(string key, out string value) => Attributes.TryGetValue(key, out value);
        public bool RemoveAttribute(string key) => Attributes.Remove(key);
        string Tab(int tab, bool TabToFourSpace)
        {
            StringBuilder ts = new StringBuilder();
            int i = 0;
            while(i < tab)
            {
                if(TabToFourSpace)
                {
                    ts.Append("    ");
                }
                else
                {
                    ts.Append("\t");
                }
                i++;
            }
            return ts.ToString();
        }
        string GetValueText(string value)
        {
            if (value.Contains(" "))
            {
                return "\"" + value + "\"";
            }
            else
            {
                return value;
            }
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<").Append(name);
            foreach (var a in Attributes)
            {
                if (a.Key != "" && a.Value != "") sb.Append(" ").Append(a.Key).Append('=').Append(GetValueText(a.Value));
            }
            sb.Append(">");
            if(value != "")
            {
                sb.Append(value);
            }
            sb.Append("</").Append(name).Append(">");
            return sb.ToString();
        }
        public StringBuilder ToString(int tab, bool TabToFourSpace)
        {
            var sb = new StringBuilder();
            if(name == "")
            {
                return sb;
            }
            sb.Append(Environment.NewLine).Append(Tab(tab, TabToFourSpace)).Append("<").Append(name);
            foreach (var a in Attributes)
            {
                if(a.Key != "" && a.Value != "")sb.Append(" ").Append(a.Key).Append('=').Append(GetValueText(a.Value));
            }
            sb.Append(">");
            if(value != "")
            {
                sb.Append(Environment.NewLine).Append(Tab(tab + 1, TabToFourSpace)).Append(value);
            }
            foreach (var n in ChildNodes)
            {
                sb.Append(n.Value.ToString(tab + 1, TabToFourSpace));
            }
            sb.Append(Environment.NewLine);
            sb.Append(Tab(tab, TabToFourSpace) + "</").Append(name).Append(">");
            return sb;
        }
        public void Dispose()
        {
            value = null;
            name = null;
            ParentNode = null;
            foreach (var node in ChildNodes)
            {
                node.Value.Dispose();
            }
            Attributes.Clear();
            Attributes = null;
        }
        public string GetPath(string AttributeName = "")
        {
            FCG_Node now = this;
            var sb = new StringBuilder(now.Name);
            if(AttributeName != "" && Attributes.ContainsKey(AttributeName))
            {
                sb.Append(':').Append(AttributeName);
            }
            while (!now.Equals(now.ParentNode))
            {
                sb.Insert(0,'/').Insert(0,now.Name);
                now = now.ParentNode;
            }
            sb.Insert(0, '/').Insert(0, now.Name);
            return sb.ToString();
        }
    }
}